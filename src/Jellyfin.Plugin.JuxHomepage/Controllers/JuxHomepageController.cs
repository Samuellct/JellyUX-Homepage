using System.Reflection;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="JuxHomepageController"/> class.
    /// </summary>
    /// <param name="registry">The widget registry.</param>
    /// <param name="widgetService">The widget orchestration service.</param>
    public JuxHomepageController(IWidgetRegistry registry, WidgetService widgetService)
    {
        _registry = registry;
        _widgetService = widgetService;
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of <see cref="WidgetDescriptor"/> objects.</returns>
    [HttpGet("Sections")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WidgetDescriptor>>> GetSections(
        [FromQuery] Guid userId,
        [FromQuery] int page = 0,
        CancellationToken cancellationToken = default)
    {
        var descriptors = await _widgetService
            .GetWidgetsForUser(userId, page, cancellationToken)
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WidgetResult>> GetSection(
        [FromRoute] string widgetType,
        [FromQuery] Guid userId,
        [FromQuery] string? additionalData = null,
        [FromQuery] int startIndex = 0,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
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
    /// Returns the current global plugin configuration.
    /// </summary>
    /// <returns>The plugin configuration.</returns>
    [HttpGet("Configuration")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<PluginConfiguration> GetConfiguration()
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        return Ok(config);
    }

    /// <summary>
    /// Replaces the global plugin configuration.
    /// Changes take effect immediately and are persisted to disk.
    /// </summary>
    /// <param name="config">The new configuration.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("Configuration")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult UpdateConfiguration([FromBody] PluginConfiguration config)
    {
        if (Plugin.Instance is null)
        {
            return BadRequest("Plugin instance is not available.");
        }

        Plugin.Instance.UpdateConfiguration(config);
        return NoContent();
    }

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

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

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

    /// <summary>Plugin status metadata returned by /JuxHomepage/meta.</summary>
    /// <param name="Enabled">Whether the plugin is active.</param>
    /// <param name="Warning">Startup warning if a dependency is missing; otherwise null.</param>
    public record PluginMeta(bool Enabled, string? Warning);
}
