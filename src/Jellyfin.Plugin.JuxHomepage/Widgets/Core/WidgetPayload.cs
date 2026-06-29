namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Request payload passed to <see cref="IWidget.GetItemsAsync"/> when fetching widget items.
/// </summary>
public sealed class WidgetPayload
{
    /// <summary>Gets or sets the requesting user's identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets optional additional data that identifies this instance when the widget supports
    /// multiple instances (e.g. a library ID).
    /// </summary>
    public string? AdditionalData { get; set; }

    /// <summary>Gets or sets the zero-based start index for pagination.</summary>
    public int StartIndex { get; set; }

    /// <summary>Gets or sets the maximum number of items to return.</summary>
    public int Limit { get; set; } = 20;

    /// <summary>Gets or sets optional extra parameters for widget-specific filtering.</summary>
    public IReadOnlyDictionary<string, string>? ExtraParams { get; set; }
}
