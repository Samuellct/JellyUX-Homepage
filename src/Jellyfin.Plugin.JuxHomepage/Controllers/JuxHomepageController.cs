using System.Globalization;
using System.Reflection;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Localization;
using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using Jellyfin.Plugin.JuxHomepage.Widgets.Admin;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.Controllers;

/// <summary>
/// Serves JellyUX Homepage web resources, plugin metadata, and widget data.
/// Route: /JuxHomepage
/// </summary>
[ApiController]
[Route("[controller]")]
public class JuxHomepageController : ControllerBase
{
    private static readonly Assembly PluginAssembly = typeof(JuxHomepageController).Assembly;

    private readonly IWidgetRegistry _registry;
    private readonly WidgetService _widgetService;
    private readonly IUserManager _userManager;
    private readonly ITMDbCacheService _tmdbCacheService;
    private readonly ITMDbApiClient _tmdbApiClient;
    private readonly ILocalizationService _localizationService;
    private readonly IAuthorizationContext _authContext;
    private readonly ILogger<JuxHomepageController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JuxHomepageController"/> class.
    /// </summary>
    /// <param name="registry">The widget registry.</param>
    /// <param name="widgetService">The widget orchestration service.</param>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="tmdbCacheService">TMDb disk cache service.</param>
    /// <param name="tmdbApiClient">TMDb HTTP API client.</param>
    /// <param name="localizationService">Widget/admin-UI translation service.</param>
    /// <param name="authContext">Jellyfin request authorization context.</param>
    /// <param name="logger">Logger.</param>
    public JuxHomepageController(
        IWidgetRegistry registry,
        WidgetService widgetService,
        IUserManager userManager,
        ITMDbCacheService tmdbCacheService,
        ITMDbApiClient tmdbApiClient,
        ILocalizationService localizationService,
        IAuthorizationContext authContext,
        ILogger<JuxHomepageController> logger)
    {
        _registry = registry;
        _widgetService = widgetService;
        _userManager = userManager;
        _tmdbCacheService = tmdbCacheService;
        _tmdbApiClient = tmdbApiClient;
        _localizationService = localizationService;
        _authContext = authContext;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // Web resources (anonymous)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Serves the JellyUX Homepage JavaScript bundle.
    /// Anonymous - loaded by a script tag injected into index.html.
    /// </summary>
    /// <returns>JavaScript file contents.</returns>
    [HttpGet("jux-homepage.js")]
    [AllowAnonymous]
    [Produces("application/javascript")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetScript()
    {
        var stream = GetEmbeddedResource("jux-homepage.js");
        if (stream is null)
        {
            return NotFound();
        }

        SetCacheHeaders(Response);
        return File(stream, "application/javascript");
    }

    /// <summary>
    /// Serves the JellyUX Homepage CSS stylesheet.
    /// Anonymous - loaded by a link tag injected into index.html.
    /// </summary>
    /// <returns>CSS file contents.</returns>
    [HttpGet("jux-homepage.css")]
    [AllowAnonymous]
    [Produces("text/css")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetStylesheet()
    {
        var stream = GetEmbeddedResource("jux-homepage.css");
        if (stream is null)
        {
            return NotFound();
        }

        SetCacheHeaders(Response);
        return File(stream, "text/css");
    }

    /// <summary>
    /// Returns the flat translation dictionary for a language, for the admin config page and the
    /// home screen script to translate their own UI/section titles client- or server-side.
    /// Anonymous - translation strings are not sensitive, same as the JS/CSS resources above.
    /// </summary>
    /// <param name="lang">The requested language code (e.g. "fr"), or null to use English.</param>
    /// <returns>The merged key-to-string dictionary for the requested language.</returns>
    [HttpGet("Localizations")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyDictionary<string, string>> GetLocalizations([FromQuery] string? lang = null)
    {
        return Ok(_localizationService.GetDictionary(lang));
    }

    // -------------------------------------------------------------------------
    // Plugin metadata
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns plugin status metadata: enabled flag and any startup warning.
    /// </summary>
    /// <returns>Plugin metadata object.</returns>
    [HttpGet("meta")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<PluginMeta> GetMeta()
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        return Ok(new PluginMeta(config.Enabled, config.StartupWarning));
    }

    // -------------------------------------------------------------------------
    // Widget layout (authenticated user)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the ordered list of widget descriptors for the user's home screen.
    /// Widgets that return fewer items than their MinItems threshold are excluded.
    /// </summary>
    /// <param name="userId">The requesting user's Jellyfin identifier.</param>
    /// <param name="page">Zero-based page index (20 descriptors per page).</param>
    /// <param name="lang">
    /// The requested language code (e.g. "fr"), sourced client-side from
    /// <c>document.documentElement.lang</c>. Null defaults to English.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of <see cref="WidgetDescriptor"/> objects.</returns>
    [HttpGet("Sections")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<WidgetDescriptor>>> GetSections(
        [FromQuery] Guid userId,
        [FromQuery] int page = 0,
        [FromQuery] string? lang = null,
        CancellationToken cancellationToken = default)
    {
        if (!await IsRequestAuthorizedForUserAsync(userId).ConfigureAwait(false))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var descriptors = await _widgetService
            .GetWidgetsForUser(userId, page, lang, cancellationToken)
            .ConfigureAwait(false);

        return Ok(descriptors);
    }

    /// <summary>
    /// Returns items for a specific widget section.
    /// Called lazily by the front end when a section scrolls into view.
    /// </summary>
    /// <param name="widgetType">The widget type identifier.</param>
    /// <param name="userId">The requesting user's Jellyfin identifier.</param>
    /// <param name="additionalData">Optional instance-specific data (e.g. library ID).</param>
    /// <param name="startIndex">Zero-based start index for pagination.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="WidgetResult"/> with items and total count.</returns>
    [HttpGet("Section/{widgetType}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WidgetResult>> GetSection(
        [FromRoute] string widgetType,
        [FromQuery] Guid userId,
        [FromQuery] string? additionalData = null,
        [FromQuery] int startIndex = 0,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (!await IsRequestAuthorizedForUserAsync(userId).ConfigureAwait(false))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var result = await _widgetService
            .GetWidgetItems(userId, widgetType, additionalData, startIndex, limit, cancellationToken)
            .ConfigureAwait(false);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    // -------------------------------------------------------------------------
    // Administration
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns descriptors for all registered widget types.
    /// Used by the admin UI to display available widgets and their capabilities.
    /// </summary>
    /// <returns>An array of <see cref="WidgetDescriptor"/> objects.</returns>
    [HttpGet("Widgets")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<WidgetDescriptor>> GetWidgets()
    {
        var descriptors = _registry.GetAll()
            .Select(w => w.GetDescriptor())
            .ToList()
            .AsReadOnly();

        return Ok(descriptors);
    }

    /// <summary>
    /// Returns the list of available values for an admin widget type, for use in the autocomplete
    /// picker when the administrator adds a new section.
    /// </summary>
    /// <param name="widgetType">The widget type identifier (e.g. "jux.admin.genre").</param>
    /// <param name="userId">The requesting user's Jellyfin identifier.</param>
    /// <param name="search">Optional search string to filter results.</param>
    /// <returns>An array of <see cref="AdminWidgetValue"/> objects.</returns>
    [HttpGet("Widget/{widgetType}/values")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<AdminWidgetValue>> GetWidgetValues(
        [FromRoute] string widgetType,
        [FromQuery] Guid userId,
        [FromQuery] string? search = null)
    {
        var widget = _registry.GetByType(widgetType);
        if (widget is null)
        {
            return NotFound();
        }

        if (widget is not AdminWidgetBase adminWidget)
        {
            return BadRequest("The specified widget type does not support configurable values.");
        }

        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(adminWidget.GetAvailableValues(user, search));
    }

    /// <summary>
    /// Returns the last-refreshed timestamp and cached item count for each TMDb cache type, for
    /// display in the admin UI.
    /// </summary>
    /// <returns>An array of <see cref="TMDbCacheStatusDto"/> objects, one per <see cref="TMDbCacheType"/>.</returns>
    [HttpGet("TMDb/Status")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<TMDbCacheStatusDto>> GetTMDbStatus()
    {
        var statuses = Enum.GetValues<TMDbCacheType>()
            .Select(type => new TMDbCacheStatusDto(
                type.ToString(),
                _tmdbCacheService.GetLastRefreshedUtc(type),
                GetTMDbItemCount(type)))
            .ToList()
            .AsReadOnly();

        return Ok(statuses);
    }

    /// <summary>
    /// Triggers an immediate refresh of all four TMDb cache types. Runs in the background; the
    /// response does not wait for the refresh to complete. Rejects the request with 409 Conflict if
    /// a refresh (manual or the daily scheduled task) is already in progress.
    /// </summary>
    /// <returns>202 Accepted if a refresh was started; 409 Conflict if one was already running.</returns>
    [HttpPost("TMDb/Refresh")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult RefreshTMDbCache()
    {
        if (!_tmdbCacheService.TryAcquireRefreshLock())
        {
            _logger.LogInformation("TMDb refresh already in progress; rejecting this manual request.");
            return Conflict(new { message = "A TMDb refresh is already in progress." });
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _tmdbCacheService.RunRefreshLockedAsync(null, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manually triggered TMDb refresh failed.");
            }
        });

        return Accepted();
    }

    /// <summary>
    /// Returns the ISO 3166-1 country list from TMDb, for use in the Now Playing region picker.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of <see cref="AdminWidgetValue"/> objects (Value=country code, Label=name).</returns>
    [HttpGet("TMDb/Countries")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminWidgetValue>>> GetTMDbCountries(CancellationToken cancellationToken)
    {
        var countries = await _tmdbApiClient.GetCountriesAsync(cancellationToken).ConfigureAwait(false);
        var values = countries
            .Select(country => new AdminWidgetValue(country.Code, country.Name))
            .OrderBy(value => value.Label, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

        return Ok(values);
    }

    /// <summary>
    /// Returns the full TMDb movie genre list, for the Discover widget's genre picker.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of <see cref="AdminWidgetValue"/> objects (Value=genre id, Label=name).</returns>
    [HttpGet("TMDb/Genres")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminWidgetValue>>> GetTMDbGenres(CancellationToken cancellationToken)
    {
        var genres = await _tmdbApiClient.GetMovieGenresAsync(cancellationToken).ConfigureAwait(false);
        var values = genres
            .Select(genre => new AdminWidgetValue(genre.Id.ToString(CultureInfo.InvariantCulture), genre.Name))
            .OrderBy(value => value.Label, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

        return Ok(values);
    }

    /// <summary>
    /// Searches TMDb people by name, for the Discover widget's person autocomplete.
    /// </summary>
    /// <param name="query">The search text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of <see cref="AdminWidgetValue"/> objects (Value=person id, Label=name).</returns>
    [HttpGet("TMDb/Search/Person")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminWidgetValue>>> SearchTMDbPerson(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        return Ok(await SearchTMDbAsync(_tmdbApiClient.SearchPersonAsync, query, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Searches TMDb keywords by name, for the Discover widget's keyword autocomplete.
    /// </summary>
    /// <param name="query">The search text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of <see cref="AdminWidgetValue"/> objects (Value=keyword id, Label=name).</returns>
    [HttpGet("TMDb/Search/Keyword")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminWidgetValue>>> SearchTMDbKeyword(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        return Ok(await SearchTMDbAsync(_tmdbApiClient.SearchKeywordAsync, query, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Searches TMDb companies by name, for the Discover widget's company autocomplete.
    /// </summary>
    /// <param name="query">The search text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of <see cref="AdminWidgetValue"/> objects (Value=company id, Label=name).</returns>
    [HttpGet("TMDb/Search/Company")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminWidgetValue>>> SearchTMDbCompany(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        return Ok(await SearchTMDbAsync(_tmdbApiClient.SearchCompanyAsync, query, cancellationToken).ConfigureAwait(false));
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the caller is allowed to act on behalf of <paramref name="requestedUserId"/>,
    /// preventing an authenticated user from reading another user's data by supplying an arbitrary
    /// userId in the query string (IDOR). Administrators and server API key requests are exempt.
    /// </summary>
    private async Task<bool> IsRequestAuthorizedForUserAsync(Guid requestedUserId)
    {
        var authInfo = await _authContext.GetAuthorizationInfo(HttpContext).ConfigureAwait(false);
        return UserAccessGuard.IsAuthorizedForUser(
            requestedUserId,
            authInfo.UserId,
            authInfo.IsApiKey,
            User.IsInRole("Administrator"));
    }

    private static Stream? GetEmbeddedResource(string suffix)
    {
        var name = PluginAssembly
            .GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

        return name is null ? null : PluginAssembly.GetManifestResourceStream(name);
    }

    private static void SetCacheHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "public, max-age=3600";
    }

    private static async Task<IReadOnlyList<AdminWidgetValue>> SearchTMDbAsync(
        Func<string, CancellationToken, Task<IReadOnlyList<TMDbSearchResult>>> search,
        string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var results = await search(query, cancellationToken).ConfigureAwait(false);
        return results
            .Select(r => new AdminWidgetValue(r.Id.ToString(CultureInfo.InvariantCulture), r.Name))
            .ToList()
            .AsReadOnly();
    }

    private int GetTMDbItemCount(TMDbCacheType type) => type switch
    {
        TMDbCacheType.TrendingMovies => _tmdbCacheService.GetTrendingMovies().Count,
        TMDbCacheType.TrendingShows => _tmdbCacheService.GetTrendingShows().Count,
        TMDbCacheType.AiringToday => _tmdbCacheService.GetAiringToday().Count,
        TMDbCacheType.TopRatedMovies => _tmdbCacheService.GetTopRatedMovies().Count,
        TMDbCacheType.TopRatedShows => _tmdbCacheService.GetTopRatedShows().Count,
        TMDbCacheType.NowPlayingMovies => _tmdbCacheService.GetNowPlayingMovies().Count,
        _ => 0
    };

    /// <summary>Plugin status metadata returned by /JuxHomepage/meta.</summary>
    /// <param name="Enabled">Whether the plugin is active.</param>
    /// <param name="Warning">Startup warning if a dependency is missing; otherwise null.</param>
    public record PluginMeta(bool Enabled, string? Warning);

    /// <summary>TMDb cache status entry returned by /JuxHomepage/TMDb/Status.</summary>
    /// <param name="Type">The <see cref="TMDbCacheType"/> name (e.g. "TrendingMovies").</param>
    /// <param name="LastRefreshedUtc">UTC timestamp of the last successful refresh, or null if never refreshed.</param>
    /// <param name="ItemCount">The number of items currently cached for this type.</param>
    public record TMDbCacheStatusDto(string Type, DateTime? LastRefreshedUtc, int ItemCount);
}
