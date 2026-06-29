using Jellyfin.Plugin.JuxHomepage.Widgets;

namespace Jellyfin.Plugin.JuxHomepage.Configuration;

/// <summary>
/// Per-user configuration stored as JSON on disk.
/// Allows users to override the global widget settings with their own preferences.
/// </summary>
public sealed class UserConfiguration
{
    /// <summary>Gets or sets the Jellyfin user this configuration belongs to.</summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets user-specific widget overrides.
    /// Each entry overrides the matching global <see cref="PluginConfiguration.Widgets"/> entry
    /// for widgets where <see cref="WidgetConfig.AllowUserOverride"/> is true.
    /// </summary>
    public WidgetConfig[] WidgetOverrides { get; set; } = [];

    /// <summary>
    /// Gets or sets whether the user has explicitly enabled or disabled the JellyUX homepage.
    /// When null, the global <see cref="PluginConfiguration.Enabled"/> setting applies.
    /// </summary>
    public bool? Enabled { get; set; }
}
