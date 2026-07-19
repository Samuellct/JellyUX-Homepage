using System.Collections.Concurrent;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;

/// <summary>
/// Analyzes a user's watch history and favorites to derive scored preferences (top genres, top
/// actors, top directors, recently watched films/shows) consumed by personalized widgets.
/// <para>
/// Scoring is computed once per user and cached for the same TTL as the widget layout
/// (<see cref="Configuration.CacheConfig.SessionTtlMinutes"/>), so scores refresh at the same
/// cadence as the home screen.
/// </para>
/// </summary>
public sealed class ScoringService
{
    private const int MaxWatchedScan = 500;

    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserDataManager _userDataManager;
    private readonly Func<PluginConfiguration?> _getConfiguration;
    private readonly ConcurrentDictionary<Guid, CacheEntry> _cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ScoringService"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="userDataManager">
    /// Jellyfin user data manager, used to read each watched item's actual last-played date so
    /// movies and series (queried separately, see <see cref="ComputeSnapshot"/>) can be merged into a
    /// single, genuinely recency-ordered list rather than one type always ranking before the other.
    /// </param>
    /// <param name="getConfiguration">
    /// Factory that returns the current plugin configuration, used to read the cache TTL.
    /// Defaults to <c>Plugin.Instance?.Configuration</c> in production.
    /// </param>
    public ScoringService(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IUserDataManager userDataManager,
        Func<PluginConfiguration?> getConfiguration)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _userDataManager = userDataManager;
        _getConfiguration = getConfiguration;
    }

    /// <summary>Returns the user's top genres by watch history, ranked highest-scored first.</summary>
    /// <param name="userId">The user to score.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <returns>Up to <paramref name="limit"/> scored genres.</returns>
    public IReadOnlyList<ScoredValue> GetTopGenres(Guid userId, int limit) =>
        GetSnapshot(userId).Genres.Take(limit).ToList().AsReadOnly();

    /// <summary>Returns the user's top actors by watch history, ranked highest-scored first.</summary>
    /// <param name="userId">The user to score.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <returns>Up to <paramref name="limit"/> scored actors.</returns>
    public IReadOnlyList<ScoredValue> GetTopActors(Guid userId, int limit) =>
        GetSnapshot(userId).Actors.Take(limit).ToList().AsReadOnly();

    /// <summary>Returns the user's top directors by watch history, ranked highest-scored first.</summary>
    /// <param name="userId">The user to score.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <returns>Up to <paramref name="limit"/> scored directors.</returns>
    public IReadOnlyList<ScoredValue> GetTopDirectors(Guid userId, int limit) =>
        GetSnapshot(userId).Directors.Take(limit).ToList().AsReadOnly();

    /// <summary>Returns the user's most recently watched movies and/or series, most recent first.</summary>
    /// <param name="userId">The user to score.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <param name="scope">
    /// Restricts which item types are eligible (see <see cref="BecauseYouWatchedScope"/>). Defaults to
    /// <see cref="BecauseYouWatchedScope.Both"/>.
    /// </param>
    /// <returns>Up to <paramref name="limit"/> recently watched items (Value=item GUID, Label=title).</returns>
    public IReadOnlyList<ScoredValue> GetRecentlyWatched(
        Guid userId,
        int limit,
        BecauseYouWatchedScope scope = BecauseYouWatchedScope.Both)
    {
        var entries = GetSnapshot(userId).RecentlyWatched.AsEnumerable();
        entries = scope switch
        {
            BecauseYouWatchedScope.Movies => entries.Where(e => e.Kind == BaseItemKind.Movie),
            BecauseYouWatchedScope.Series => entries.Where(e => e.Kind == BaseItemKind.Series),
            _ => entries
        };

        return entries.Take(limit).Select(e => new ScoredValue(e.Value, e.Label)).ToList().AsReadOnly();
    }

    /// <summary>Clears all cached scoring snapshots, forcing recomputation on next access.</summary>
    public void Clear() => _cache.Clear();

    private ScoreSnapshot GetSnapshot(Guid userId)
    {
        var ttlMinutes = _getConfiguration()?.Cache?.SessionTtlMinutes ?? 15;
        var ttl = TimeSpan.FromMinutes(ttlMinutes);

        if (_cache.TryGetValue(userId, out var entry) && DateTime.UtcNow - entry.ComputedAt < ttl)
        {
            return entry.Snapshot;
        }

        var snapshot = ComputeSnapshot(userId);
        _cache[userId] = new CacheEntry(DateTime.UtcNow, snapshot);
        return snapshot;
    }

    private ScoreSnapshot ComputeSnapshot(Guid userId)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return ScoreSnapshot.Empty;
        }

        var watchedMovies = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            IsPlayed = true,
            Recursive = true,
            OrderBy = [(ItemSortBy.DatePlayed, SortOrder.Descending)],
            Limit = MaxWatchedScan,
            DtoOptions = new DtoOptions { Fields = [] }
        });

        // Deliberately a separate query, not merged into the movie query's IncludeItemTypes array:
        // Jellyfin's query layer only applies "at least one episode watched" (partial-viewing)
        // semantics to IsPlayed=true when IncludeItemTypes is exactly [Series] alone. Mixing Series
        // into a multi-type array instead falls back to each item's own aggregate UserData.Played,
        // which for a Series only becomes true once every episode has been watched -- silently
        // excluding a show the user is still in the middle of, exactly the gap this fix must close.
        var watchedSeries = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Series],
            IsPlayed = true,
            Recursive = true,
            OrderBy = [(ItemSortBy.DatePlayed, SortOrder.Descending)],
            Limit = MaxWatchedScan,
            DtoOptions = new DtoOptions { Fields = [] }
        });

        if (watchedMovies.Count == 0 && watchedSeries.Count == 0)
        {
            return ScoreSnapshot.Empty;
        }

        // Each sub-list is already ordered by DatePlayed server-side, but the two lists can't be
        // concatenated and trusted as one recency order -- re-sort the combined list using each
        // item's actual last-played date so movies and series interleave correctly instead of one
        // type always outranking the other.
        var watched = watchedMovies.Concat(watchedSeries)
            .OrderByDescending(item => _userDataManager.GetUserData(user, item)?.LastPlayedDate ?? DateTime.MinValue)
            .Take(MaxWatchedScan)
            .ToList();

        var favoriteIds = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
            IsFavorite = true,
            Recursive = true,
            Limit = MaxWatchedScan,
            DtoOptions = new DtoOptions { Fields = [] }
        }).Select(i => i.Id).ToHashSet();

        var genreScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var actorScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var directorScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < watched.Count; i++)
        {
            var item = watched[i];

            // Earlier position in the DatePlayed-descending list means more recently watched;
            // give it a small recency bonus on top of the base weight of 1. Favorites get a
            // further fixed bonus so favorited titles influence scoring more than a single watch.
            var recencyWeight = 1.0 + ((watched.Count - i) / (double)watched.Count);
            var weight = recencyWeight + (favoriteIds.Contains(item.Id) ? 1.5 : 0);

            // Genres/People are read identically regardless of item type: a Series carries its own
            // main-cast People and Genres (populated by the TMDb series metadata provider), so no
            // special-casing is needed here -- confirmed against Jellyfin's actual provider behavior
            // before relying on it (see Phase 1.3 research notes).
            foreach (var genre in item.Genres)
            {
                genreScores[genre] = genreScores.GetValueOrDefault(genre) + weight;
            }

            foreach (var person in _libraryManager.GetPeople(item))
            {
                if (person.Type == PersonKind.Actor)
                {
                    actorScores[person.Name] = actorScores.GetValueOrDefault(person.Name) + weight;
                }
                else if (person.Type == PersonKind.Director)
                {
                    directorScores[person.Name] = directorScores.GetValueOrDefault(person.Name) + weight;
                }
            }
        }

        return new ScoreSnapshot(
            Rank(genreScores),
            Rank(actorScores),
            Rank(directorScores),
            watched.Select(i => new RecentlyWatchedEntry(
                i.Id.ToString(),
                i.Name,
                i is Series ? BaseItemKind.Series : BaseItemKind.Movie)).ToList());
    }

    private static IReadOnlyList<ScoredValue> Rank(Dictionary<string, double> scores) =>
        scores
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new ScoredValue(kv.Key, kv.Key))
            .ToList()
            .AsReadOnly();

    private sealed record CacheEntry(DateTime ComputedAt, ScoreSnapshot Snapshot);

    /// <summary>
    /// A recently-watched item retained for "Because You Watched" reference selection, tagged with
    /// its <see cref="BaseItemKind"/> so <see cref="GetRecentlyWatched"/> can honor a per-row scope
    /// (movies only / series only / both).
    /// </summary>
    private sealed record RecentlyWatchedEntry(string Value, string Label, BaseItemKind Kind);

    private sealed record ScoreSnapshot(
        IReadOnlyList<ScoredValue> Genres,
        IReadOnlyList<ScoredValue> Actors,
        IReadOnlyList<ScoredValue> Directors,
        IReadOnlyList<RecentlyWatchedEntry> RecentlyWatched)
    {
        public static ScoreSnapshot Empty { get; } = new([], [], [], []);
    }
}
