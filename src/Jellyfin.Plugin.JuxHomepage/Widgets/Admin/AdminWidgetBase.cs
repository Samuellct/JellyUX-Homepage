using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.JuxHomepage.Widgets.Admin;

/// <summary>
/// Abstract base class for JellyUX Homepage admin widgets.
/// <para>
/// Admin widgets display items filtered by a value chosen by the administrator (e.g. a specific
/// genre, actor, studio, collection, tag, or year). The chosen value is stored in
/// <c>WidgetConfig.ExtraParams["value"]</c> and forwarded as <see cref="WidgetPayload.AdditionalData"/>
/// at runtime.
/// </para>
/// <para>
/// Multiple sections of the same admin widget type are modeled as multiple
/// <see cref="WidgetConfig"/> rows sharing the same <see cref="IWidget.WidgetType"/>, each with a
/// different <c>ExtraParams["value"]</c>.
/// </para>
/// </summary>
public abstract class AdminWidgetBase : IWidget
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminWidgetBase"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="dtoService">Jellyfin DTO projection service.</param>
    protected AdminWidgetBase(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService)
    {
        UserManager = userManager;
        LibraryManager = libraryManager;
        DtoService = dtoService;
    }

    /// <summary>Gets the Jellyfin user manager.</summary>
    protected IUserManager UserManager { get; }

    /// <summary>Gets the Jellyfin library manager.</summary>
    protected ILibraryManager LibraryManager { get; }

    /// <summary>Gets the Jellyfin DTO projection service.</summary>
    protected IDtoService DtoService { get; }

    /// <inheritdoc/>
    public abstract string WidgetType { get; }

    /// <inheritdoc/>
    public abstract string DefaultDisplayName { get; }

    /// <summary>Gets the default view mode identifier for this widget's cards.</summary>
    public abstract string DefaultViewMode { get; }

    /// <inheritdoc/>
    public abstract int DefaultMinItems { get; }

    /// <inheritdoc/>
    public virtual WidgetCategory Category => WidgetCategory.Admin;

    /// <summary>
    /// Gets the maximum number of instances (rows) an administrator can create for this type.
    /// Defaults to 5.
    /// </summary>
    public virtual int MaxInstances => 5;

    /// <summary>
    /// Gets the item types to include in the query.
    /// Override and return <see langword="null"/> to include all item types (e.g. for collections).
    /// </summary>
    protected virtual BaseItemKind[]? IncludeItemTypes => [BaseItemKind.Movie, BaseItemKind.Series];

    /// <inheritdoc/>
    public IEnumerable<IWidget> CreateInstances(Guid userId, WidgetInstanceConfig config, int count)
    {
        yield return this;
    }

    /// <summary>
    /// Applies the widget-specific filter to the query using the provided value.
    /// Called by <see cref="GetItemsAsync"/> after the query is initialized.
    /// </summary>
    /// <param name="query">The query to modify.</param>
    /// <param name="value">The value from <c>ExtraParams["value"]</c> / <see cref="WidgetPayload.AdditionalData"/>.</param>
    protected abstract void ApplyFilter(InternalItemsQuery query, string value);

    /// <summary>
    /// Returns the list of available values for this widget type, optionally filtered by a search term.
    /// Used by the <c>GET /JuxHomepage/Widget/{widgetType}/values</c> autocomplete endpoint.
    /// </summary>
    /// <param name="user">The requesting Jellyfin user.</param>
    /// <param name="search">Optional search string for filtering results.</param>
    /// <returns>A list of <see cref="AdminWidgetValue"/> options.</returns>
    public abstract IReadOnlyList<AdminWidgetValue> GetAvailableValues(User user, string? search);

    /// <inheritdoc/>
    public Task<WidgetResult> GetItemsAsync(WidgetPayload payload, CancellationToken cancellationToken)
    {
        // Resolve the instance value: prefer AdditionalData (set from ExtraParams["value"] by
        // WidgetService.BuildInstanceConfig), fall back to ExtraParams["value"] for direct calls.
        string? value = payload.AdditionalData;
        if (string.IsNullOrEmpty(value) && payload.ExtraParams is not null)
        {
            payload.ExtraParams.TryGetValue("value", out value);
        }

        if (string.IsNullOrEmpty(value))
        {
            return Task.FromResult(WidgetResult.Empty);
        }

        User? user = UserManager.GetUserById(payload.UserId);
        if (user is null)
        {
            return Task.FromResult(WidgetResult.Empty);
        }

        var dtoOptions = BuildDtoOptions();
        var query = new InternalItemsQuery(user)
        {
            Recursive = true,
            IsMissing = false,
            EnableTotalRecordCount = true,
            StartIndex = payload.StartIndex,
            Limit = payload.Limit,
            DtoOptions = dtoOptions
        };

        if (IncludeItemTypes is not null)
        {
            query.IncludeItemTypes = IncludeItemTypes;
        }

        ApplyFilter(query, value);

        QueryResult<BaseItem> result = LibraryManager.GetItemsResult(query);
        var dtos = DtoService.GetBaseItemDtos(result.Items, dtoOptions, user);
        return Task.FromResult(new WidgetResult(dtos, result.TotalRecordCount));
    }

    /// <inheritdoc/>
    public WidgetDescriptor GetDescriptor() => new()
    {
        WidgetType = WidgetType,
        DisplayName = DefaultDisplayName,
        Category = Category,
        ViewMode = DefaultViewMode,
        MinItems = DefaultMinItems
    };

    /// <summary>
    /// Builds a standard <see cref="DtoOptions"/> for admin widget queries.
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
