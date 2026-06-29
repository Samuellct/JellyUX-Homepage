namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// A key/value pair for widget-specific extra configuration.
/// Used in place of Dictionary so that <see cref="WidgetConfig.ExtraParams"/> remains
/// serializable by XmlSerializer, which does not support IDictionary types.
/// </summary>
public sealed class WidgetExtraParam
{
    /// <summary>Gets or sets the parameter key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameter value.</summary>
    public string Value { get; set; } = string.Empty;
}
