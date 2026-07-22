using Jellyfin.Data.Enums;
using Jellyfin.Plugin.JuxHomepage.IO;
using Jellyfin.Plugin.JuxHomepage.Library;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Library;

public sealed class CollectionsIndexCacheServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "jux-collections-index-tests-" + Guid.NewGuid());

    private CollectionsIndexCacheService BuildService(ILibraryManager libraryManager)
    {
        var applicationPathsMock = new Mock<IApplicationPaths>();
        applicationPathsMock.Setup(p => p.DataPath).Returns(_tempDir);

        return new CollectionsIndexCacheService(
            applicationPathsMock.Object,
            new FileSystem(),
            libraryManager,
            NullLogger<CollectionsIndexCacheService>.Instance);
    }

    [Fact]
    public async Task RefreshAsync_MovieBelongingToOneCollection_CachesReverseIndex()
    {
        var boxSet = new BoxSet { Id = Guid.NewGuid(), Name = "Trilogy" };
        var movie = new Movie { Id = Guid.NewGuid(), Name = "Part One" };
        boxSet.LinkedChildren = [new LinkedChild { ItemId = movie.Id }];

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Contains(BaseItemKind.BoxSet))))
            .Returns([boxSet]);
        libraryManagerMock
            .Setup(m => m.GetItemById(movie.Id))
            .Returns(movie);

        BaseItem.LibraryManager = libraryManagerMock.Object;

        var service = BuildService(libraryManagerMock.Object);

        var refreshed = await service.RefreshAsync(CancellationToken.None);
        Assert.True(refreshed);

        var collections = service.GetCollectionsFor(movie.Id);

        var collectionRef = Assert.Single(collections);
        Assert.Equal(boxSet.Id, collectionRef.CollectionId);
        Assert.Equal("Trilogy", collectionRef.CollectionName);
    }

    [Fact]
    public void GetCollectionsFor_ItemNotInAnyCollection_ReturnsEmpty()
    {
        var service = BuildService(new Mock<ILibraryManager>().Object);

        Assert.Empty(service.GetCollectionsFor(Guid.NewGuid()));
    }

    [Fact]
    public void IsStale_NoRefreshYet_ReturnsTrue()
    {
        var service = BuildService(new Mock<ILibraryManager>().Object);

        Assert.True(service.IsStale());
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentCalls_OnlyOneActuallyRuns()
    {
        var started = new ManualResetEventSlim(false);
        var release = new ManualResetEventSlim(false);

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Contains(BaseItemKind.BoxSet))))
            .Returns(() =>
            {
                started.Set();
                release.Wait(TimeSpan.FromSeconds(5));
                return [];
            });

        var service = BuildService(libraryManagerMock.Object);

        var firstCallTask = Task.Run(() => service.RefreshAsync(CancellationToken.None));

        var reachedFetch = started.Wait(TimeSpan.FromSeconds(5));
        Assert.True(reachedFetch, "First refresh did not reach the BoxSet-enumeration step in time.");

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
