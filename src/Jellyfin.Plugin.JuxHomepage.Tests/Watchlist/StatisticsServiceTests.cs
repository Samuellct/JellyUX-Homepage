using Jellyfin.Plugin.JuxHomepage.Watchlist;
using Jellyfin.Plugin.JuxHomepage.Watchlist.Models;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Watchlist;

public sealed class StatisticsServiceTests
{
    [Fact]
    public void GetStatistics_AggregatesCountersFromBothCaches()
    {
        var userId = Guid.NewGuid();

        var progressCacheMock = new Mock<ISeriesProgressCacheService>();
        progressCacheMock.Setup(m => m.GetProgress(userId)).Returns(
        [
            new SeriesProgressEntry { SeriesId = Guid.NewGuid(), WatchedEpisodes = 10, TotalEpisodes = 10 }, // completed
            new SeriesProgressEntry { SeriesId = Guid.NewGuid(), WatchedEpisodes = 3, TotalEpisodes = 8 } // in progress
        ]);

        var historyCacheMock = new Mock<IMovieHistoryCacheService>();
        historyCacheMock.Setup(m => m.GetHistory(userId)).Returns(
        [
            new MovieHistoryEntry { ItemId = Guid.NewGuid(), Name = "Movie A" },
            new MovieHistoryEntry { ItemId = Guid.NewGuid(), Name = "Movie B" },
            new MovieHistoryEntry { ItemId = Guid.NewGuid(), Name = "Movie C" }
        ]);

        var service = new StatisticsService(progressCacheMock.Object, historyCacheMock.Object);

        var stats = service.GetStatistics(userId);

        Assert.Equal(3, stats.MoviesWatched);
        Assert.Equal(2, stats.SeriesTracked);
        Assert.Equal(1, stats.SeriesCompleted);
        Assert.Equal(13, stats.EpisodesWatched);
    }

    [Fact]
    public void GetStatistics_NoHistoryYet_ReturnsAllZeroes()
    {
        var userId = Guid.NewGuid();

        var progressCacheMock = new Mock<ISeriesProgressCacheService>();
        progressCacheMock.Setup(m => m.GetProgress(userId)).Returns([]);

        var historyCacheMock = new Mock<IMovieHistoryCacheService>();
        historyCacheMock.Setup(m => m.GetHistory(userId)).Returns([]);

        var service = new StatisticsService(progressCacheMock.Object, historyCacheMock.Object);

        var stats = service.GetStatistics(userId);

        Assert.Equal(0, stats.MoviesWatched);
        Assert.Equal(0, stats.SeriesTracked);
        Assert.Equal(0, stats.SeriesCompleted);
        Assert.Equal(0, stats.EpisodesWatched);
    }
}
