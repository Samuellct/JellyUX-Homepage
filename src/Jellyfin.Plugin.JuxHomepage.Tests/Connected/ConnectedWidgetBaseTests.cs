using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using Jellyfin.Plugin.JuxHomepage.Widgets.Connected;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Connected;

/// <summary>
/// Tests the generic mechanics shared by every <see cref="Widgets.Connected.ConnectedWidgetBase{T}"/>
/// subclass (descriptor shape, pagination, filtering of unmatched/deleted library items, unknown
/// user) -- not Trending-specific behavior. <see cref="TrendingMoviesWidget"/> is used only as a
/// concrete stand-in to exercise the shared base class; per-widget cache wiring is covered in
/// <c>ConnectedWidgetsTests.cs</c> instead.
/// </summary>
public sealed class ConnectedWidgetBaseTests
{
    private static TrendingMoviesWidget BuildWidget(
        IReadOnlyList<TMDbMovie> cachedMovies,
        Mock<IUserManager>? userManagerMock = null,
        Mock<ILibraryManager>? libraryManagerMock = null,
        Mock<IDtoService>? dtoServiceMock = null)
    {
        var cacheServiceMock = new Mock<ITMDbCacheService>();
        cacheServiceMock.Setup(c => c.GetTrendingMovies()).Returns(cachedMovies);

        userManagerMock ??= new Mock<IUserManager>();
        libraryManagerMock ??= new Mock<ILibraryManager>();

        // Only apply the default "return empty" setup when the caller didn't supply their own
        // mock -- re-applying it here would shadow a caller-configured Callback (Moq resolves
        // multiple matching setups by "last configured wins").
        dtoServiceMock ??= TestMocks.DtoServiceReturningEmpty();

        return new TrendingMoviesWidget(
            userManagerMock.Object,
            libraryManagerMock.Object,
            dtoServiceMock.Object,
            cacheServiceMock.Object);
    }

    // -------------------------------------------------------------------------
    // Descriptor
    // -------------------------------------------------------------------------

    [Fact]
    public void GetDescriptor_HasExpectedProperties()
    {
        var widget = BuildWidget([]);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.connected.trending-movies", d.WidgetType);
        Assert.Equal(WidgetCategory.Connected, d.Category);
        Assert.Equal(4, d.MinItems);
        Assert.Equal(WidgetViewMode.Portrait, d.ViewMode);
        Assert.Null(d.Route);
    }

    [Fact]
    public void CreateInstances_ReturnsSameInstance()
    {
        var widget = BuildWidget([]);

        var instances = widget.CreateInstances(Guid.NewGuid(), new WidgetInstanceConfig(), 1).ToList();

        var single = Assert.Single(instances);
        Assert.Same(widget, single);
    }

