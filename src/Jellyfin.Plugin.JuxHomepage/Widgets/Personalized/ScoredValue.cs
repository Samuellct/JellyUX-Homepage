namespace Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;

/// <summary>
/// A single scored preference produced by <see cref="ScoringService"/>.
/// </summary>
/// <param name="Value">
/// The filter value to apply to the query (e.g. a genre name, a person name, or an item GUID).
/// </param>
/// <param name="Label">
/// The human-readable label used to build the section display name. Equal to <paramref name="Value"/>
/// for genres and people; for recently-watched films this is the film title while
/// <paramref name="Value"/> is the film's item GUID.
/// </param>
public sealed record ScoredValue(string Value, string Label);
