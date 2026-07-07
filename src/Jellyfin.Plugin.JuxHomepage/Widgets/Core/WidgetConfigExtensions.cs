namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Shared helpers for resolving a <see cref="WidgetConfig"/> row's <see cref="WidgetConfig.ExtraParams"/>.
/// </summary>
public static class WidgetConfigExtensions
{
    /// <summary>
    /// Returns the value of a single named extra parameter, or null if absent. Avoids building a
    /// full dictionary for the common single-key lookup case.
    /// </summary>
    /// <param name="config">The widget config row to read.</param>
    /// <param name="key">The extra parameter key to look up.</param>
    /// <returns>The parameter's value, or null if not present.</returns>
    public static string? GetExtraParam(this WidgetConfig config, string key)
    {
        foreach (var param in config.ExtraParams)
        {
            if (param.Key == key)
            {
                return param.Value;
            }
        }

        return null;
    }

    /// <summary>Returns all extra parameters as a dictionary (empty if there are none).</summary>
    /// <param name="config">The widget config row to read.</param>
    /// <returns>A dictionary of every configured extra parameter.</returns>
    public static IReadOnlyDictionary<string, string> GetExtraParamsDictionary(this WidgetConfig config) =>
        config.ExtraParams.ToDictionary(p => p.Key, p => p.Value);
}
