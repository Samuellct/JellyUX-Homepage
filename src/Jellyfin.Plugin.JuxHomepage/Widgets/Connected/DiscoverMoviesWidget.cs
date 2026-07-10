using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.Widgets.Connected;

/// <summary>
/// Displays movies matching an administrator-defined TMDb Discover filter (genre, person, keyword,
/// company, sort order, year, minimum rating/votes) that are present in the local library. Unlike
/// the other connected widgets, each instance has its own filter and its own cache, identified by
/// <see cref="WidgetPayload.AdditionalData"/> (the config row's <c>ExtraParams["value"]</c>, the
/// same mechanism admin widgets use).
/// </summary>
public sealed class DiscoverMoviesWidget : ConnectedWidgetBase<TMDbMovie>
{
    private readonly ITMDbCacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverMoviesWidget"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="dtoService">Jellyfin DTO projection service.</param>
    /// <param name="cacheService">TMDb disk cache service.</param>
    /// <param name="logger">Logger.</param>
    public DiscoverMoviesWidget(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ITMDbCacheService cacheService,
        ILogger<DiscoverMoviesWidget> logger)
        : base(userManager, libraryManager, dtoService, logger)
    {
        _cacheService = cacheService;
    }

    /// <inheritdoc/>
    public override string WidgetType => TMDbWidgetTypes.DiscoverMovies;

    /// <inheritdoc/>
    public override string DefaultDisplayName => "Discover Movies";

    /// <inheritdoc/>
    public override string DefaultViewMode => WidgetViewMode.Portrait;

    /// <inheritdoc/>
    public override int MaxInstances => 5;

    /// <inheritdoc/>
    protected override IReadOnlyList<TMDbMovie> GetCachedItems(WidgetPayload payload) =>
        payload.AdditionalData is null ? [] : _cacheService.GetDiscoverMovies(payload.AdditionalData);
}
