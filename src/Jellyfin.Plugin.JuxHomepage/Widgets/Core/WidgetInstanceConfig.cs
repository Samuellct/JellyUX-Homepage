namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Resolved per-instance configuration passed to <see cref="IWidget.Resolve"/>.
/// Carries the effective settings after merging global plugin configuration and user overrides.
/// </summary>
public sealed class WidgetInstanceConfig
{
    /// <summary>Gets or sets the resolved display name for this instance.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the minimum number of items required to display this widget instance.</summary>
    public int MinItems { get; set; } = 4;

    /// <summary>Gets or sets the maximum number of items to return per request.</summary>
    public int MaxItems { get; set; } = 20;

    /// <summary>
    /// Gets or sets the view mode identifier for this instance.
    /// Use <see cref="WidgetViewMode"/> constants.
    /// </summary>
    public string ViewMode { get; set; } = WidgetViewMode.Landscape;

    /// <summary>Gets or sets the display order on the home screen (lower values appear first).</summary>
    public int Order { get; set; }

    /// <summary>Gets or sets optional extra parameters for widget-specific instance configuration.</summary>
    public IReadOnlyDictionary<string, string>? ExtraParams { get; set; }

    /// <summary>
    /// Gets or sets optional additional data that differentiates multiple instances of the same widget type
    /// (e.g. a library ID for a per-library widget).
    /// </summary>
    public string? AdditionalData { get; set; }

    /// <summary>
    /// Gets or sets the normalized language code (e.g. "fr", "en") this instance's <see cref="DisplayName"/>
    /// was resolved for. Read by fan-out widgets (e.g. personalized widgets) so a per-instance name they
    /// build themselves is translated in the same language as the rest of the layout.
    /// </summary>
    public string? Lang { get; set; }
}
