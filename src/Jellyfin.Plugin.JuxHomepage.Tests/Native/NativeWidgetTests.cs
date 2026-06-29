using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using Jellyfin.Plugin.JuxHomepage.Widgets.Native;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.TV;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Native;

public sealed class NativeWidgetTests
{
    // Returns a mock IUserManager whose GetUserById always returns null.
    private static Mock<IUserManager> UserManagerReturningNull()
    {
        var mock = new Mock<IUserManager>();
        mock.Setup(m => m.GetUserById(It.IsAny<Guid>()))
            .Returns((User?)null);
        return mock;
    }

    // -------------------------------------------------------------------------
    // Descriptor tests
    // -------------------------------------------------------------------------

    [Fact]
    public void ContinueWatching_GetDescriptor_HasExpectedProperties()
    {
        var widget = new ContinueWatchingWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.native.continue-watching", d.WidgetType);
        Assert.Equal(WidgetCategory.Native, d.Category);
        Assert.Equal(WidgetViewMode.Landscape, d.ViewMode);
        Assert.Equal(1, d.MinItems);
        Assert.Null(d.Route);
    }

    [Fact]
    public void NextUp_GetDescriptor_HasExpectedProperties()
    {
        var widget = new NextUpWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            new Mock<ITVSeriesManager>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.native.next-up", d.WidgetType);
        Assert.Equal(WidgetCategory.Native, d.Category);
        Assert.Equal(WidgetViewMode.Landscape, d.ViewMode);
        Assert.Equal(1, d.MinItems);
        Assert.Equal("nextup", d.Route);
    }

    [Fact]
    public void RecentlyAddedMovies_GetDescriptor_HasExpectedProperties()
    {
        var widget = new RecentlyAddedMoviesWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.native.recently-added-movies", d.WidgetType);
        Assert.Equal(WidgetCategory.Native, d.Category);
        Assert.Equal(WidgetViewMode.Portrait, d.ViewMode);
        Assert.Equal(4, d.MinItems);
        Assert.Equal("movies", d.Route);
    }

    [Fact]
    public void RecentlyAddedShows_GetDescriptor_HasExpectedProperties()
    {
        var widget = new RecentlyAddedShowsWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.native.recently-added-shows", d.WidgetType);
        Assert.Equal(WidgetCategory.Native, d.Category);
        Assert.Equal(WidgetViewMode.Portrait, d.ViewMode);
        Assert.Equal(4, d.MinItems);
        Assert.Equal("tvshows", d.Route);
    }

    [Fact]
    public void MyMedia_GetDescriptor_HasExpectedProperties()
    {
        var widget = new MyMediaWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            new Mock<IUserViewManager>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.native.my-media", d.WidgetType);
        Assert.Equal(WidgetCategory.Native, d.Category);
        Assert.Equal(WidgetViewMode.Landscape, d.ViewMode);
        Assert.Equal(1, d.MinItems);
        Assert.Null(d.Route);
    }

    // -------------------------------------------------------------------------
    // CreateInstances test
    // -------------------------------------------------------------------------

    [Fact]
    public void ContinueWatching_CreateInstances_ReturnsSameInstance()
    {
        var widget = new ContinueWatchingWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var instances = widget.CreateInstances(Guid.NewGuid(), new WidgetInstanceConfig(), 1).ToList();

        var single = Assert.Single(instances);
        Assert.Same(widget, single);
    }

    // -------------------------------------------------------------------------
    // User-null defensive tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ContinueWatching_UserNotFound_ReturnsEmpty()
    {
        var widget = new ContinueWatchingWidget(
            UserManagerReturningNull().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task NextUp_UserNotFound_ReturnsEmpty()
    {
        var widget = new NextUpWidget(
            UserManagerReturningNull().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            new Mock<ITVSeriesManager>().Object);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task RecentlyAddedMovies_UserNotFound_ReturnsEmpty()
    {
        var widget = new RecentlyAddedMoviesWidget(
            UserManagerReturningNull().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task RecentlyAddedShows_UserNotFound_ReturnsEmpty()
    {
        var widget = new RecentlyAddedShowsWidget(
            UserManagerReturningNull().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task MyMedia_UserNotFound_ReturnsEmpty()
    {
        var widget = new MyMediaWidget(
            UserManagerReturningNull().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            new Mock<IUserViewManager>().Object);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        Assert.Empty(result.Items);
    }

    // -------------------------------------------------------------------------
    // NativeWidgetDefaults tests
    // -------------------------------------------------------------------------

    [Fact]
    public void NativeWidgetDefaults_Build_ReturnsFiveEntries()
    {
        var configs = NativeWidgetDefaults.Build();
        Assert.Equal(5, configs.Length);
    }

    [Fact]
    public void NativeWidgetDefaults_Build_AllEnabled()
    {
        var configs = NativeWidgetDefaults.Build();
        Assert.All(configs, c => Assert.True(c.Enabled));
    }

    [Fact]
    public void NativeWidgetDefaults_Build_OrdersAreAscendingAndUnique()
    {
        var orders = NativeWidgetDefaults.Build().Select(c => c.Order).ToArray();
        Assert.Equal(orders.Length, orders.Distinct().Count());
        Assert.Equal(orders, orders.OrderBy(o => o).ToArray());
    }

    [Fact]
    public void NativeWidgetDefaults_Build_MinItemsMatchWidgetDefaults()
    {
        var configs = NativeWidgetDefaults.Build();

        Assert.Equal(1, configs.Single(c => c.WidgetType == "jux.native.continue-watching").MinItems);
        Assert.Equal(1, configs.Single(c => c.WidgetType == "jux.native.next-up").MinItems);
        Assert.Equal(4, configs.Single(c => c.WidgetType == "jux.native.recently-added-movies").MinItems);
        Assert.Equal(4, configs.Single(c => c.WidgetType == "jux.native.recently-added-shows").MinItems);
        Assert.Equal(1, configs.Single(c => c.WidgetType == "jux.native.my-media").MinItems);
    }
}
