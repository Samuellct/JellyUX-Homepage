namespace Jellyfin.Plugin.JuxHomepage.Widgets.Admin;

/// <summary>
/// Represents a selectable value for an admin widget.
/// <para>
/// For most widget types <see cref="Value"/> and <see cref="Label"/> are identical (e.g. a genre
/// name). For collection-based widgets <see cref="Value"/> is the item GUID and
/// <see cref="Label"/> is the human-readable collection name.
/// </para>
/// </summary>
public sealed class AdminWidgetValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminWidgetValue"/> class.
    /// </summary>
    /// <param name="value">The machine-readable value stored in ExtraParams.</param>
    /// <param name="label">The human-readable label shown in the autocomplete list.</param>
    public AdminWidgetValue(string value, string label)
    {
        Value = value;
        Label = label;
    }

    /// <summary>Gets the machine-readable value stored in <c>ExtraParams["value"]</c>.</summary>
    public string Value { get; }

    /// <summary>Gets the human-readable label shown to the administrator.</summary>
    public string Label { get; }
}
