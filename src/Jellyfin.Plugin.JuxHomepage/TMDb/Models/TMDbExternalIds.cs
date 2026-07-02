using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JuxHomepage.TMDb.Models;

/// <summary>
/// Response shape of TMDb's <c>/movie/{id}/external_ids</c> and <c>/tv/{id}/external_ids</c>
/// endpoints. Only the IMDb ID is used by this plugin, for cross-referencing with the local
/// Jellyfin library.
/// </summary>
public sealed class TMDbExternalIds
{
    /// <summary>Gets or sets the IMDb identifier (e.g. "tt1375666"), or null if unknown to TMDb.</summary>
    [JsonPropertyName("imdb_id")]
    public string? ImdbId { get; set; }
}
