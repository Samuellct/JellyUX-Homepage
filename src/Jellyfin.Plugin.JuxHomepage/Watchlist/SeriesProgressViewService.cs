using Jellyfin.Plugin.JuxHomepage.Watchlist.Models;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.Watchlist;

/// <inheritdoc cref="ISeriesProgressViewService"/>
public sealed class SeriesProgressViewService : ISeriesProgressViewService
{
    private readonly ISeriesProgressCacheService _seriesProgressCache;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IDtoService _dtoService;
    private readonly ILogger<SeriesProgressViewService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeriesProgressViewService"/> class.
    /// </summary>
    /// <param name="seriesProgressCache">Series Progress disk cache.</param>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="dtoService">Jellyfin DTO projection service.</param>
    /// <param name="logger">Logger.</param>
    public SeriesProgressViewService(
        ISeriesProgressCacheService seriesProgressCache,
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ILogger<SeriesProgressViewService> logger)
    {
        _seriesProgressCache = seriesProgressCache;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _dtoService = dtoService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public SeriesProgressResult GetItems(
        Guid userId,
        string? sortBy,
        string? sortOrder,
        int startIndex,
        int limit,
        CancellationToken cancellationToken)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return SeriesProgressResult.Empty;
        }

        var entries = _seriesProgressCache.GetProgress(userId);
        var sorted = Sort(entries, sortBy, sortOrder);
        var page = sorted.Skip(startIndex).Take(limit).ToList();

        if (page.Count == 0)
        {
            return new SeriesProgressResult { Items = [], TotalRecordCount = entries.Count };
        }

        // Resolve the page's items in a single batched query rather than one GetItemById call per
        // id. Note: ILibraryManager.GetItemList with InternalItemsQuery.ItemIds does NOT preserve the
        // order of the ids passed in, so the result is re-keyed into a dictionary and re-read back
        // out in the cache's own sorted order, rather than used as-is (same pattern as
        // Widgets/Connected/ConnectedWidgetBase.cs).
        var dtoOptions = WidgetDtoOptions.Standard();
        var itemsById = _libraryManager
            .GetItemList(new InternalItemsQuery { ItemIds = page.Select(e => e.SeriesId).ToArray() })
            .ToDictionary(i => i.Id);

        var resolvedItems = new List<BaseItem>(page.Count);
        var resolvedEntries = new List<SeriesProgressEntry>(page.Count);
        var unresolvedCount = 0;

        foreach (var entry in page)
        {
            if (itemsById.TryGetValue(entry.SeriesId, out var item))
            {
                resolvedItems.Add(item);
                resolvedEntries.Add(entry);
            }
            else
            {
                unresolvedCount++;
            }
        }

        if (unresolvedCount > 0)
        {
            _logger.LogDebug(
                "{Count} cached Series Progress entrie(s) no longer resolve to a local library item (likely removed since the last cache refresh).",
                unresolvedCount);
        }

        if (resolvedItems.Count == 0)
        {
            return new SeriesProgressResult { Items = [], TotalRecordCount = entries.Count };
        }

        var dtos = _dtoService.GetBaseItemDtos(resolvedItems, dtoOptions, user);

        var items = new List<SeriesProgressItem>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            var entry = resolvedEntries[i];
            items.Add(new SeriesProgressItem
            {
                Item = dtos[i],
                WatchedEpisodes = entry.WatchedEpisodes,
                TotalEpisodes = entry.TotalEpisodes,
                LastPlayedDate = entry.LastPlayedDate,
                LastEpisodeName = entry.LastEpisodeName,
                LastEpisodeSeasonNumber = entry.LastEpisodeSeasonNumber,
                LastEpisodeIndexNumber = entry.LastEpisodeIndexNumber
            });
        }

        return new SeriesProgressResult { Items = items, TotalRecordCount = entries.Count };
    }

    /// <summary>
    /// Sorts the cached entries in memory -- these are precomputed, exhaustive per-user lists (not a
    /// live library query), so sorting here rather than in <see cref="ISeriesProgressCacheService"/>
    /// keeps the cache itself agnostic of view-specific sort options.
    /// </summary>
    private static IReadOnlyList<SeriesProgressEntry> Sort(
        IReadOnlyList<SeriesProgressEntry> entries,
        string? sortBy,
        string? sortOrder)
    {
        var descending = !string.Equals(sortOrder, "Ascending", StringComparison.OrdinalIgnoreCase);

        IOrderedEnumerable<SeriesProgressEntry> ordered = string.Equals(sortBy, "Name", StringComparison.OrdinalIgnoreCase)
            ? (descending
                ? entries.OrderByDescending(e => e.SeriesName, StringComparer.OrdinalIgnoreCase)
                : entries.OrderBy(e => e.SeriesName, StringComparer.OrdinalIgnoreCase))
            : (descending
                ? entries.OrderByDescending(e => e.LastPlayedDate ?? DateTime.MinValue)
                : entries.OrderBy(e => e.LastPlayedDate ?? DateTime.MinValue));

        return ordered.ToList();
    }
}
