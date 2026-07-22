using Jellyfin.Plugin.JuxHomepage.Watchlist.Models;

namespace Jellyfin.Plugin.JuxHomepage.Watchlist;

/// <inheritdoc cref="IStatisticsService"/>
public sealed class StatisticsService : IStatisticsService
{
    private readonly ISeriesProgressCacheService _seriesProgressCache;
    private readonly IMovieHistoryCacheService _movieHistoryCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatisticsService"/> class.
    /// </summary>
    /// <param name="seriesProgressCache">Series Progress disk cache.</param>
    /// <param name="movieHistoryCache">Movie History disk cache.</param>
    public StatisticsService(ISeriesProgressCacheService seriesProgressCache, IMovieHistoryCacheService movieHistoryCache)
    {
        _seriesProgressCache = seriesProgressCache;
        _movieHistoryCache = movieHistoryCache;
    }

    /// <inheritdoc/>
    public WatchingStatistics GetStatistics(Guid userId)
    {
        var progress = _seriesProgressCache.GetProgress(userId);
        var history = _movieHistoryCache.GetHistory(userId);

        return new WatchingStatistics
        {
            MoviesWatched = history.Count,
            SeriesTracked = progress.Count,
            SeriesCompleted = progress.Count(e => e.WatchedEpisodes == e.TotalEpisodes),
            EpisodesWatched = progress.Sum(e => e.WatchedEpisodes)
        };
    }
}
