using System.Collections.Concurrent;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;

/// <summary>
/// Analyzes a user's watch history and favorites to derive scored preferences (top genres, top
/// actors, top directors, recently watched films) consumed by personalized widgets.
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
    private readonly Func<PluginConfiguration?> _getConfiguration;
    private readonly ConcurrentDictionary<Guid, CacheEntry> _cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ScoringService"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="getConfiguration">
    /// Factory that returns the current plugin configuration, used to read the cache TTL.
    /// Defaults to <c>Plugin.Instance?.Configuration</c> in production.
    /// </param>
    public ScoringService(
        IUserManager userManager,
        ILibraryManager libraryManager,
        Func<PluginConfiguration?> getConfiguration)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
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

    /// <summary>Returns the user's most recently watched films, most recent first.</summary>
    /// <param name="userId">The user to score.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <returns>Up to <paramref name="limit"/> recently watched films (Value=item GUID, Label=title).</returns>
    public IReadOnlyList<ScoredValue> GetRecentlyWatched(Guid userId, int limit) =>
        GetSnapshot(userId).RecentlyWatched.Take(limit).ToList().AsReadOnly();

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

        var watched = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            IsPlayed = true,
            Recursive = true,
            OrderBy = [(ItemSortBy.DatePlayed, SortOrder.Descending)],
            Limit = MaxWatchedScan,
            DtoOptions = new DtoOptions { Fields = [] }
        });

        if (watched.Count == 0)
        {
            return ScoreSnapshot.Empty;
        }

        var favoriteIds = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            IsPlayed = true,
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
            watched.Select(i => new ScoredValue(i.Id.ToString(), i.Name)).ToList());
    }

    private static IReadOnlyList<ScoredValue> Rank(Dictionary<string, double> scores) =>
        scores
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new ScoredValue(kv.Key, kv.Key))
            .ToList()
            .AsReadOnly();

    private sealed record CacheEntry(DateTime ComputedAt, ScoreSnapshot Snapshot);

    private sealed record ScoreSnapshot(
        IReadOnlyList<ScoredValue> Genres,
        IReadOnlyList<ScoredValue> Actors,
        IReadOnlyList<ScoredValue> Directors,
        IReadOnlyList<ScoredValue> RecentlyWatched)
    {
        public static ScoreSnapshot Empty { get; } = new([], [], [], []);
    }
}
