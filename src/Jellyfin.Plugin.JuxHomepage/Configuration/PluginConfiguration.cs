using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.JuxHomepage.Configuration;

/// <summary>
/// Plugin configuration. Extended fully in Phase 3.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the JellyUX Homepage plugin is active.
    /// Full configuration is built in Phase 3.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a warning message set at startup when a required dependency is missing.
    /// Null when all dependencies are present. Surfaced via /JuxHomepage/meta.
    /// </summary>
    public string? StartupWarning { get; set; }
}
