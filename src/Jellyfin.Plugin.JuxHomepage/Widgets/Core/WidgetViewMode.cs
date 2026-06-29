namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Standard view mode identifiers for widget card rendering.
/// Use these constants instead of magic strings when setting <see cref="WidgetConfig.ViewMode"/> or
/// <see cref="WidgetDescriptor.ViewMode"/>.
/// </summary>
public static class WidgetViewMode
{
    /// <summary>Tall portrait cards (e.g. movie and series posters).</summary>
    public const string Portrait = "Portrait";

    /// <summary>Wide landscape cards (e.g. episode backdrops).</summary>
    public const string Landscape = "Landscape";

    /// <summary>Square cards (e.g. music albums, people).</summary>
    public const string Square = "Square";
}