    // -------------------------------------------------------------------------
    // GetItemsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetItemsAsync_EmptyCache_ReturnsEmpty()
    {
        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(TestMocks.DefaultUser());

        var widget = BuildWidget([], userManagerMock);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), Limit = 20 },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetItemsAsync_UnknownUser_ReturnsEmpty()
    {
        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns((User?)null);

        var widget = BuildWidget(
            [new TMDbMovie { Id = 1, Title = "A", LibraryItemId = Guid.NewGuid() }],
            userManagerMock);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), Limit = 20 },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
    }

    [Fact]
    public async Task GetItemsAsync_UnmatchedItemsAreExcluded_TotalRecordCountReflectsOnlyMatched()
    {
        var user = TestMocks.DefaultUser();
        var matchedId1 = Guid.NewGuid();
        var matchedId2 = Guid.NewGuid();
        var libraryItem1 = new Movie { Id = matchedId1, Name = "Owned 1" };
        var libraryItem2 = new Movie { Id = matchedId2, Name = "Owned 2" };

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(user);

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock.Setup(m => m.GetItemById(matchedId1)).Returns(libraryItem1);
        libraryManagerMock.Setup(m => m.GetItemById(matchedId2)).Returns(libraryItem2);

        IReadOnlyList<BaseItem>? capturedItems = null;
        var dtoServiceMock = new Mock<IDtoService>();
        dtoServiceMock
            .Setup(m => m.GetBaseItemDtos(
                It.IsAny<IReadOnlyList<BaseItem>>(),
                It.IsAny<DtoOptions>(),
                It.IsAny<User>(),
                It.IsAny<BaseItem>()))
            .Callback<IReadOnlyList<BaseItem>, DtoOptions, User, BaseItem>((items, _, _, _) => capturedItems = items)
            .Returns([]);

        var cachedMovies = new List<TMDbMovie>
        {
            new() { Id = 1, Title = "Not owned", LibraryItemId = null },
            new() { Id = 2, Title = "Owned 1", LibraryItemId = matchedId1 },
            new() { Id = 3, Title = "Also not owned", LibraryItemId = null },
            new() { Id = 4, Title = "Owned 2", LibraryItemId = matchedId2 }
        };

        var widget = BuildWidget(cachedMovies, userManagerMock, libraryManagerMock, dtoServiceMock);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), StartIndex = 0, Limit = 20 },
            CancellationToken.None);

        Assert.Equal(2, result.TotalRecordCount);
        Assert.NotNull(capturedItems);
        Assert.Equal(2, capturedItems!.Count);
    }

    [Fact]
    public async Task GetItemsAsync_DeletedLibraryItem_IsSkippedNotAnError()
    {
        var user = TestMocks.DefaultUser();
        var deletedId = Guid.NewGuid();

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(user);

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock.Setup(m => m.GetItemById(deletedId)).Returns((BaseItem?)null);

        IReadOnlyList<BaseItem>? capturedItems = null;
        var dtoServiceMock = new Mock<IDtoService>();
        dtoServiceMock
            .Setup(m => m.GetBaseItemDtos(
                It.IsAny<IReadOnlyList<BaseItem>>(),
                It.IsAny<DtoOptions>(),
                It.IsAny<User>(),
                It.IsAny<BaseItem>()))
            .Callback<IReadOnlyList<BaseItem>, DtoOptions, User, BaseItem>((items, _, _, _) => capturedItems = items)
            .Returns([]);

        var widget = BuildWidget(
            [new TMDbMovie { Id = 1, Title = "Gone", LibraryItemId = deletedId }],
            userManagerMock,
            libraryManagerMock,
            dtoServiceMock);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), Limit = 20 },
            CancellationToken.None);

        // TotalRecordCount still counts the cached match (self-corrects on next daily refresh);
        // the actually-resolved item list passed to the DTO service is empty.
        Assert.Equal(1, result.TotalRecordCount);
        Assert.NotNull(capturedItems);
        Assert.Empty(capturedItems!);
    }

    [Fact]
    public async Task GetItemsAsync_Pagination_RespectsStartIndexAndLimit()
    {
        var user = TestMocks.DefaultUser();
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(user);

        var libraryManagerMock = new Mock<ILibraryManager>();
        foreach (var id in ids)
        {
            libraryManagerMock.Setup(m => m.GetItemById(id)).Returns(new Movie { Id = id, Name = id.ToString() });
        }

        IReadOnlyList<BaseItem>? capturedItems = null;
        var dtoServiceMock = new Mock<IDtoService>();
        dtoServiceMock
            .Setup(m => m.GetBaseItemDtos(
                It.IsAny<IReadOnlyList<BaseItem>>(),
                It.IsAny<DtoOptions>(),
                It.IsAny<User>(),
                It.IsAny<BaseItem>()))
            .Callback<IReadOnlyList<BaseItem>, DtoOptions, User, BaseItem>((items, _, _, _) => capturedItems = items)
            .Returns([]);

        var cachedMovies = ids.Select((id, i) => new TMDbMovie { Id = i, Title = "M" + i, LibraryItemId = id }).ToList();
        var widget = BuildWidget(cachedMovies, userManagerMock, libraryManagerMock, dtoServiceMock);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), StartIndex = 2, Limit = 2 },
            CancellationToken.None);

        Assert.Equal(5, result.TotalRecordCount);
        Assert.NotNull(capturedItems);
        Assert.Equal(2, capturedItems!.Count);
        Assert.Equal(ids[2], capturedItems[0].Id);
        Assert.Equal(ids[3], capturedItems[1].Id);
    }
}
