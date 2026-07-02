using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JuxHomepage.TMDb.Models;

/// <summary>
/// A TV show as returned by TMDb's trending/airing-today list endpoints.
/// </summary>
public sealed class TMDbShow : ITMDbCacheItem
{
    /// <summary>Gets or sets the TMDb show identifier.</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Gets or sets the show name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the show overview/synopsis.</summary>
    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    /// <summary>Gets or sets the first air date (ISO 8601 date string, e.g. "2024-06-01").</summary>
    [JsonPropertyName("first_air_date")]
    public string? FirstAirDate { get; set; }

    /// <summary>Gets or sets the poster image path (relative to TMDb's image CDN).</summary>
    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    /// <summary>Gets or sets the backdrop image path (relative to TMDb's image CDN).</summary>
    [JsonPropertyName("backdrop_path")]
    public string? BackdropPath { get; set; }

    /// <summary>Gets or sets the average community vote (0-10).</summary>
    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; set; }

    /// <summary>Gets or sets the TMDb genre identifiers for this show.</summary>
    [JsonPropertyName("genre_ids")]
    public int[] GenreIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the matching Jellyfin library item identifier, if this show was found in the
    /// local library during cache refresh (by IMDb ID, falling back to TMDb ID). Null when the
    /// show is not present in the library, or before cross-referencing has run.
    /// This field is never populated by <see cref="ITMDbApiClient"/> -- only by
    /// <see cref="ITMDbCacheService"/> during a refresh.
    /// </summary>
    [JsonPropertyName("libraryItemId")]
    public Guid? LibraryItemId { get; set; }
}
