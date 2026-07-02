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

    /// <summary>
    /// Returns whether the given cache type is missing or older than the configured refresh
    /// interval (<see cref="Configuration.CacheConfig.TMDbRefreshIntervalHours"/>).
    /// </summary>
    /// <param name="type">The cache type to check.</param>
    /// <returns>True if the cache is absent or stale; otherwise false.</returns>
    bool IsStale(TMDbCacheType type);
}
