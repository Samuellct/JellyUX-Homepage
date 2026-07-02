using Jellyfin.Plugin.JuxHomepage.TMDb.Models;

namespace Jellyfin.Plugin.JuxHomepage.TMDb;

/// <summary>
/// Reads and refreshes the on-disk TMDb data cache. Each refresh calls <see cref="ITMDbApiClient"/>
/// and cross-references the results against the local Jellyfin library (by IMDb ID, falling back
/// to TMDb ID), so cached items already know whether -- and where -- they exist locally.
/// </summary>
public interface ITMDbCacheService
{
    /// <summary>Reads the cached weekly trending movies.</summary>
    /// <returns>The cached movies, or an empty list if no refresh has completed yet.</returns>
    IReadOnlyList<TMDbMovie> GetTrendingMovies();

    /// <summary>Reads the cached weekly trending TV shows.</summary>
    /// <returns>The cached shows, or an empty list if no refresh has completed yet.</returns>
    IReadOnlyList<TMDbShow> GetTrendingShows();

    /// <summary>Reads the cached TV shows airing today.</summary>
    /// <returns>The cached shows, or an empty list if no refresh has completed yet.</returns>
    IReadOnlyList<TMDbShow> GetAiringToday();

    /// <summary>Reads the cached upcoming movie releases.</summary>
    /// <returns>The cached movies, or an empty list if no refresh has completed yet.</returns>
    IReadOnlyList<TMDbMovie> GetUpcomingMovies();

    /// <summary>Reads the cached all-time top rated movies.</summary>
    /// <returns>The cached movies, or an empty list if no refresh has completed yet.</returns>
    IReadOnlyList<TMDbMovie> GetTopRatedMovies();

    /// <summary>Reads the cached all-time top rated TV shows.</summary>
    /// <returns>The cached shows, or an empty list if no refresh has completed yet.</returns>
    IReadOnlyList<TMDbShow> GetTopRatedShows();

    /// <summary>Reads the cached movies currently in theatres.</summary>
    /// <returns>The cached movies, or an empty list if no refresh has completed yet.</returns>
    IReadOnlyList<TMDbMovie> GetNowPlayingMovies();

    /// <summary>Refreshes the trending movies cache from TMDb, cross-referencing the local library.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshTrendingMoviesAsync(CancellationToken cancellationToken);

    /// <summary>Refreshes the trending shows cache from TMDb, cross-referencing the local library.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshTrendingShowsAsync(CancellationToken cancellationToken);

    /// <summary>Refreshes the airing-today cache from TMDb, cross-referencing the local library.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshAiringTodayAsync(CancellationToken cancellationToken);

    /// <summary>Refreshes the upcoming-movies cache from TMDb, cross-referencing the local library.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshUpcomingMoviesAsync(CancellationToken cancellationToken);

    /// <summary>Refreshes the top-rated-movies cache from TMDb, cross-referencing the local library.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshTopRatedMoviesAsync(CancellationToken cancellationToken);

    /// <summary>Refreshes the top-rated-shows cache from TMDb, cross-referencing the local library.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshTopRatedShowsAsync(CancellationToken cancellationToken);

    /// <summary>Refreshes the now-playing-movies cache from TMDb, cross-referencing the local library.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshNowPlayingMoviesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns whether the given cache type is missing or older than the configured refresh
    /// interval (<see cref="Configuration.CacheConfig.TMDbRefreshIntervalHours"/>).
    /// </summary>
    /// <param name="type">The cache type to check.</param>
    /// <returns>True if the cache is absent or stale; otherwise false.</returns>
    bool IsStale(TMDbCacheType type);

    /// <summary>
    /// Returns the UTC timestamp of the last successful refresh for the given cache type, for
    /// display in the admin UI.
    /// </summary>
    /// <param name="type">The cache type to check.</param>
    /// <returns>The last refresh timestamp, or null if the cache has never been refreshed.</returns>
    DateTime? GetLastRefreshedUtc(TMDbCacheType type);

    /// <summary>
    /// Refreshes all fixed TMDb cache types in sequence. Each individual refresh is already
    /// fault-tolerant (a failure is logged and does not abort the others). Shared by the daily
    /// scheduled task and the admin "Refresh now" action so neither duplicates the sequence.
    /// </summary>
    /// <param name="progress">
    /// Optional progress reporter, updated as each refresh completes. Pass null when progress
    /// reporting is not needed (e.g. a fire-and-forget trigger).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshAllAsync(IProgress<double>? progress, CancellationToken cancellationToken);
}
