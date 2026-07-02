using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JuxHomepage.TMDb.Models;

/// <summary>
/// Generic paged response envelope returned by every TMDb list endpoint used by this plugin
/// (trending movies/shows, airing today, upcoming movies). The shape is identical across all
/// four endpoints, so a single generic type is reused rather than one wrapper per endpoint.
/// </summary>
/// <typeparam name="T">The item type contained in <see cref="Results"/> (e.g. <see cref="TMDbMovie"/>).</typeparam>
public sealed class TMDbTrending<T>
{
    /// <summary>Gets or sets the current page number.</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>Gets or sets the items on this page.</summary>
    [JsonPropertyName("results")]
    public IReadOnlyList<T> Results { get; set; } = [];

    /// <summary>Gets or sets the total number of pages available.</summary>
    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    /// <summary>Gets or sets the total number of results across all pages.</summary>
    [JsonPropertyName("total_results")]
    public int TotalResults { get; set; }
}
