using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.JuxHomepage.Widgets.Connected;

/// <summary>
/// Abstract base class for JellyUX Homepage connected widgets.
/// <para>
/// Connected widgets display TMDb data (trending/on the air/top rated/now playing/discover) that
/// has been cross-referenced
/// against the local Jellyfin library by <see cref="ITMDbCacheService"/>. Only cached entries that
/// carry a non-null <see cref="ITMDbCacheItem.LibraryItemId"/> -- i.e. items the user actually owns
/// -- are ever displayed; a cached entry with no local match is not a playable Jellyfin item and is
/// silently excluded.
/// </para>
/// <para>
/// Single-instance, like <see cref="Native.NativeWidgetBase"/>: there is exactly one "Trending
/// Movies" section, not one per scored/chosen value.
/// </para>
/// </summary>
/// <typeparam name="T">The cached TMDb item type (<see cref="TMDbMovie"/> or <see cref="TMDbShow"/>).</typeparam>
public abstract class ConnectedWidgetBase<T> : IWidget
    where T : ITMDbCacheItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedWidgetBase{T}"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="dtoService">Jellyfin DTO projection service.</param>
    /// <param name="cacheService">TMDb disk cache service.</param>
    protected ConnectedWidgetBase(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ITMDbCacheService cacheService)
    {
        UserManager = userManager;
        LibraryManager = libraryManager;
        DtoService = dtoService;
        CacheService = cacheService;
    }

    /// <summary>Gets the Jellyfin user manager.</summary>
    protected IUserManager UserManager { get; }

    /// <summary>Gets the Jellyfin library manager.</summary>
    protected ILibraryManager LibraryManager { get; }

    /// <summary>Gets the Jellyfin DTO projection service.</summary>
    protected IDtoService DtoService { get; }

    /// <summary>Gets the TMDb disk cache service.</summary>
    protected ITMDbCacheService CacheService { get; }

    /// <inheritdoc/>
    public abstract string WidgetType { get; }

    /// <inheritdoc/>
    public abstract string DefaultDisplayName { get; }

    /// <summary>Gets the default view mode identifier for this widget's cards.</summary>
    public abstract string DefaultViewMode { get; }

    /// <inheritdoc/>
    public virtual int DefaultMinItems => 4;

    /// <summary>Gets the optional front-end route for browsing all items of this type.</summary>
    public virtual string? Route => null;

    /// <inheritdoc/>
    public virtual WidgetCategory Category => WidgetCategory.Connected;

    /// <inheritdoc/>
    public virtual int MaxInstances => 1;

    /// <summary>
    /// Returns the currently cached TMDb items for this widget's data set (e.g. trending movies).
    /// </summary>
    /// <param name="payload">
    /// The request payload. Fixed single-instance widgets ignore it; multi-instance widgets (e.g.
    /// Discover) read <see cref="WidgetPayload.AdditionalData"/> to identify which instance's cache
    /// to read.
    /// </param>
    /// <returns>The cached items, or an empty list if no refresh has completed yet.</returns>
    protected abstract IReadOnlyList<T> GetCachedItems(WidgetPayload payload);

    /// <inheritdoc/>
    public IEnumerable<IWidget> CreateInstances(Guid userId, WidgetInstanceConfig config, int count)
    {
        yield return this;
    }

    /// <inheritdoc/>
    public Task<WidgetResult> GetItemsAsync(WidgetPayload payload, CancellationToken cancellationToken)
    {
        User? user = UserManager.GetUserById(payload.UserId);
        if (user is null)
        {
            return Task.FromResult(WidgetResult.Empty);
        }

        var libraryIds = GetCachedItems(payload)
            .Where(i => i.LibraryItemId.HasValue)
            .Select(i => i.LibraryItemId!.Value)
            .ToList();

        if (libraryIds.Count == 0)
        {
            return Task.FromResult(WidgetResult.Empty);
        }

        var dtoOptions = BuildDtoOptions();

        // This is an in-memory id list (already resolved by the cache's cross-referencing), not a
        // live InternalItemsQuery, so pagination is a plain Skip/Take -- the same pattern
        // MyMediaWidget uses over its in-memory library folder list.
        var items = new List<BaseItem>();
        foreach (var id in libraryIds.Skip(payload.StartIndex).Take(payload.Limit))
        {
            var item = LibraryManager.GetItemById(id);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        var dtos = DtoService.GetBaseItemDtos(items, dtoOptions, user);
        return Task.FromResult(new WidgetResult(dtos, libraryIds.Count));
    }

    /// <inheritdoc/>
    public WidgetDescriptor GetDescriptor() => new()
    {
        WidgetType = WidgetType,
        DisplayName = DefaultDisplayName,
        Category = Category,
        ViewMode = DefaultViewMode,
        Route = Route,
        MinItems = DefaultMinItems
    };

    /// <summary>
    /// Builds a standard <see cref="DtoOptions"/> for connected widget queries.
    /// Requests primary image aspect ratio, creation date, thumbnail, and backdrop images.
    /// </summary>
    /// <returns>A pre-configured <see cref="DtoOptions"/> instance.</returns>
    protected static DtoOptions BuildDtoOptions() => new()
    {
        Fields =
        [
            ItemFields.PrimaryImageAspectRatio,
            ItemFields.DateCreated
        ],
        ImageTypeLimit = 1,
        ImageTypes =
        [
            ImageType.Primary,
            ImageType.Thumb,
            ImageType.Backdrop
        ]
    };
}
