namespace Jellyfin.Plugin.JuxHomepage.TMDb;

/// <summary>
/// Identifies one of the fixed TMDb data sets persisted to disk by <see cref="ITMDbCacheService"/>.
/// </summary>
public enum TMDbCacheType
{
    /// <summary>Weekly trending movies.</summary>
    TrendingMovies,

    /// <summary>Weekly trending TV shows.</summary>
    TrendingShows,

    /// <summary>TV shows airing today.</summary>
    AiringToday,

    /// <summary>Upcoming movie releases.</summary>
    UpcomingMovies,

    /// <summary>All-time top rated movies (TMDb's own vote_count.gte-filtered ranking).</summary>
    TopRatedMovies,

    /// <summary>All-time top rated TV shows.</summary>
    TopRatedShows,

    /// <summary>Movies currently in theatres, optionally scoped to a configured region.</summary>
    NowPlayingMovies
}
