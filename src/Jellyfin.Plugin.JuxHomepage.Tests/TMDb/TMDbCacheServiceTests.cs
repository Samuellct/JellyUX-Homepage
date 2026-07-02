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
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[]);

        var service = BuildService(apiClientMock.Object, new Mock<ILibraryManager>().Object, refreshIntervalHours: 24);

        await service.RefreshTrendingMoviesAsync(CancellationToken.None);

        Assert.False(service.IsStale(TMDbCacheType.TrendingMovies));
    }

    [Fact]
    public async Task IsStale_OlderThanConfiguredInterval_ReturnsTrue()
    {
        var apiClientMock = new Mock<ITMDbApiClient>();
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TMDbMovie>)[]);

        var service = BuildService(apiClientMock.Object, new Mock<ILibraryManager>().Object, refreshIntervalHours: 1);

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
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<CancellationToken>()))
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
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<CancellationToken>()))
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
        apiClientMock.Setup(c => c.GetTrendingMoviesAsync(It.IsAny<CancellationToken>()))
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

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
