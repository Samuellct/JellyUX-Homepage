using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
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
        Func<BaseItem, IReadOnlyList<PersonInfo>> people,
        IReadOnlyList<BaseItem>? watchedSeries = null)
    {
        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(user);

        var libraryManagerMock = new Mock<ILibraryManager>();
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(
                q => q.IsFavorite != true && q.IncludeItemTypes.Contains(BaseItemKind.Movie))))
            .Returns(watched);
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(
                q => q.IsFavorite != true && q.IncludeItemTypes.SequenceEqual(new[] { BaseItemKind.Series }))))
            .Returns(watchedSeries ?? []);
        libraryManagerMock
            .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(q => q.IsFavorite == true)))
            .Returns(favorites);
        libraryManagerMock
            .Setup(m => m.GetPeople(It.IsAny<BaseItem>()))
            .Returns((BaseItem item) => people(item));

        // No LastPlayedDate mocked here: ComputeSnapshot falls back to DateTime.MinValue for every
        // item in that case, so the stable OrderByDescending preserves the movies-then-series concat
        // order below -- fine for tests that don't specifically exercise cross-type recency ordering
        // (see the dedicated test for that).
        var userDataManagerMock = new Mock<IUserDataManager>();

        return new ScoringService(
            userManagerMock.Object,
            libraryManagerMock.Object,
            userDataManagerMock.Object,
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
    public void GetTopGenres_SeriesContributeToTallyAlongsideMovies()
    {
        var user = new User("test", "Default", "Default");

        var action1 = new Movie { Name = "Action Movie", Genres = ["Action"] };
        var dramaSeries1 = new Series { Name = "Drama Series 1", Genres = ["Drama"] };
        var dramaSeries2 = new Series { Name = "Drama Series 2", Genres = ["Drama"] };

        var service = BuildService(
            user,
            watched: [action1],
            favorites: [],
            people: _ => [],
            watchedSeries: [dramaSeries1, dramaSeries2]);

        var result = service.GetTopGenres(user.Id, 5);

        Assert.Equal("Drama", result[0].Value);
    }

    [Fact]
    public void GetTopGenres_UnknownUser_ReturnsEmpty()
    {
        var userManagerMock = new Mock<IUserManager>();
        userManagerMock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns((User?)null);

        var service = new ScoringService(
            userManagerMock.Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IUserDataManager>().Object,
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

    [Fact]
    public void GetRecentlyWatched_ScopeMovies_ExcludesSeries()
    {
        var user = new User("test", "Default", "Default");
        var movie = new Movie { Name = "A Movie", Genres = [] };
        var series = new Series { Name = "A Series", Genres = [] };

        var service = BuildService(
            user,
            watched: [movie],
            favorites: [],
            people: _ => [],
            watchedSeries: [series]);

        var result = service.GetRecentlyWatched(user.Id, 5, BecauseYouWatchedScope.Movies);

        var single = Assert.Single(result);
        Assert.Equal("A Movie", single.Label);
    }

    [Fact]
    public void GetRecentlyWatched_ScopeSeries_ExcludesMovies()
    {
        var user = new User("test", "Default", "Default");
        var movie = new Movie { Name = "A Movie", Genres = [] };
        var series = new Series { Name = "A Series", Genres = [] };

        var service = BuildService(
            user,
            watched: [movie],
            favorites: [],
            people: _ => [],
            watchedSeries: [series]);

        var result = service.GetRecentlyWatched(user.Id, 5, BecauseYouWatchedScope.Series);

        var single = Assert.Single(result);
        Assert.Equal("A Series", single.Label);
    }

    [Fact]
    public void GetRecentlyWatched_ScopeBoth_IncludesMoviesAndSeries()
    {
        var user = new User("test", "Default", "Default");
        var movie = new Movie { Name = "A Movie", Genres = [] };
        var series = new Series { Name = "A Series", Genres = [] };

        var service = BuildService(
            user,
            watched: [movie],
            favorites: [],
            people: _ => [],
            watchedSeries: [series]);

        var result = service.GetRecentlyWatched(user.Id, 5, BecauseYouWatchedScope.Both);

        Assert.Equal(2, result.Count);
    }
}
