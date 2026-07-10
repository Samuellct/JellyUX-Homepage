using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JuxHomepage.Rewards.Models;

/// <summary>
/// Response envelope for the MediaWiki Action API's <c>action=wbsearchentities</c> endpoint, used to
/// back the admin UI's Ceremony/Category autocomplete fields (TODO_V2.md Phase 14.4).
/// </summary>
public sealed class WikidataSearchResponse
{
    /// <summary>Gets or sets the matching entities.</summary>
    [JsonPropertyName("search")]
    public IReadOnlyList<WikidataSearchEntity> Search { get; set; } = [];
}

/// <summary>A single entity match from <c>action=wbsearchentities</c>.</summary>
public sealed class WikidataSearchEntity
{
    /// <summary>Gets or sets the Wikidata Q-id (e.g. "Q102427").</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the display label (e.g. "Academy Award for Best Picture").</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the short description shown alongside the label, if any.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
