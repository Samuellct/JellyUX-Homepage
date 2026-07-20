using Jellyfin.Data.Enums;
using Jellyfin.Plugin.JuxHomepage.IO;
using Jellyfin.Plugin.JuxHomepage.Library.Models;
using Jellyfin.Plugin.JuxHomepage.TMDb;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.Library;

/// <summary>
/// Reads and refreshes the on-disk Collections reverse index under
/// <c>DataPath/Jellyfin.Plugin.JuxHomepage/cache/library/collections-index.json</c> -- a single
/// global file (not per-user), since collection membership does not depend on which user is asking.
/// Reuses <see cref="DiskJsonCache{T}"/> (TODO_V2.md Phase 7.2), same TTL/refresh-lock shape as
/// <see cref="TMDbCacheService"/>.
/// </summary>
public sealed class CollectionsIndexCacheService : ICollectionsIndexCacheService, IDisposable
{
    private const string FileName = "collections-index.json";
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    private readonly DiskJsonCache<CollectionMembership> _cache;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<CollectionsIndexCacheService> _logger;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionsIndexCacheService"/> class.
    /// </summary>
    /// <param name="applicationPaths">Provides the application data directory path.</param>
    /// <param name="fileSystem">File system abstraction, for testability.</param>
    /// <param name="libraryManager">Jellyfin library manager, used to enumerate collections and their members.</param>
    /// <param name="logger">Logger.</param>
    public CollectionsIndexCacheService(
        IApplicationPaths applicationPaths,
        IFileSystem fileSystem,
        ILibraryManager libraryManager,
        ILogger<CollectionsIndexCacheService> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;

        var cacheDir = Path.Combine(
            applicationPaths.DataPath,
            "Jellyfin.Plugin.JuxHomepage",
            "cache",
            "library");

        _cache = new DiskJsonCache<CollectionMembership>(cacheDir, fileSystem, logger);
    }

    /// <inheritdoc/>
    public IReadOnlyList<CollectionRef> GetCollectionsFor(Guid itemId)
    {
        var entry = _cache.Read(FileName).FirstOrDefault(m => m.ItemId == itemId);
        return entry?.Collections ?? [];
    }

    /// <inheritdoc/>
    public bool IsStale() => _cache.IsStale(FileName, Ttl);

    /// <inheritdoc/>
    public bool TryAcquireRefreshLock() => _refreshGate.Wait(0);

    /// <inheritdoc/>
    public Task RunRefreshLockedAsync(CancellationToken cancellationToken)
    {
        try
        {
            var index = ComputeIndex(cancellationToken);
            _cache.WriteUnlessEmpty(FileName, index);
            _logger.LogInformation("Collections index refreshed: {Count} item(s) with at least one collection.", index.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh collections index.");
        }
        finally
        {
            _refreshGate.Release();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<bool> RefreshAsync(CancellationToken cancellationToken)
    {
        if (!TryAcquireRefreshLock())
        {
            _logger.LogInformation("Collections index refresh already in progress; skipping this request.");
            return false;
        }

        await RunRefreshLockedAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cache.Dispose();
            _refreshGate.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Builds the reverse index by enumerating every BoxSet in the library and, for each, its member
    /// items (via <see cref="InternalItemsQuery.AncestorIds"/> -- the same parent/collection linkage
    /// Jellyfin's own Items API uses to browse a collection's contents, avoiding any dependency on a
    /// specific requesting user since this index is global).
    /// </summary>
    private IReadOnlyList<CollectionMembership> ComputeIndex(CancellationToken cancellationToken)
    {
        var boxSets = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.BoxSet],
            Recursive = true,
            DtoOptions = new MediaBrowser.Controller.Dto.DtoOptions { Fields = [] }
        });

        var byItem = new Dictionary<Guid, List<CollectionRef>>();

        foreach (var boxSet in boxSets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var members = _libraryManager.GetItemList(new InternalItemsQuery
            {
                AncestorIds = [boxSet.Id],
                IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
                Recursive = true,
                DtoOptions = new MediaBrowser.Controller.Dto.DtoOptions { Fields = [] }
            });

            var collectionRef = new CollectionRef { CollectionId = boxSet.Id, CollectionName = boxSet.Name };

            foreach (var member in members)
            {
                if (!byItem.TryGetValue(member.Id, out var collections))
                {
                    collections = [];
                    byItem[member.Id] = collections;
                }

                collections.Add(collectionRef);
            }
        }

        return byItem
            .Select(kv => new CollectionMembership { ItemId = kv.Key, Collections = kv.Value })
            .ToList();
    }
}
