using System.Globalization;
using System.Text.Json;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.TMDb;

/// <summary>
/// Reads and refreshes the on-disk TMDb data cache under
/// <c>PluginConfigurationsPath/Jellyfin.Plugin.JuxHomepage/cache/tmdb/</c>.
/// Mirrors <see cref="Configuration.UserConfigurationStore"/>'s disk-persistence pattern:
/// <c>System.Text.Json</c> serialization guarded by a <see cref="ReaderWriterLockSlim"/>.
/// </summary>
public sealed class TMDbCacheService : ITMDbCacheService, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _cacheDir;
    private readonly ITMDbApiClient _apiClient;
    private readonly ILibraryManager _libraryManager;
    private readonly Func<PluginConfiguration?> _getConfiguration;
    private readonly ILogger<TMDbCacheService> _logger;
    private readonly ReaderWriterLockSlim _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TMDbCacheService"/> class.
    /// </summary>
    /// <param name="applicationPaths">Provides the plugin configurations directory path.</param>
    /// <param name="apiClient">TMDb API client used to fetch fresh data during a refresh.</param>
    /// <param name="libraryManager">Jellyfin library manager, used to cross-reference cached items.</param>
    /// <param name="getConfiguration">
    /// Factory that returns the current plugin configuration, used to read the refresh interval.
    /// Defaults to <c>Plugin.Instance?.Configuration</c> in production.
    /// </param>
    /// <param name="logger">Logger.</param>
    public TMDbCacheService(
        IApplicationPaths applicationPaths,
        ITMDbApiClient apiClient,
        ILibraryManager libraryManager,
        Func<PluginConfiguration?> getConfiguration,
        ILogger<TMDbCacheService> logger)
    {
        _apiClient = apiClient;
        _libraryManager = libraryManager;
        _getConfiguration = getConfiguration;
        _logger = logger;

        _cacheDir = Path.Combine(
            applicationPaths.PluginConfigurationsPath,
            "Jellyfin.Plugin.JuxHomepage",
            "cache",
            "tmdb");

        Directory.CreateDirectory(_cacheDir);
    }

    /// <inheritdoc/>
    public IReadOnlyList<TMDbMovie> GetTrendingMovies() => ReadCache<TMDbMovie>(TMDbCacheType.TrendingMovies);

    /// <inheritdoc/>
    public IReadOnlyList<TMDbShow> GetTrendingShows() => ReadCache<TMDbShow>(TMDbCacheType.TrendingShows);

    /// <inheritdoc/>
    public IReadOnlyList<TMDbShow> GetAiringToday() => ReadCache<TMDbShow>(TMDbCacheType.AiringToday);

    /// <inheritdoc/>
    public IReadOnlyList<TMDbMovie> GetUpcomingMovies() => ReadCache<TMDbMovie>(TMDbCacheType.UpcomingMovies);

    /// <inheritdoc/>
    public IReadOnlyList<TMDbMovie> GetTopRatedMovies() => ReadCache<TMDbMovie>(TMDbCacheType.TopRatedMovies);

    /// <inheritdoc/>
    public IReadOnlyList<TMDbShow> GetTopRatedShows() => ReadCache<TMDbShow>(TMDbCacheType.TopRatedShows);

    /// <inheritdoc/>
    public IReadOnlyList<TMDbMovie> GetNowPlayingMovies() => ReadCache<TMDbMovie>(TMDbCacheType.NowPlayingMovies);

    /// <inheritdoc/>
    public Task RefreshTrendingMoviesAsync(CancellationToken cancellationToken)
    {
        var pages = _getConfiguration()?.TMDbLists?.TrendingMoviesPages ?? 1;
        return RefreshMoviesAsync(
            TMDbCacheType.TrendingMovies,
            ct => _apiClient.GetTrendingMoviesAsync(pages, ct),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task RefreshUpcomingMoviesAsync(CancellationToken cancellationToken)
    {
        var pages = _getConfiguration()?.TMDbLists?.UpcomingMoviesPages ?? 1;
        return RefreshMoviesAsync(
            TMDbCacheType.UpcomingMovies,
            ct => _apiClient.GetUpcomingMoviesAsync(pages, ct),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task RefreshTrendingShowsAsync(CancellationToken cancellationToken)
    {
        var pages = _getConfiguration()?.TMDbLists?.TrendingShowsPages ?? 1;
        return RefreshShowsAsync(
            TMDbCacheType.TrendingShows,
            ct => _apiClient.GetTrendingShowsAsync(pages, ct),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task RefreshAiringTodayAsync(CancellationToken cancellationToken)
    {
        var pages = _getConfiguration()?.TMDbLists?.AiringTodayPages ?? 1;
        return RefreshShowsAsync(
            TMDbCacheType.AiringToday,
            ct => _apiClient.GetAiringTodayAsync(pages, ct),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task RefreshTopRatedMoviesAsync(CancellationToken cancellationToken)
    {
        var pages = _getConfiguration()?.TMDbLists?.TopRatedMoviesPages ?? 1;
        return RefreshMoviesAsync(
            TMDbCacheType.TopRatedMovies,
            ct => _apiClient.GetTopRatedMoviesAsync(pages, ct),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task RefreshTopRatedShowsAsync(CancellationToken cancellationToken)
    {
        var pages = _getConfiguration()?.TMDbLists?.TopRatedShowsPages ?? 1;
        return RefreshShowsAsync(
            TMDbCacheType.TopRatedShows,
            ct => _apiClient.GetTopRatedShowsAsync(pages, ct),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task RefreshNowPlayingMoviesAsync(CancellationToken cancellationToken)
    {
        var tmdbLists = _getConfiguration()?.TMDbLists;
        var pages = tmdbLists?.NowPlayingMoviesPages ?? 1;
        var region = tmdbLists?.NowPlayingRegion;
        return RefreshMoviesAsync(
            TMDbCacheType.NowPlayingMovies,
            ct => _apiClient.GetNowPlayingMoviesAsync(pages, region, ct),
            cancellationToken);
    }

    /// <inheritdoc/>
    public bool IsStale(TMDbCacheType type)
    {
        var path = GetPath(type);
        if (!File.Exists(path))
        {
            return true;
        }

        var hours = _getConfiguration()?.Cache?.TMDbRefreshIntervalHours ?? 24;
        var lastWrite = File.GetLastWriteTimeUtc(path);
        return DateTime.UtcNow - lastWrite >= TimeSpan.FromHours(hours);
    }

    /// <inheritdoc/>
    public DateTime? GetLastRefreshedUtc(TMDbCacheType type)
    {
        var path = GetPath(type);
        return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : null;
    }

    /// <inheritdoc/>
    public async Task RefreshAllAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        // Seven fixed refreshes, evenly spaced across 0-100.
        Func<CancellationToken, Task>[] steps =
        [
            RefreshTrendingMoviesAsync,
            RefreshTrendingShowsAsync,
            RefreshAiringTodayAsync,
            RefreshUpcomingMoviesAsync,
            RefreshTopRatedMoviesAsync,
            RefreshTopRatedShowsAsync,
            RefreshNowPlayingMoviesAsync
        ];

        progress?.Report(0);
        for (var i = 0; i < steps.Length; i++)
        {
            await steps[i](cancellationToken).ConfigureAwait(false);
            progress?.Report((i + 1) * 100.0 / steps.Length);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _lock.Dispose();
            _disposed = true;
        }
    }

    private async Task RefreshMoviesAsync(
        TMDbCacheType type,
        Func<CancellationToken, Task<IReadOnlyList<TMDbMovie>>> fetch,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await fetch(cancellationToken).ConfigureAwait(false);
            var matched = await CrossReferenceAsync(
                items,
                _apiClient.GetMovieExternalIdsAsync,
                [BaseItemKind.Movie],
                cancellationToken).ConfigureAwait(false);
            WriteCacheUnlessEmpty(type, items);
            LogRefreshOutcome(type, items.Count, matched);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh TMDb cache '{Type}'.", type);
        }
    }

    private async Task RefreshShowsAsync(
        TMDbCacheType type,
        Func<CancellationToken, Task<IReadOnlyList<TMDbShow>>> fetch,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await fetch(cancellationToken).ConfigureAwait(false);
            var matched = await CrossReferenceAsync(
                items,
                _apiClient.GetShowExternalIdsAsync,
                [BaseItemKind.Series],
                cancellationToken).ConfigureAwait(false);
            WriteCacheUnlessEmpty(type, items);
            LogRefreshOutcome(type, items.Count, matched);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh TMDb cache '{Type}'.", type);
        }
    }

    /// <summary>
    /// Logs an explicit, unambiguous summary of a refresh's outcome. In particular, an empty item
    /// count almost always means the API key is missing/invalid or the request failed -- both of
    /// which are already logged in detail by <see cref="TMDbApiClient"/> -- rather than a genuine
    /// "TMDb has nothing to report" result.
    /// </summary>
    private void LogRefreshOutcome(TMDbCacheType type, int itemCount, int matchedCount)
    {
        if (itemCount == 0)
        {
            _logger.LogInformation(
                "TMDb cache '{Type}' refresh returned 0 items -- check for a preceding TMDbApiClient warning/error (missing or invalid API key, or a network failure). The previous cache, if any, was left untouched.",
                type);
            return;
        }

        _logger.LogInformation(
            "TMDb cache '{Type}' refreshed: {ItemCount} item(s), {MatchedCount} matched to the local library.",
            type,
            itemCount,
            matchedCount);
    }

    /// <summary>
    /// Sets <see cref="ITMDbCacheItem.LibraryItemId"/> on each item by looking it up in the local
    /// library, primarily by IMDb ID (fetched via <paramref name="getExternalImdbId"/>), falling
    /// back to a direct TMDb ID match when no IMDb ID is available or no match is found.
    /// </summary>
    /// <returns>The number of items that were matched to a local library item.</returns>
    private async Task<int> CrossReferenceAsync<T>(
        IReadOnlyList<T> items,
        Func<int, CancellationToken, Task<string?>> getExternalImdbId,
        BaseItemKind[] includeItemTypes,
        CancellationToken cancellationToken)
        where T : ITMDbCacheItem
    {
        var matched = 0;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? imdbId = null;
            try
            {
                imdbId = await getExternalImdbId(item.Id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch external IDs for TMDb id {TmdbId}.", item.Id);
            }

            BaseItem? match = string.IsNullOrEmpty(imdbId)
                ? null
                : FindLibraryMatch(MetadataProvider.Imdb, imdbId, includeItemTypes);

            match ??= FindLibraryMatch(
                MetadataProvider.Tmdb,
                item.Id.ToString(CultureInfo.InvariantCulture),
                includeItemTypes);

            item.LibraryItemId = match?.Id;
            if (match is not null)
            {
                matched++;
            }
        }

        return matched;
    }

    private BaseItem? FindLibraryMatch(MetadataProvider provider, string value, BaseItemKind[] includeItemTypes)
    {
        var result = _libraryManager.GetItemList(new InternalItemsQuery
        {
            HasAnyProviderId = new Dictionary<string, string> { [provider.ToString()] = value },
            IncludeItemTypes = includeItemTypes,
            Recursive = true,
            Limit = 1
        });

        return result.Count > 0 ? result[0] : null;
    }

    private IReadOnlyList<T> ReadCache<T>(TMDbCacheType type)
    {
        var path = GetPath(type);

        _lock.EnterReadLock();
        try
        {
            if (!File.Exists(path))
            {
                return [];
            }

            var json = File.ReadAllText(path);
            var entry = JsonSerializer.Deserialize<TMDbCacheEntry<T>>(json);
            return entry?.Items ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read TMDb cache file for '{Type}'.", type);
            return [];
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Writes the cache file for the given type, unless <paramref name="items"/> is empty. An empty
    /// result almost always means the fetch failed (missing/invalid key, network error) rather than
    /// TMDb genuinely having nothing to report, so overwriting a previously-populated cache with an
    /// empty one would silently destroy good data on a transient failure (e.g. a temporarily invalid
    /// API key). <see cref="LogRefreshOutcome"/> still surfaces the empty result either way.
    /// </summary>
    private void WriteCacheUnlessEmpty<T>(TMDbCacheType type, IReadOnlyList<T> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        var path = GetPath(type);
        var entry = new TMDbCacheEntry<T> { RefreshedAtUtc = DateTime.UtcNow, Items = items };
        var json = JsonSerializer.Serialize(entry, SerializerOptions);

        _lock.EnterWriteLock();
        try
        {
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write TMDb cache file for '{Type}'.", type);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private string GetPath(TMDbCacheType type) => Path.Combine(_cacheDir, GetFileName(type));

    private static string GetFileName(TMDbCacheType type) => type switch
    {
        TMDbCacheType.TrendingMovies => "trending_movies.json",
        TMDbCacheType.TrendingShows => "trending_shows.json",
        TMDbCacheType.AiringToday => "airing_today.json",
        TMDbCacheType.UpcomingMovies => "upcoming_movies.json",
        TMDbCacheType.TopRatedMovies => "top_rated_movies.json",
        TMDbCacheType.TopRatedShows => "top_rated_shows.json",
        TMDbCacheType.NowPlayingMovies => "now_playing_movies.json",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown TMDb cache type.")
    };
}
