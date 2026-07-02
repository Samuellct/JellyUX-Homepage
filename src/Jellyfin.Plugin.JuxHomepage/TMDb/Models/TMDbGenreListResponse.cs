using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JuxHomepage.TMDb.Models;

/// <summary>
/// Response envelope for TMDb's <c>/genre/movie/list</c> endpoint. Unlike the other list endpoints
/// used by this plugin, it is not paginated and wraps its results under <c>genres</c> rather than
/// <c>results</c>.
/// </summary>
public sealed class TMDbGenreListResponse
{
    /// <summary>Gets or sets the full list of movie genres.</summary>
    [JsonPropertyName("genres")]
    public IReadOnlyList<TMDbGenre> Genres { get; set; } = [];
}
