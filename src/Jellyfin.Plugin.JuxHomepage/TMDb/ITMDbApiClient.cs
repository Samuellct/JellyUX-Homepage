using Jellyfin.Plugin.JuxHomepage.TMDb.Models;

namespace Jellyfin.Plugin.JuxHomepage.TMDb;

/// <summary>
/// Client for TMDb (The Movie Database) API v3 list and external-ids endpoints.
/// All methods degrade gracefully: a missing API key, an invalid key (HTTP 401), or a network
/// failure (after one retry) results in an empty list or null, never an exception.
/// </summary>
public interface ITMDbApiClient
{
    /// <summary>Fetches the current weekly trending movies.</summary>
    /// <param name="pages">
    /// Number of TMDb result pages to fetch and concatenate (1 page = up to 20 items). Clamped to
    /// 1-5 by the caller/config; fetching stops early if a page returns no results.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The trending movies, or an empty list if the key is missing/invalid or the request failed.</returns>
    Task<IReadOnlyList<TMDbMovie>> GetTrendingMoviesAsync(int pages, CancellationToken cancellationToken);

    /// <summary>Fetches the current weekly trending TV shows.</summary>
    /// <param name="pages">Number of TMDb result pages to fetch and concatenate (1 page = up to 20 items).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The trending shows, or an empty list if the key is missing/invalid or the request failed.</returns>
    Task<IReadOnlyList<TMDbShow>> GetTrendingShowsAsync(int pages, CancellationToken cancellationToken);

    /// <summary>Fetches TV shows airing today.</summary>
    /// <param name="pages">Number of TMDb result pages to fetch and concatenate (1 page = up to 20 items).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The airing-today shows, or an empty list if the key is missing/invalid or the request failed.</returns>
    Task<IReadOnlyList<TMDbShow>> GetAiringTodayAsync(int pages, CancellationToken cancellationToken);

    /// <summary>Fetches upcoming movie releases.</summary>
    /// <param name="pages">Number of TMDb result pages to fetch and concatenate (1 page = up to 20 items).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The upcoming movies, or an empty list if the key is missing/invalid or the request failed.</returns>
    Task<IReadOnlyList<TMDbMovie>> GetUpcomingMoviesAsync(int pages, CancellationToken cancellationToken);

    /// <summary>Fetches all-time top rated movies.</summary>
    /// <param name="pages">Number of TMDb result pages to fetch and concatenate (1 page = up to 20 items).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The top rated movies, or an empty list if the key is missing/invalid or the request failed.</returns>
    Task<IReadOnlyList<TMDbMovie>> GetTopRatedMoviesAsync(int pages, CancellationToken cancellationToken);

    /// <summary>Fetches all-time top rated TV shows.</summary>
    /// <param name="pages">Number of TMDb result pages to fetch and concatenate (1 page = up to 20 items).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The top rated shows, or an empty list if the key is missing/invalid or the request failed.</returns>
    Task<IReadOnlyList<TMDbShow>> GetTopRatedShowsAsync(int pages, CancellationToken cancellationToken);

    /// <summary>Fetches movies currently in theatres.</summary>
    /// <param name="pages">Number of TMDb result pages to fetch and concatenate (1 page = up to 20 items).</param>
    /// <param name="region">
    /// Optional ISO 3166-1 region code (e.g. "FR") to scope results to a specific country's theatrical
    /// releases. Null uses TMDb's own default.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The now-playing movies, or an empty list if the key is missing/invalid or the request failed.</returns>
    Task<IReadOnlyList<TMDbMovie>> GetNowPlayingMoviesAsync(int pages, string? region, CancellationToken cancellationToken);

    /// <summary>Fetches the list of ISO 3166-1 countries known to TMDb, for the region dropdown.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The countries, or an empty list if the key is missing/invalid or the request failed.</returns>
    Task<IReadOnlyList<TMDbCountry>> GetCountriesAsync(CancellationToken cancellationToken);

    /// <summary>Fetches the IMDb identifier for a TMDb movie, used for library cross-referencing.</summary>
    /// <param name="tmdbId">The TMDb movie identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The IMDb identifier (e.g. "tt1375666"), or null if unknown/unavailable.</returns>
    Task<string?> GetMovieExternalIdsAsync(int tmdbId, CancellationToken cancellationToken);

    /// <summary>Fetches the IMDb identifier for a TMDb TV show, used for library cross-referencing.</summary>
    /// <param name="tmdbId">The TMDb show identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The IMDb identifier (e.g. "tt1234567"), or null if unknown/unavailable.</returns>
    Task<string?> GetShowExternalIdsAsync(int tmdbId, CancellationToken cancellationToken);
}
