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
    public Task RefreshTrendingMoviesAsync(CancellationToken cancellationToken) =>
        RefreshMoviesAsync(TMDbCacheType.TrendingMovies, _apiClient.GetTrendingMoviesAsync, cancellationToken);

    /// <inheritdoc/>
    public Task RefreshUpcomingMoviesAsync(CancellationToken cancellationToken) =>
        RefreshMoviesAsync(TMDbCacheType.UpcomingMovies, _apiClient.GetUpcomingMoviesAsync, cancellationToken);

    /// <inheritdoc/>
    public Task RefreshTrendingShowsAsync(CancellationToken cancellationToken) =>
        RefreshShowsAsync(TMDbCacheType.TrendingShows, _apiClient.GetTrendingShowsAsync, cancellationToken);

    /// <inheritdoc/>
    public Task RefreshAiringTodayAsync(CancellationToken cancellationToken) =>
        RefreshShowsAsync(TMDbCacheType.AiringToday, _apiClient.GetAiringTodayAsync, cancellationToken);

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
            await CrossReferenceAsync(
                items,
                _apiClient.GetMovieExternalIdsAsync,
                [BaseItemKind.Movie],
                cancellationToken).ConfigureAwait(false);
            WriteCache(type, items);
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
            await CrossReferenceAsync(
                items,
                _apiClient.GetShowExternalIdsAsync,
                [BaseItemKind.Series],
                cancellationToken).ConfigureAwait(false);
            WriteCache(type, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh TMDb cache '{Type}'.", type);
        }
    }

    /// <summary>
    /// Sets <see cref="ITMDbCacheItem.LibraryItemId"/> on each item by looking it up in the local
    /// library, primarily by IMDb ID (fetched via <paramref name="getExternalImdbId"/>), falling
    /// back to a direct TMDb ID match when no IMDb ID is available or no match is found.
    /// </summary>
    private async Task CrossReferenceAsync<T>(
        IReadOnlyList<T> items,
        Func<int, CancellationToken, Task<string?>> getExternalImdbId,
        BaseItemKind[] includeItemTypes,
        CancellationToken cancellationToken)
        where T : ITMDbCacheItem
    {
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
        }
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

    private void WriteCache<T>(TMDbCacheType type, IReadOnlyList<T> items)
    {
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
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown TMDb cache type.")
    };
}
