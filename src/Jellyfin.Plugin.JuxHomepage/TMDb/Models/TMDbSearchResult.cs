using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JuxHomepage.TMDb.Models;

/// <summary>
/// A single result from TMDb's <c>/search/person</c>, <c>/search/keyword</c>, or
/// <c>/search/company</c> endpoints. All three share the same minimal id/name shape.
/// </summary>
public sealed class TMDbSearchResult
{
    /// <summary>Gets or sets the TMDb identifier.</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
