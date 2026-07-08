using System.Globalization;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.TMDb;

/// <summary>
/// Cross-references external cache items (<see cref="IExternalCacheItem"/>) against the local
/// Jellyfin library, by IMDb ID falling back to the provider's own ID, setting
/// <see cref="IExternalCacheItem.LibraryItemId"/> on each matched item.
/// <para>
/// Extracted from <see cref="TMDbCacheService"/> (TODO_V2.md Phase 7.3) so a future second external
/// data provider can reuse the same cross-referencing logic without duplicating it. Depends only on
/// <see cref="ILibraryManager"/> and an external-id fetcher function -- no TMDb-specific coupling.
/// </para>
/// </summary>
public sealed class LibraryCrossReferencer
{
    /// <summary>Maximum number of items cross-referenced concurrently.</summary>
    private const int MaxConcurrency = 5;

    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryCrossReferencer"/> class.
    /// </summary>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="logger">Logger.</param>
    public LibraryCrossReferencer(ILibraryManager libraryManager, ILogger logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Sets <see cref="IExternalCacheItem.LibraryItemId"/> on each item by looking it up in the local
    /// library, primarily by IMDb ID (fetched via <paramref name="getExternalImdbId"/>), falling
    /// back to a direct provider-id match when no IMDb ID is available or no match is found. Items are
    /// processed with bounded concurrency (both the external IMDb ID HTTP lookup and the local
    /// library query per item) -- <see cref="ILibraryManager"/> does not support matching against a
    /// list of provider id values in a single query (<c>InternalItemsQuery.HasAnyProviderId</c> is a
    /// dictionary, one exact value per provider key), so <see cref="FindLibraryMatch"/> itself cannot
    /// be batched across items; parallelizing the per-item pipeline is the available alternative.
    /// </summary>
    /// <typeparam name="T">The external cache item type.</typeparam>
    /// <param name="items">The items to cross-reference, mutated in place.</param>
    /// <param name="getExternalImdbId">Fetches the IMDb ID for a given provider id, if known.</param>
    /// <param name="includeItemTypes">The Jellyfin item kinds to search within.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of items that were matched to a local library item.</returns>
    public async Task<int> CrossReferenceAsync<T>(
        IReadOnlyList<T> items,
        Func<int, CancellationToken, Task<string?>> getExternalImdbId,
        BaseItemKind[] includeItemTypes,
        CancellationToken cancellationToken)
        where T : IExternalCacheItem
    {
        using var semaphore = new SemaphoreSlim(MaxConcurrency);

        var tasks = items.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
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
                return match is not null;
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var matched = results.Count(wasMatched => wasMatched);

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
}
