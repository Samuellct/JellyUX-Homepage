using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.JuxHomepage.IO;
using Jellyfin.Plugin.JuxHomepage.Watchlist;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Watchlist;

public sealed class SeriesProgressCacheServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "jux-series-progress-tests-" + Guid.NewGuid());

    private SeriesProgressCacheService BuildService(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IUserDataManager userDataManager)
    {
        var applicationPathsMock = new Mock<IApplicationPaths>();
        applicationPathsMock.Setup(p => p.DataPath).Returns(_tempDir);

        return new SeriesProgressCacheService(
            applicationPathsMock.Object,
            new FileSystem(),
            userManager,
            libraryManager,
            userDataManager,
            NullLogger<SeriesProgressCacheService>.Instance);
    }

    [Fact]
    public async Task RefreshAllAsync_UserWithPartiallyWatchedSeries_CachesWatchedAndTotalCounts()
    {
        var user = new User("test", "Default", "Default");
        var seriesId = Guid.NewGuid();

        var episode1 = new Episode { Id = Guid.NewGuid(), Name = "E1", SeriesId = seriesId, SeriesName = "My Show" };
        var episode2 = new Episode { Id = Guid.NewGuid(), Name = "E2", SeriesId = seriesId, SeriesName = "My Show" };

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.Users).Returns([user]);
        userManagerMock.Setup(m => m.GetUserById(user.Id)).Returns(user);

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Contains(BaseItemKind.Episode))))
            .Returns([episode1, episode2]);

        var userDataManagerMock = new Mock<IUserDataManager>();
        userDataManagerMock
            .Setup(m => m.GetUserData(user, episode1))
            .Returns(new UserItemData { Key = "k1", Played = true, LastPlayedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        userDataManagerMock
            .Setup(m => m.GetUserData(user, episode2))
            .Returns(new UserItemData { Key = "k2", Played = false });

        var service = BuildService(userManagerMock.Object, libraryManagerMock.Object, userDataManagerMock.Object);

        var refreshed = await service.RefreshAllAsync(CancellationToken.None);
        Assert.True(refreshed);

        var progress = service.GetProgress(user.Id);

        var entry = Assert.Single(progress);
        Assert.Equal(seriesId, entry.SeriesId);
        Assert.Equal("My Show", entry.SeriesName);
        Assert.Equal(1, entry.WatchedEpisodes);
        Assert.Equal(2, entry.TotalEpisodes);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), entry.LastPlayedDate);
    }

    [Fact]
    public async Task RefreshAllAsync_NoWatchedEpisodesForASeries_ExcludesItFromResults()
    {
        var user = new User("test", "Default", "Default");
        var seriesId = Guid.NewGuid();
        var episode = new Episode { Id = Guid.NewGuid(), Name = "E1", SeriesId = seriesId, SeriesName = "Unwatched Show" };

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.Users).Returns([user]);
        userManagerMock.Setup(m => m.GetUserById(user.Id)).Returns(user);

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Contains(BaseItemKind.Episode))))
            .Returns([episode]);

        var userDataManagerMock = new Mock<IUserDataManager>();
        userDataManagerMock.Setup(m => m.GetUserData(user, episode)).Returns(new UserItemData { Key = "k2", Played = false });

        var service = BuildService(userManagerMock.Object, libraryManagerMock.Object, userDataManagerMock.Object);

        await service.RefreshAllAsync(CancellationToken.None);

        Assert.Empty(service.GetProgress(user.Id));
    }

    [Fact]
    public void IsStale_NoRefreshYet_ReturnsTrue()
    {
        var service = BuildService(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IUserDataManager>().Object);

        Assert.True(service.IsStale(Guid.NewGuid()));
    }

    [Fact]
    public async Task RefreshAllAsync_ConcurrentCalls_OnlyOneActuallyRuns()
    {
        var started = new ManualResetEventSlim(false);
        var release = new ManualResetEventSlim(false);

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.Users).Returns(() =>
        {
            started.Set();
            release.Wait(TimeSpan.FromSeconds(5));
            return [];
        });

        var service = BuildService(
            userManagerMock.Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IUserDataManager>().Object);

        var firstCallTask = Task.Run(() => service.RefreshAllAsync(CancellationToken.None));

        var reachedFetch = started.Wait(TimeSpan.FromSeconds(5));
        Assert.True(reachedFetch, "First refresh did not reach the user-enumeration step in time.");

        var secondAcquired = service.TryAcquireRefreshLock();
        Assert.False(secondAcquired, "A second concurrent refresh should not acquire the lock.");

        release.Set();
        var firstResult = await firstCallTask;
        Assert.True(firstResult);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
