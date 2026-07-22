using Jellyfin.Plugin.JuxHomepage.Widgets;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.Watchlist;

/// <inheritdoc cref="IMovieHistoryViewService"/>
public sealed class MovieHistoryViewService : IMovieHistoryViewService
{
    private readonly IMovieHistoryCacheService _movieHistoryCache;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IDtoService _dtoService;
    private readonly ILogger<MovieHistoryViewService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieHistoryViewService"/> class.
    /// </summary>
    /// <param name="movieHistoryCache">Movie History disk cache.</param>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="dtoService">Jellyfin DTO projection service.</param>
    /// <param name="logger">Logger.</param>
    public MovieHistoryViewService(
        IMovieHistoryCacheService movieHistoryCache,
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ILogger<MovieHistoryViewService> logger)
    {
        _movieHistoryCache = movieHistoryCache;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _dtoService = dtoService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public WidgetResult GetItems(
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
            return WidgetResult.Empty;
        }

        var entries = _movieHistoryCache.GetHistory(userId);
        var sorted = Sort(entries, sortBy, sortOrder);
        var page = sorted.Skip(startIndex).Take(limit).ToList();

        if (page.Count == 0)
        {
            return new WidgetResult([], entries.Count);
        }

        // Same batched-hydration + order-preservation pattern as SeriesProgressViewService /
        // Widgets/Connected/ConnectedWidgetBase.cs -- GetItemList with ItemIds does not preserve
        // input order.
        var dtoOptions = WidgetDtoOptions.Standard();
        var itemsById = _libraryManager
            .GetItemList(new InternalItemsQuery { ItemIds = page.Select(e => e.ItemId).ToArray() })
            .ToDictionary(i => i.Id);

        var resolvedItems = new List<BaseItem>(page.Count);
        var unresolvedCount = 0;

        foreach (var entry in page)
        {
            if (itemsById.TryGetValue(entry.ItemId, out var item))
            {
                resolvedItems.Add(item);
            }
            else
            {
                unresolvedCount++;
            }
        }

        if (unresolvedCount > 0)
        {
            _logger.LogDebug(
                "{Count} cached Movie History entrie(s) no longer resolve to a local library item (likely removed since the last cache refresh).",
                unresolvedCount);
        }

        if (resolvedItems.Count == 0)
        {
            return new WidgetResult([], entries.Count);
        }

        var dtos = _dtoService.GetBaseItemDtos(resolvedItems, dtoOptions, user);
        return new WidgetResult(dtos, entries.Count);
    }

    private static IReadOnlyList<Models.MovieHistoryEntry> Sort(
        IReadOnlyList<Models.MovieHistoryEntry> entries,
        string? sortBy,
        string? sortOrder)
    {
        var descending = !string.Equals(sortOrder, "Ascending", StringComparison.OrdinalIgnoreCase);

        IOrderedEnumerable<Models.MovieHistoryEntry> ordered = string.Equals(sortBy, "Name", StringComparison.OrdinalIgnoreCase)
            ? (descending
                ? entries.OrderByDescending(e => e.Name, StringComparer.OrdinalIgnoreCase)
                : entries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
            : (descending
                ? entries.OrderByDescending(e => e.LastPlayedDate ?? DateTime.MinValue)
                : entries.OrderBy(e => e.LastPlayedDate ?? DateTime.MinValue));

        return ordered.ToList();
    }
}
