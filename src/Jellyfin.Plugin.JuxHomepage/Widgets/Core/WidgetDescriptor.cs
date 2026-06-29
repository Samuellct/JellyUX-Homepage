namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Read-only descriptor returned by <see cref="IWidget.GetDescriptor"/>.
/// Describes a widget instance to the front end without exposing internal implementation details.
/// </summary>
public sealed class WidgetDescriptor
{
    /// <summary>Gets or sets the unique type identifier for this widget.</summary>
    public string WidgetType { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name shown in the UI for this instance.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the widget category.</summary>
    public WidgetCategory Category { get; set; }

    /// <summary>
    /// Gets or sets the view mode identifier.
    /// Use <see cref="WidgetViewMode"/> constants.
    /// </summary>
    public string ViewMode { get; set; } = WidgetViewMode.Landscape;

    /// <summary>Gets or sets the front-end route for browsing items of this type, if applicable.</summary>
    public string? Route { get; set; }

    /// <summary>
    /// Gets or sets optional additional data that identifies this instance when multiple instances
    /// of the same widget type are present.
    /// </summary>
    public string? AdditionalData { get; set; }

    /// <summary>Gets or sets the display order on the home screen (lower values appear first).</summary>
    public int Order { get; set; }

    /// <summary>Gets or sets the minimum number of items required to display this widget.</summary>
    public int MinItems { get; set; }
}
