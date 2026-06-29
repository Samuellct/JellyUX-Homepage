namespace Jellyfin.Plugin.JuxHomepage.Widgets.Native;

/// <summary>
/// Provides the default <see cref="WidgetConfig"/> entries for all built-in native widgets.
/// Used to seed <see cref="Configuration.PluginConfiguration.Widgets"/> on first startup
/// when no configuration has been saved yet.
/// </summary>
public static class NativeWidgetDefaults
{
    /// <summary>
    /// Builds the default widget configuration array for all registered native widgets.
    /// Each entry is enabled, ordered with a gap of 10 between widgets, and configured
    /// with the widget's own <see cref="IWidget.DefaultMinItems"/> and view mode.
    /// </summary>
    /// <returns>A fixed-order array of five <see cref="WidgetConfig"/> entries.</returns>
    public static WidgetConfig[] Build() =>
    [
        new WidgetConfig
        {
            WidgetType = "jux.native.continue-watching",
            Enabled = true,
            Order = 0,
            MinItems = 1,
            MaxItems = 20,
            ViewMode = WidgetViewMode.Landscape,
            MinInstances = 1,
            MaxInstances = 1,
            AllowUserOverride = true
        },
        new WidgetConfig
        {
            WidgetType = "jux.native.next-up",
            Enabled = true,
            Order = 10,
            MinItems = 1,
            MaxItems = 20,
            ViewMode = WidgetViewMode.Landscape,
            MinInstances = 1,
            MaxInstances = 1,
            AllowUserOverride = true
        },
        new WidgetConfig
        {
            WidgetType = "jux.native.recently-added-movies",
            Enabled = true,
            Order = 20,
            MinItems = 4,
            MaxItems = 20,
            ViewMode = WidgetViewMode.Portrait,
            MinInstances = 1,
            MaxInstances = 1,
            AllowUserOverride = true
        },
        new WidgetConfig
        {
            WidgetType = "jux.native.recently-added-shows",
            Enabled = true,
            Order = 30,
            MinItems = 4,
            MaxItems = 20,
            ViewMode = WidgetViewMode.Portrait,
            MinInstances = 1,
            MaxInstances = 1,
            AllowUserOverride = true
        },
        new WidgetConfig
        {
            WidgetType = "jux.native.my-media",
            Enabled = true,
            Order = 40,
            MinItems = 1,
            MaxItems = 20,
            ViewMode = WidgetViewMode.Landscape,
            MinInstances = 1,
            MaxInstances = 1,
            AllowUserOverride = true
        }
    ];
}
