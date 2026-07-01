using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Personalized;

public sealed class PersonalizedWidgetTests
{
    // Builds a real ScoringService backed by mocked Jellyfin services, so FavoriteGenreWidget's
    // fan-out (CreateInstances) exercises the real scoring pipeline with controlled watch history.
    private static ScoringService BuildScoringService(User user, IReadOnlyList<BaseItem> watched)
    {
        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(user);

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(q => q.IsFavorite != true)))
            .Returns(watched);
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(q => q.IsFavorite == true)))
            .Returns([]);
        libraryManagerMock
            .Setup(m => m.GetPeople(It.IsAny<BaseItem>()))
            .Returns([]);

        return new ScoringService(
            userManagerMock.Object,
            libraryManagerMock.Object,
            () => new PluginConfiguration());
    }

    private static FavoriteGenreWidget BuildWidget(
        ScoringService scoringService,
        Mock<IUserManager>? userManagerMock = null,
        Mock<ILibraryManager>? libraryManagerMock = null,
        Mock<IDtoService>? dtoServiceMock = null) =>
        new(
            (userManagerMock ?? new Mock<IUserManager>()).Object,
            (libraryManagerMock ?? new Mock<ILibraryManager>()).Object,
            (dtoServiceMock ?? new Mock<IDtoService>()).Object,
            scoringService);

    // -------------------------------------------------------------------------
    // Descriptor
    // -------------------------------------------------------------------------

    [Fact]
    public void FavoriteGenre_GetDescriptor_HasExpectedProperties()
    {
        var user = new User("test", "Default", "Default");
        var widget = BuildWidget(BuildScoringService(user, []));

        var d = widget.GetDescriptor();

        Assert.Equal("jux.personalized.favorite-genre", d.WidgetType);
        Assert.Equal(WidgetCategory.Personalized, d.Category);
        Assert.Equal(WidgetViewMode.Portrait, d.ViewMode);
        Assert.Null(d.AdditionalData);
    }

    // -------------------------------------------------------------------------
    // CreateInstances fan-out
    // -------------------------------------------------------------------------

    [Fact]
    public void FavoriteGenre_CreateInstances_FansOutOneInstancePerScoredGenre()
    {
        var user = new User("test", "Default", "Default");
        var action1 = new Movie { Id = Guid.NewGuid(), Name = "A1", Genres = ["Action"] };
        var action2 = new Movie { Id = Guid.NewGuid(), Name = "A2", Genres = ["Action"] };
        var drama1 = new Movie { Id = Guid.NewGuid(), Name = "D1", Genres = ["Drama"] };

        var widget = BuildWidget(BuildScoringService(user, [action1, action2, drama1]));

        var instances = widget.CreateInstances(user.Id, new WidgetInstanceConfig(), 3).ToList();

        Assert.Equal(2, instances.Count);
        var descriptors = instances.Select(i => i.GetDescriptor()).ToList();
        Assert.Equal("Action", descriptors[0].AdditionalData);
        Assert.Equal("More Action", descriptors[0].DisplayName);
        Assert.Equal("Drama", descriptors[1].AdditionalData);
        Assert.Equal("More Drama", descriptors[1].DisplayName);
    }

    [Fact]
    public void FavoriteGenre_CreateInstances_NoWatchHistory_ReturnsNoInstances()
    {
        var user = new User("test", "Default", "Default");
        var widget = BuildWidget(BuildScoringService(user, []));

        var instances = widget.CreateInstances(user.Id, new WidgetInstanceConfig(), 3).ToList();

        Assert.Empty(instances);
    }

    // -------------------------------------------------------------------------
    // GetItemsAsync / ApplyFilter
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FavoriteGenre_GetItemsAsync_AppliesGenreFilterAndExcludesWatchedByDefault()
    {
        var user = new User("test", "Default", "Default");
        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(user.Id)).Returns(user);

        InternalItemsQuery? capturedQuery = null;
        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemsResult(It.IsAny<InternalItemsQuery>()))
            .Callback<InternalItemsQuery>(q => capturedQuery = q)
            .Returns(new QueryResult<BaseItem>([]));

        var dtoServiceMock = new Mock<IDtoService>();
        dtoServiceMock
            .Setup(m => m.GetBaseItemDtos(
                It.IsAny<IReadOnlyList<BaseItem>>(),
                It.IsAny<DtoOptions>(),
                It.IsAny<User>(),
                It.IsAny<BaseItem>()))
            .Returns([]);

        var widget = BuildWidget(
            BuildScoringService(user, []),
            userManagerMock,
            libraryManagerMock,
            dtoServiceMock);

        await widget.GetItemsAsync(
            new WidgetPayload { UserId = user.Id, AdditionalData = "Action" },
            CancellationToken.None);

        Assert.NotNull(capturedQuery);
        Assert.Equal(["Action"], capturedQuery!.Genres);
        Assert.True(capturedQuery.IsPlayed == false);
    }

    [Fact]
    public async Task FavoriteGenre_GetItemsAsync_ExcludeWatchedFalse_DoesNotFilterIsPlayed()
    {
        var user = new User("test", "Default", "Default");
        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(user.Id)).Returns(user);

        InternalItemsQuery? capturedQuery = null;
        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemsResult(It.IsAny<InternalItemsQuery>()))
            .Callback<InternalItemsQuery>(q => capturedQuery = q)
            .Returns(new QueryResult<BaseItem>([]));

        var dtoServiceMock = new Mock<IDtoService>();
        dtoServiceMock
            .Setup(m => m.GetBaseItemDtos(
                It.IsAny<IReadOnlyList<BaseItem>>(),
                It.IsAny<DtoOptions>(),
                It.IsAny<User>(),
                It.IsAny<BaseItem>()))
            .Returns([]);

        var widget = BuildWidget(
            BuildScoringService(user, []),
            userManagerMock,
            libraryManagerMock,
            dtoServiceMock);

        await widget.GetItemsAsync(
            new WidgetPayload
            {
                UserId = user.Id,
                AdditionalData = "Action",
                ExtraParams = new Dictionary<string, string> { ["excludeWatched"] = "false" }
            },
            CancellationToken.None);

        Assert.NotNull(capturedQuery);
        Assert.Null(capturedQuery!.IsPlayed);
    }

    [Fact]
    public async Task FavoriteGenre_EmptyAdditionalData_ReturnsEmpty()
    {
        var user = new User("test", "Default", "Default");
        var widget = BuildWidget(BuildScoringService(user, []));

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = user.Id, AdditionalData = null },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task FavoriteGenre_UserNotFound_ReturnsEmpty()
    {
        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns((User?)null);

        var widget = BuildWidget(
            BuildScoringService(new User("test", "Default", "Default"), []),
            userManagerMock);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), AdditionalData = "Action" },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
    }

    // -------------------------------------------------------------------------
    // FavoriteActorWidget / FavoriteDirectorWidget
    // -------------------------------------------------------------------------

    [Fact]
    public void FavoriteActor_GetDescriptor_HasExpectedProperties()
    {
        var user = new User("test", "Default", "Default");
        var widget = new FavoriteActorWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            BuildScoringService(user, []));

        var d = widget.GetDescriptor();

        Assert.Equal("jux.personalized.favorite-actor", d.WidgetType);
        Assert.Equal(WidgetCategory.Personalized, d.Category);
        Assert.Equal(WidgetViewMode.Portrait, d.ViewMode);
    }

    [Fact]
    public void FavoriteDirector_GetDescriptor_HasExpectedProperties()
    {
        var user = new User("test", "Default", "Default");
        var widget = new FavoriteDirectorWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object,
            BuildScoringService(user, []));

        var d = widget.GetDescriptor();

        Assert.Equal("jux.personalized.favorite-director", d.WidgetType);
        Assert.Equal(WidgetCategory.Personalized, d.Category);
        Assert.Equal(WidgetViewMode.Portrait, d.ViewMode);
    }

    [Fact]
    public async Task FavoriteActor_GetItemsAsync_AppliesPersonAndPersonTypeFilter()
    {
        var user = new User("test", "Default", "Default");
        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(user.Id)).Returns(user);

        InternalItemsQuery? capturedQuery = null;
        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemsResult(It.IsAny<InternalItemsQuery>()))
            .Callback<InternalItemsQuery>(q => capturedQuery = q)
            .Returns(new QueryResult<BaseItem>([]));

        var dtoServiceMock = new Mock<IDtoService>();
        dtoServiceMock
            .Setup(m => m.GetBaseItemDtos(
                It.IsAny<IReadOnlyList<BaseItem>>(),
                It.IsAny<DtoOptions>(),
                It.IsAny<User>(),
                It.IsAny<BaseItem>()))
            .Returns([]);

        var widget = new FavoriteActorWidget(
            userManagerMock.Object,
            libraryManagerMock.Object,
            dtoServiceMock.Object,
            BuildScoringService(user, []));

        await widget.GetItemsAsync(
            new WidgetPayload { UserId = user.Id, AdditionalData = "Brad Pitt" },
            CancellationToken.None);

        Assert.NotNull(capturedQuery);
        Assert.Equal("Brad Pitt", capturedQuery!.Person);
        Assert.Equal(["Actor"], capturedQuery.PersonTypes);
    }

    [Fact]
    public void FavoriteDirector_CreateInstances_UsesTopDirectorsNotActors()
    {
        var user = new User("test", "Default", "Default");
        var film1 = new Movie { Id = Guid.NewGuid(), Name = "F1", Genres = [] };

        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(user);

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(q => q.IsFavorite != true)))
            .Returns([film1]);
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(q => q.IsFavorite == true)))
            .Returns([]);
        libraryManagerMock
            .Setup(m => m.GetPeople(It.IsAny<BaseItem>()))
            .Returns([new PersonInfo { Name = "Christopher Nolan", Type = PersonKind.Director }]);

        var scoringService = new ScoringService(
            userManagerMock.Object,
            libraryManagerMock.Object,
            () => new PluginConfiguration());

        var widget = new FavoriteDirectorWidget(
            userManagerMock.Object,
            libraryManagerMock.Object,
            new Mock<IDtoService>().Object,
            scoringService);

        var instances = widget.CreateInstances(user.Id, new WidgetInstanceConfig(), 3).ToList();

        var single = Assert.Single(instances);
        var descriptor = single.GetDescriptor();
        Assert.Equal("Christopher Nolan", descriptor.AdditionalData);
        Assert.Equal("Directed by Christopher Nolan", descriptor.DisplayName);
    }
}
