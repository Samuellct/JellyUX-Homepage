using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using Jellyfin.Plugin.JuxHomepage.Widgets.Connected;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Connected;

/// <summary>
/// Descriptor and cache-wiring tests for the three remaining connected widgets. Each widget's mock
/// <see cref="ITMDbCacheService"/> only sets up the ONE method that widget should call; calling any
/// other method returns Moq's default (null), which throws when the base class LINQ-processes it --
/// so a miswired <c>GetCachedItems()</c> surfaces as a test failure, not a silent pass.
/// </summary>
public sealed class RemainingConnectedWidgetTests
{
    private static readonly User TestUser = new("test", "Default", "Default");

    // -------------------------------------------------------------------------
    // Descriptors
    // -------------------------------------------------------------------------

    [Fact]
    public void TrendingShows_GetDescriptor_HasExpectedProperties()
    {
        var widget = new TrendingShowsWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            new Mock<ITMDbCacheService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.connected.trending-shows", d.WidgetType);
        Assert.Equal(WidgetCategory.Connected, d.Category);
        Assert.Equal(4, d.MinItems);
        Assert.Equal(WidgetViewMode.Portrait, d.ViewMode);
    }

    [Fact]
    public void AiringToday_GetDescriptor_HasExpectedProperties()
    {
        var widget = new AiringTodayShowsWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            new Mock<ITMDbCacheService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.connected.airing-today", d.WidgetType);
        Assert.Equal(WidgetCategory.Connected, d.Category);
        Assert.Equal(4, d.MinItems);
    }

    [Fact]
    public void UpcomingMovies_GetDescriptor_HasExpectedProperties()
    {
        var widget = new UpcomingMoviesWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            new Mock<ITMDbCacheService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.connected.upcoming-movies", d.WidgetType);
        Assert.Equal(WidgetCategory.Connected, d.Category);
        Assert.Equal(4, d.MinItems);
    }

    // -------------------------------------------------------------------------
    // Cache method wiring
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TrendingShows_ReadsFromGetTrendingShows()
    {
        var cacheServiceMock = new Mock<ITMDbCacheService>();
        cacheServiceMock.Setup(c => c.GetTrendingShows()).Returns([]);

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(TestUser);

        var widget = new TrendingShowsWidget(
            userManagerMock.Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            cacheServiceMock.Object);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), Limit = 20 },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        cacheServiceMock.Verify(c => c.GetTrendingShows(), Times.AtLeastOnce);
        cacheServiceMock.Verify(c => c.GetAiringToday(), Times.Never);
    }

    [Fact]
    public async Task AiringToday_ReadsFromGetAiringToday()
    {
        var cacheServiceMock = new Mock<ITMDbCacheService>();
        cacheServiceMock.Setup(c => c.GetAiringToday()).Returns([]);

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(TestUser);

        var widget = new AiringTodayShowsWidget(
            userManagerMock.Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            cacheServiceMock.Object);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), Limit = 20 },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        cacheServiceMock.Verify(c => c.GetAiringToday(), Times.AtLeastOnce);
        cacheServiceMock.Verify(c => c.GetTrendingShows(), Times.Never);
    }

    [Fact]
    public async Task UpcomingMovies_ReadsFromGetUpcomingMovies()
    {
        var cacheServiceMock = new Mock<ITMDbCacheService>();
        cacheServiceMock.Setup(c => c.GetUpcomingMovies()).Returns([]);

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(TestUser);

        var widget = new UpcomingMoviesWidget(
            userManagerMock.Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            cacheServiceMock.Object);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), Limit = 20 },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        cacheServiceMock.Verify(c => c.GetUpcomingMovies(), Times.AtLeastOnce);
        cacheServiceMock.Verify(c => c.GetTrendingMovies(), Times.Never);
    }
}
