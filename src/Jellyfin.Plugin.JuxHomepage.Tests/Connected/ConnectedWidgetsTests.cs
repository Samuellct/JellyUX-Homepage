using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using Jellyfin.Plugin.JuxHomepage.Widgets.Connected;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Connected;

/// <summary>
/// Descriptor and cache-wiring tests for the "thin" connected widgets -- each one simply reads from
/// its own <see cref="ITMDbCacheService"/> accessor. Each widget's mock cache service only sets up
/// the ONE method that widget should call; calling any other method returns Moq's default (null),
/// which throws when the base class LINQ-processes it -- so a miswired cache accessor (e.g. copying
/// GetTrendingShows into a widget that should read GetAiringToday) surfaces as a test failure, not a
/// silent pass. Organized by widget below (was split by chronological order of writing across
/// NewConnectedWidgetTests.cs / RemainingConnectedWidgetTests.cs prior to Phase 2 of TODO_V2.md).
/// </summary>
public sealed class ConnectedWidgetsTests
{
    private static readonly User TestUser = new("test", "Default", "Default");

    // -------------------------------------------------------------------------
    // TrendingShows
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

    // -------------------------------------------------------------------------
    // TopRatedMovies
    // -------------------------------------------------------------------------

    [Fact]
    public void TopRatedMovies_GetDescriptor_HasExpectedProperties()
    {
        var widget = new TopRatedMoviesWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            new Mock<ITMDbCacheService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.connected.top-rated-movies", d.WidgetType);
        Assert.Equal(WidgetCategory.Connected, d.Category);
        Assert.Equal(WidgetViewMode.Portrait, d.ViewMode);
    }

    [Fact]
    public async Task TopRatedMovies_ReadsFromGetTopRatedMovies()
    {
        var cacheServiceMock = new Mock<ITMDbCacheService>();
        cacheServiceMock.Setup(c => c.GetTopRatedMovies()).Returns([]);

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(TestUser);

        var widget = new TopRatedMoviesWidget(
            userManagerMock.Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            cacheServiceMock.Object);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), Limit = 20 },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        cacheServiceMock.Verify(c => c.GetTopRatedMovies(), Times.AtLeastOnce);
        cacheServiceMock.Verify(c => c.GetTrendingMovies(), Times.Never);
    }

    // -------------------------------------------------------------------------
    // TopRatedShows
    // -------------------------------------------------------------------------

    [Fact]
    public void TopRatedShows_GetDescriptor_HasExpectedProperties()
    {
        var widget = new TopRatedShowsWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            new Mock<ITMDbCacheService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.connected.top-rated-shows", d.WidgetType);
        Assert.Equal(WidgetCategory.Connected, d.Category);
    }

    [Fact]
    public async Task TopRatedShows_ReadsFromGetTopRatedShows()
    {
        var cacheServiceMock = new Mock<ITMDbCacheService>();
        cacheServiceMock.Setup(c => c.GetTopRatedShows()).Returns([]);

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(TestUser);

        var widget = new TopRatedShowsWidget(
            userManagerMock.Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            cacheServiceMock.Object);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), Limit = 20 },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        cacheServiceMock.Verify(c => c.GetTopRatedShows(), Times.AtLeastOnce);
        cacheServiceMock.Verify(c => c.GetTrendingShows(), Times.Never);
    }

    // -------------------------------------------------------------------------
    // NowPlayingMovies
    // -------------------------------------------------------------------------

    [Fact]
    public void NowPlayingMovies_GetDescriptor_HasExpectedProperties()
    {
        var widget = new NowPlayingMoviesWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            new Mock<ITMDbCacheService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.connected.now-playing-movies", d.WidgetType);
        Assert.Equal(WidgetCategory.Connected, d.Category);
    }

    [Fact]
    public async Task NowPlayingMovies_ReadsFromGetNowPlayingMovies()
    {
        var cacheServiceMock = new Mock<ITMDbCacheService>();
        cacheServiceMock.Setup(c => c.GetNowPlayingMovies()).Returns([]);

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(TestUser);

        var widget = new NowPlayingMoviesWidget(
            userManagerMock.Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            cacheServiceMock.Object);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), Limit = 20 },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        cacheServiceMock.Verify(c => c.GetNowPlayingMovies(), Times.AtLeastOnce);
        cacheServiceMock.Verify(c => c.GetTopRatedMovies(), Times.Never);
    }

    // -------------------------------------------------------------------------
    // AiringToday
    // -------------------------------------------------------------------------

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
}
