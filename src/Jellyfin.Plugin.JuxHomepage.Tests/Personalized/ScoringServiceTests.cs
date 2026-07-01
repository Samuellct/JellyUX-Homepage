using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Personalized;

public sealed class ScoringServiceTests
{
    private static PersonInfo Actor(string name) => new() { Name = name, Type = PersonKind.Actor };

    private static PersonInfo Director(string name) => new() { Name = name, Type = PersonKind.Director };

    private static ScoringService BuildService(
        User user,
        IReadOnlyList<BaseItem> watched,
        IReadOnlyList<BaseItem> favorites,
        Func<BaseItem, IReadOnlyList<PersonInfo>> people)
    {
        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(user);

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(q => q.IsFavorite != true)))
            .Returns(watched);
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(q => q.IsFavorite == true)))
            .Returns(favorites);
        libraryManagerMock
            .Setup(m => m.GetPeople(It.IsAny<BaseItem>()))
            .Returns((BaseItem item) => people(item));

        return new ScoringService(
            userManagerMock.Object,
            libraryManagerMock.Object,
            () => new PluginConfiguration());
    }

    // -------------------------------------------------------------------------
    // GetTopGenres
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTopGenres_TalliesAcrossWatchedItems_MostFrequentFirst()
    {
        var user = new User("test", "Default", "Default");

        var action1 = new Movie { Name = "Action 1", Genres = ["Action"] };
        var action2 = new Movie { Name = "Action 2", Genres = ["Action"] };
        var drama1 = new Movie { Name = "Drama 1", Genres = ["Drama"] };

        var service = BuildService(
            user,
            watched: [action1, action2, drama1],
            favorites: [],
            people: _ => []);

        var result = service.GetTopGenres(user.Id, 5);

        Assert.Equal("Action", result[0].Value);
        Assert.Equal("Drama", result[1].Value);
    }

    [Fact]
    public void GetTopGenres_FavoriteOutranksNonFavoriteWithFewerWatches()
    {
        var user = new User("test", "Default", "Default");

        // Drama watched once but favorited should outrank Comedy watched twice, unfavored.
        // Distinct Ids are required: BaseItem.Id defaults to Guid.Empty, which would make every
        // item match a favorite-id set built from any single unfavored item.
        var dramaFavorite = new Movie { Id = Guid.NewGuid(), Name = "Drama Fav", Genres = ["Drama"] };
        var comedy1 = new Movie { Id = Guid.NewGuid(), Name = "Comedy 1", Genres = ["Comedy"] };
        var comedy2 = new Movie { Id = Guid.NewGuid(), Name = "Comedy 2", Genres = ["Comedy"] };

        var service = BuildService(
            user,
            watched: [dramaFavorite, comedy1, comedy2],
            favorites: [dramaFavorite],
            people: _ => []);

        var result = service.GetTopGenres(user.Id, 5);

        Assert.Equal("Drama", result[0].Value);
    }

    [Fact]
    public void GetTopGenres_NoWatchHistory_ReturnsEmpty()
    {
        var user = new User("test", "Default", "Default");
        var service = BuildService(user, watched: [], favorites: [], people: _ => []);

        var result = service.GetTopGenres(user.Id, 5);

        Assert.Empty(result);
    }

    [Fact]
    public void GetTopGenres_UnknownUser_ReturnsEmpty()
    {
        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns((User?)null);

        var service = new ScoringService(
            userManagerMock.Object,
            new Mock<ILibraryManager>().Object,
            () => new PluginConfiguration());

        var result = service.GetTopGenres(Guid.NewGuid(), 5);

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // GetTopActors / GetTopDirectors
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTopActors_TalliesActorsAcrossWatchedItems()
    {
        var user = new User("test", "Default", "Default");
        var film1 = new Movie { Name = "Film 1", Genres = [] };
        var film2 = new Movie { Name = "Film 2", Genres = [] };

        var service = BuildService(
            user,
            watched: [film1, film2],
            favorites: [],
            people: item => item == film1
                ? [Actor("Brad Pitt"), Director("Someone")]
                : [Actor("Brad Pitt")]);

        var result = service.GetTopActors(user.Id, 5);

        Assert.Equal("Brad Pitt", result[0].Value);
    }

    [Fact]
    public void GetTopDirectors_ExcludesActorsFromDirectorTally()
    {
        var user = new User("test", "Default", "Default");
        var film1 = new Movie { Name = "Film 1", Genres = [] };

        var service = BuildService(
            user,
            watched: [film1],
            favorites: [],
            people: _ => [Actor("Brad Pitt"), Director("Christopher Nolan")]);

        var result = service.GetTopDirectors(user.Id, 5);

        var single = Assert.Single(result);
        Assert.Equal("Christopher Nolan", single.Value);
    }

    // -------------------------------------------------------------------------
    // GetRecentlyWatched
    // -------------------------------------------------------------------------

    [Fact]
    public void GetRecentlyWatched_ReturnsWatchedItemsInQueryOrder()
    {
        var user = new User("test", "Default", "Default");
        var recent = new Movie { Name = "Recent", Genres = [] };
        var older = new Movie { Name = "Older", Genres = [] };

        var service = BuildService(user, watched: [recent, older], favorites: [], people: _ => []);

        var result = service.GetRecentlyWatched(user.Id, 5);

        Assert.Equal(2, result.Count);
        Assert.Equal("Recent", result[0].Label);
        Assert.Equal(recent.Id.ToString(), result[0].Value);
    }

    [Fact]
    public void GetRecentlyWatched_RespectsLimit()
    {
        var user = new User("test", "Default", "Default");
        var items = Enumerable.Range(0, 5)
            .Select(i => new Movie { Name = $"Film {i}", Genres = [] })
            .Cast<BaseItem>()
            .ToList();

        var service = BuildService(user, watched: items, favorites: [], people: _ => []);

        var result = service.GetRecentlyWatched(user.Id, 2);

        Assert.Equal(2, result.Count);
    }
}
