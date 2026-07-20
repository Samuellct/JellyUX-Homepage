using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using Jellyfin.Plugin.JuxHomepage.Widgets.Connected;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Connected;

public sealed class DiscoverMoviesWidgetTests
{
    private static readonly User TestUser = new("test", "Default", "Default");

    [Fact]
    public void GetDescriptor_HasExpectedProperties()
    {
        var widget = new DiscoverMoviesWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            new Mock<ITMDbCacheService>().Object,
            NullLogger<DiscoverMoviesWidget>.Instance);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.connected.discover-movies", d.WidgetType);
        Assert.Equal(WidgetCategory.Connected, d.Category);
    }

    [Fact]
    public async Task GetItemsAsync_ReadsCacheForItsOwnInstanceId()
    {
        var instanceId = Guid.NewGuid().ToString();
        var otherInstanceId = Guid.NewGuid().ToString();

        var cacheServiceMock = new Mock<ITMDbCacheService>();
        cacheServiceMock.Setup(c => c.GetDiscoverMovies(instanceId)).Returns([]);

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(TestUser);

        var widget = new DiscoverMoviesWidget(
            userManagerMock.Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            cacheServiceMock.Object,
            NullLogger<DiscoverMoviesWidget>.Instance);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), Limit = 20, AdditionalData = instanceId },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        cacheServiceMock.Verify(c => c.GetDiscoverMovies(instanceId), Times.AtLeastOnce);
        cacheServiceMock.Verify(c => c.GetDiscoverMovies(otherInstanceId), Times.Never);
    }

    [Fact]
    public async Task GetItemsAsync_NoAdditionalData_ReturnsEmptyWithoutCallingCache()
    {
        var cacheServiceMock = new Mock<ITMDbCacheService>();

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(TestUser);

        var widget = new DiscoverMoviesWidget(
            userManagerMock.Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            cacheServiceMock.Object,
            NullLogger<DiscoverMoviesWidget>.Instance);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), Limit = 20, AdditionalData = null },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        cacheServiceMock.Verify(c => c.GetDiscoverMovies(It.IsAny<string>()), Times.Never);
    }
}
