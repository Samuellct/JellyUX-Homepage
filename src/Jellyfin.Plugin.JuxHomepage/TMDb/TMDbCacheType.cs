namespace Jellyfin.Plugin.JuxHomepage.TMDb;

/// <summary>
/// Identifies one of the four TMDb data sets persisted to disk by <see cref="ITMDbCacheService"/>.
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
    UpcomingMovies
}
