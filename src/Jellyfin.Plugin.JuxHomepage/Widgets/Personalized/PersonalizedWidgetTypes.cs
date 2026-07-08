namespace Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;

/// <summary>
/// Widget type identifiers for the 4 Personalized widgets, exposed independently of the concrete
/// widget classes so <see cref="Plugin"/>'s configuration migration (TODO_V2.md Phase 8.4) can
/// recognize them without depending on the widget implementations themselves.
/// </summary>
public static class PersonalizedWidgetTypes
{
    /// <summary>Every Personalized widget type identifier.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        "jux.personalized.favorite-genre",
        "jux.personalized.favorite-actor",
        "jux.personalized.favorite-director",
        "jux.personalized.because-you-watched"
    ];
}
