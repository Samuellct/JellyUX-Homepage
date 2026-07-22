using Jellyfin.Plugin.JuxHomepage.Watchlist.Models;

namespace Jellyfin.Plugin.JuxHomepage.Watchlist;

/// <summary>
/// Computes the aggregate watch-history counters for the Statistics view (TODO_V3.md Phase 6.3),
/// derived entirely from <see cref="ISeriesProgressCacheService"/>/<see cref="IMovieHistoryCacheService"/>
/// -- no new library collection, per the phase's own scope ("dérivé des données existantes").
/// </summary>
public interface IStatisticsService
{
    /// <summary>Computes the watch-history counters for a user.</summary>
    /// <param name="userId">The user to compute statistics for.</param>
    /// <returns>The aggregate counters.</returns>
    WatchingStatistics GetStatistics(Guid userId);
}
