using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.TMDb;

public sealed class TMDbCacheServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "jux-tmdb-tests-" + Guid.NewGuid());

    private TMDbCacheService BuildService(
        ITMDbApiClient apiClient,
        ILibraryManager libraryManager,
        int refreshIntervalHours = 24)
    {
        var applicationPathsMock = new Mock<IApplicationPaths>();
        applicationPathsMock.Setup(p => p.PluginConfigurationsPath).Returns(_tempDir);

        return new TMDbCacheService(
            applicationPathsMock.Object,
            apiClient,
            libraryManager,
            () => new PluginConfiguration { Cache = new CacheConfig { TMDbRefreshIntervalHours = refreshIntervalHours } },
            NullLogger<TMDbCacheService>.Instance);
    }

    private string CacheFilePath => Path.Combine(_tempDir, "Jellyfin.Plugin.JuxHomepage", "cache", "tmdb", "trending_movies.json");

    // A library manager mock that never matches anything, for tests that only care about the
    // fetch/write/staleness mechanics, not cross-referencing itself.
    private static ILibraryManager NoMatchLibraryManager()
    {
        var mock = new Mock<ILibraryManager>();
        mock.Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>())).Returns([]);
        return mock.Object;
    }

    // -------------------------------------------------------------------------
    // IsStale
    // -------------------------------------------------------------------------

    [Fact]
    public void IsStale_FileAbsent_ReturnsTrue()
    {
        var service = BuildService(new Mock<ITMDbApiClient>().Object, new Mock<ILibraryManager>().Object);

        Assert.True(service.IsStale(TMDbCacheType.TrendingMovies));
    }

    [Fact]
    public async Task IsStale_FreshlyRefreshed_ReturnsFalse()
    {
        var apiClientMock = new Mock<ITMDbApiClient>();
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[new TMDbMovie { Id = 1, Title = "A" }]);
        apiClientMock.Setup(c => c.GetMovieExternalIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var service = BuildService(apiClientMock.Object, NoMatchLibraryManager(), refreshIntervalHours: 24);

        await service.RefreshTrendingMoviesAsync(CancellationToken.None);

        Assert.False(service.IsStale(TMDbCacheType.TrendingMovies));
    }

    [Fact]
    public async Task IsStale_OlderThanConfiguredInterval_ReturnsTrue()
    {
        var apiClientMock = new Mock<ITMDbApiClient>();
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[new TMDbMovie { Id = 1, Title = "A" }]);
        apiClientMock.Setup(c => c.GetMovieExternalIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var service = BuildService(apiClientMock.Object, NoMatchLibraryManager(), refreshIntervalHours: 1);

        await service.RefreshTrendingMoviesAsync(CancellationToken.None);
        File.SetLastWriteTimeUtc(CacheFilePath, DateTime.UtcNow.AddHours(-2));

        Assert.True(service.IsStale(TMDbCacheType.TrendingMovies));
    }

    // -------------------------------------------------------------------------
    // GetTrendingMovies before any refresh
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTrendingMovies_NoRefreshYet_ReturnsEmpty()
    {
        var service = BuildService(new Mock<ITMDbApiClient>().Object, new Mock<ILibraryManager>().Object);

        Assert.Empty(service.GetTrendingMovies());
    }

    // -------------------------------------------------------------------------
    // Cross-referencing
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RefreshTrendingMoviesAsync_ImdbMatchFound_SetsLibraryItemId()
    {
        var libraryItem = new Movie { Id = Guid.NewGuid(), Name = "Inception" };

        var apiClientMock = new Mock<ITMDbApiClient>();
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[new TMDbMovie { Id = 27205, Title = "Inception" }]);
        apiClientMock.Setup(c => c.GetMovieExternalIdsAsync(27205, It.IsAny<CancellationToken>()))
            .ReturnsAsync("tt1375666");

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(
                q => q.HasAnyProviderId != null && q.HasAnyProviderId.ContainsKey("Imdb"))))
            .Returns([libraryItem]);

        var service = BuildService(apiClientMock.Object, libraryManagerMock.Object);

        await service.RefreshTrendingMoviesAsync(CancellationToken.None);
        var result = service.GetTrendingMovies();

        Assert.Single(result);
        Assert.Equal(libraryItem.Id, result[0].LibraryItemId);
    }

    [Fact]
    public async Task RefreshTrendingMoviesAsync_NoImdbMatch_FallsBackToTmdbId()
    {
        var libraryItem = new Movie { Id = Guid.NewGuid(), Name = "Inception" };

        var apiClientMock = new Mock<ITMDbApiClient>();
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[new TMDbMovie { Id = 27205, Title = "Inception" }]);
        apiClientMock.Setup(c => c.GetMovieExternalIdsAsync(27205, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(
                q => q.HasAnyProviderId != null && q.HasAnyProviderId.ContainsKey("Tmdb")
                     && q.HasAnyProviderId["Tmdb"] == "27205")))
            .Returns([libraryItem]);

        var service = BuildService(apiClientMock.Object, libraryManagerMock.Object);

        await service.RefreshTrendingMoviesAsync(CancellationToken.None);
        var result = service.GetTrendingMovies();

        Assert.Single(result);
        Assert.Equal(libraryItem.Id, result[0].LibraryItemId);
    }

    [Fact]
    public async Task RefreshTrendingMoviesAsync_NoMatchAtAll_LibraryItemIdStaysNull()
    {
        var apiClientMock = new Mock<ITMDbApiClient>();
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[new TMDbMovie { Id = 999, Title = "Unknown" }]);
        apiClientMock.Setup(c => c.GetMovieExternalIdsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns([]);

        var service = BuildService(apiClientMock.Object, libraryManagerMock.Object);

        await service.RefreshTrendingMoviesAsync(CancellationToken.None);
        var result = service.GetTrendingMovies();

        Assert.Single(result);
        Assert.Null(result[0].LibraryItemId);
    }

    // -------------------------------------------------------------------------
    // Empty refresh must not clobber a previously-populated cache (regression test: a fetch
    // failure, e.g. an invalid API key producing HTTP 401s, previously overwrote a good cache with
    // an empty one, silently destroying real cross-referenced data).
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RefreshTrendingMoviesAsync_EmptyResultAfterPopulatedCache_PreservesExistingCache()
    {
        var libraryItem = new Movie { Id = Guid.NewGuid(), Name = "Inception" };

        var apiClientMock = new Mock<ITMDbApiClient>();
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[new TMDbMovie { Id = 27205, Title = "Inception" }]);
        apiClientMock.Setup(c => c.GetMovieExternalIdsAsync(27205, It.IsAny<CancellationToken>()))
            .ReturnsAsync("tt1375666");

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns([libraryItem]);

        var service = BuildService(apiClientMock.Object, libraryManagerMock.Object);

        // First refresh succeeds and populates the cache.
        await service.RefreshTrendingMoviesAsync(CancellationToken.None);
        Assert.Single(service.GetTrendingMovies());

        // A subsequent refresh fails (e.g. the configured key became invalid) and the API client
        // degrades to an empty list, per its documented contract.
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[]);

        await service.RefreshTrendingMoviesAsync(CancellationToken.None);

        // The good cache from the first refresh must still be there.
        var result = service.GetTrendingMovies();
        Assert.Single(result);
        Assert.Equal("Inception", result[0].Title);
    }

    // -------------------------------------------------------------------------
    // GetLastRefreshedUtc
    // -------------------------------------------------------------------------

    [Fact]
    public void GetLastRefreshedUtc_NoRefreshYet_ReturnsNull()
    {
        var service = BuildService(new Mock<ITMDbApiClient>().Object, new Mock<ILibraryManager>().Object);

        Assert.Null(service.GetLastRefreshedUtc(TMDbCacheType.TrendingMovies));
    }

    [Fact]
    public async Task GetLastRefreshedUtc_AfterRefresh_ReturnsRecentTimestamp()
    {
        var apiClientMock = new Mock<ITMDbApiClient>();
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[new TMDbMovie { Id = 1, Title = "A" }]);
        apiClientMock.Setup(c => c.GetMovieExternalIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var service = BuildService(apiClientMock.Object, NoMatchLibraryManager());

        await service.RefreshTrendingMoviesAsync(CancellationToken.None);
        var lastRefreshed = service.GetLastRefreshedUtc(TMDbCacheType.TrendingMovies);

        Assert.NotNull(lastRefreshed);
        Assert.True(DateTime.UtcNow - lastRefreshed!.Value < TimeSpan.FromMinutes(1));
    }

    // -------------------------------------------------------------------------
    // RefreshAllAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RefreshAllAsync_CallsAllSevenFetchesAndReportsProgress()
    {
        var apiClientMock = new Mock<ITMDbApiClient>();
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[]);
        apiClientMock.Setup(c => c.GetTrendingShowsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbShow>)[]);
        apiClientMock.Setup(c => c.GetAiringTodayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbShow>)[]);
        apiClientMock.Setup(c => c.GetUpcomingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[]);
        apiClientMock.Setup(c => c.GetTopRatedMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[]);
        apiClientMock.Setup(c => c.GetTopRatedShowsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbShow>)[]);
        apiClientMock.Setup(c => c.GetNowPlayingMoviesAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[]);

        var service = BuildService(apiClientMock.Object, new Mock<ILibraryManager>().Object);
        var progress = new SyncProgress();

        await service.RefreshAllAsync(progress, CancellationToken.None);

        apiClientMock.Verify(c => c.GetTrendingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        apiClientMock.Verify(c => c.GetTrendingShowsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        apiClientMock.Verify(c => c.GetAiringTodayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        apiClientMock.Verify(c => c.GetUpcomingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        apiClientMock.Verify(c => c.GetTopRatedMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        apiClientMock.Verify(c => c.GetTopRatedShowsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        apiClientMock.Verify(c => c.GetNowPlayingMoviesAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(8, progress.Values.Count);
        Assert.Equal(0d, progress.Values[0]);
        Assert.Equal(100d, progress.Values[^1]);
        Assert.True(progress.Values.SequenceEqual(progress.Values.OrderBy(v => v)), "Progress values must be non-decreasing.");
    }

    [Fact]
    public async Task RefreshAllAsync_NullProgress_DoesNotThrow()
    {
        var apiClientMock = new Mock<ITMDbApiClient>();
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[]);
        apiClientMock.Setup(c => c.GetTrendingShowsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbShow>)[]);
        apiClientMock.Setup(c => c.GetAiringTodayAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbShow>)[]);
        apiClientMock.Setup(c => c.GetUpcomingMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[new TMDbMovie { Id = 1, Title = "A" }]);
        apiClientMock.Setup(c => c.GetTopRatedMoviesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[]);
        apiClientMock.Setup(c => c.GetTopRatedShowsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbShow>)[]);
        apiClientMock.Setup(c => c.GetNowPlayingMoviesAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[]);
        apiClientMock.Setup(c => c.GetMovieExternalIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var service = BuildService(apiClientMock.Object, NoMatchLibraryManager());

        await service.RefreshAllAsync(null, CancellationToken.None);

        Assert.False(service.IsStale(TMDbCacheType.UpcomingMovies));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    // Synchronous IProgress<double> test double -- unlike System.Progress<T>, which marshals
    // callbacks via SynchronizationContext.Post/ThreadPool and is not guaranteed to have delivered
    // them by the time an awaited call returns, this reports immediately and deterministically.
    private sealed class SyncProgress : IProgress<double>
    {
        public List<double> Values { get; } = [];

        public void Report(double value) => Values.Add(value);
    }
}
