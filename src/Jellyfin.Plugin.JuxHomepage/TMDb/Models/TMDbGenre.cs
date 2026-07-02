using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JuxHomepage.TMDb.Models;

/// <summary>A movie genre as returned by TMDb's <c>/genre/movie/list</c> endpoint.</summary>
public sealed class TMDbGenre
{
    /// <summary>Gets or sets the TMDb genre identifier.</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Gets or sets the genre name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
