namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Shared helpers for resolving a <see cref="WidgetConfig"/> row's <see cref="WidgetConfig.ExtraParams"/>.
/// </summary>
public static class WidgetConfigExtensions
{
    /// <summary>Returns all extra parameters as a dictionary (empty if there are none).</summary>
    /// <param name="config">The widget config row to read.</param>
    /// <returns>A dictionary of every configured extra parameter.</returns>
    public static IReadOnlyDictionary<string, string> GetExtraParamsDictionary(this WidgetConfig config) =>
        config.ExtraParams.ToDictionary(p => p.Key, p => p.Value);
}
