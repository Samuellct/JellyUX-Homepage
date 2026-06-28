using System.Reflection;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.JuxHomepage.Controllers;

/// <summary>
/// Serves JellyUX Homepage web resources and plugin metadata.
/// Route: /JuxHomepage
/// </summary>
[ApiController]
[Route("[controller]")]
public class JuxHomepageController : ControllerBase
{
    private static readonly Assembly PluginAssembly = typeof(JuxHomepageController).Assembly;

    /// <summary>
    /// Serves the JellyUX Homepage JavaScript bundle.
    /// Anonymous — loaded by a script tag injected into index.html.
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
    /// Anonymous — loaded by a link tag injected into index.html.
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
