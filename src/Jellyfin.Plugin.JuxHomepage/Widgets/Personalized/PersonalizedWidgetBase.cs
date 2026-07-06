using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.JuxHomepage.Localization;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;

/// <summary>
/// Abstract base class for JellyUX Homepage personalized widgets.
/// <para>
/// Personalized widgets display items filtered by a value derived from the requesting user's
/// watch history via <see cref="ScoringService"/> (e.g. a favorite genre, actor, or director),
/// rather than a value chosen by an administrator.
/// </para>
/// <para>
/// A single <see cref="WidgetConfig"/> row (added by the administrator, with no value to pick)
/// fans out per user into up to <see cref="IWidget.MaxInstances"/> instances, one per scored
/// value returned by <see cref="GetScoredValues"/>. Each instance is a transient clone
/// produced by <see cref="CreateInstances"/> that self-identifies via its own
/// <see cref="WidgetDescriptor.AdditionalData"/> and <see cref="WidgetDescriptor.DisplayName"/>,
/// so a user with no matching history simply receives zero instances (no section shown).
/// </para>
/// </summary>
public abstract class PersonalizedWidgetBase : IWidget
{
    private string? _value;
    private string? _displayName;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonalizedWidgetBase"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="dtoService">Jellyfin DTO projection service.</param>
    /// <param name="scoringService">User preference scoring service.</param>
    /// <param name="localizationService">Widget display-name translation service.</param>
    protected PersonalizedWidgetBase(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ScoringService scoringService,
        ILocalizationService localizationService)
    {
        UserManager = userManager;
        LibraryManager = libraryManager;
        DtoService = dtoService;
        ScoringService = scoringService;
        LocalizationService = localizationService;
    }

    /// <summary>Gets the Jellyfin user manager.</summary>
    protected IUserManager UserManager { get; }

    /// <summary>Gets the Jellyfin library manager.</summary>
    protected ILibraryManager LibraryManager { get; }

    /// <summary>Gets the Jellyfin DTO projection service.</summary>
    protected IDtoService DtoService { get; }

    /// <summary>Gets the user preference scoring service.</summary>
    protected ScoringService ScoringService { get; }

    /// <summary>Gets the widget display-name translation service.</summary>
    protected ILocalizationService LocalizationService { get; }

    /// <inheritdoc/>
    public abstract string WidgetType { get; }

    /// <inheritdoc/>
    public abstract string DefaultDisplayName { get; }

    /// <summary>Gets the default view mode identifier for this widget's cards.</summary>
    public abstract string DefaultViewMode { get; }

    /// <inheritdoc/>
    public virtual int DefaultMinItems => 4;

    /// <inheritdoc/>
    public virtual WidgetCategory Category => WidgetCategory.Personalized;

    /// <summary>
    /// Gets the default maximum number of sections (scored values) fanned out per user.
    /// The administrator may configure a lower value via <see cref="WidgetConfig.MaxInstances"/>.
    /// </summary>
    public virtual int MaxInstances => 3;

    /// <summary>
    /// Gets the item types to include in the recommendation query. Never null, unlike
    /// <see cref="Jellyfin.Plugin.JuxHomepage.Widgets.Admin.AdminWidgetBase.IncludeItemTypes"/> --
    /// a personalized recommendation always needs a concrete type constraint (there is no
    /// "recommend across every type" case here, as there is for an admin-chosen filter value).
    /// </summary>
    protected virtual BaseItemKind[] IncludeItemTypes => [BaseItemKind.Movie, BaseItemKind.Series];

    /// <summary>
    /// Returns the user's scored values for this widget's preference dimension (e.g. top genres),
    /// most relevant first. Used by <see cref="CreateInstances"/> to fan out one instance per value.
    /// </summary>
    /// <param name="userId">The requesting user's identifier.</param>
    /// <param name="count">The maximum number of scored values to return.</param>
    /// <returns>Up to <paramref name="count"/> scored values.</returns>
    protected abstract IReadOnlyList<ScoredValue> GetScoredValues(Guid userId, int count);

    /// <summary>
    /// Formats the section display name for a scored value's label (e.g. "More Action"), translated
    /// for <paramref name="lang"/>.
    /// </summary>
    /// <param name="label">The scored value's human-readable label.</param>
    /// <param name="lang">The language to translate the format template into.</param>
    /// <returns>The display name shown for this instance's section.</returns>
    protected abstract string FormatDisplayName(string label, string? lang);

    /// <summary>
    /// Applies the widget-specific filter to the query using the resolved scored value.
    /// </summary>
    /// <param name="query">The query to modify.</param>
    /// <param name="value">The scored value to filter by.</param>
    /// <param name="user">The requesting Jellyfin user.</param>
    protected abstract void ApplyFilter(InternalItemsQuery query, string value, User user);

    /// <inheritdoc/>
    public IEnumerable<IWidget> CreateInstances(Guid userId, WidgetInstanceConfig config, int count)
    {
        foreach (var scored in GetScoredValues(userId, count))
        {
            var clone = (PersonalizedWidgetBase)MemberwiseClone();
            clone._value = scored.Value;
            clone._displayName = FormatDisplayName(scored.Label, config.Lang);
            yield return clone;
        }
    }

    /// <inheritdoc/>
    public Task<WidgetResult> GetItemsAsync(WidgetPayload payload, CancellationToken cancellationToken)
    {
        var value = payload.AdditionalData;
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
            IncludeItemTypes = IncludeItemTypes,
            Recursive = true,
            IsMissing = false,
            EnableTotalRecordCount = true,
            StartIndex = payload.StartIndex,
            Limit = payload.Limit,
            DtoOptions = dtoOptions
        };

        if (ExcludeWatched(payload))
        {
            query.IsPlayed = false;
        }

        ApplyFilter(query, value, user);

        QueryResult<BaseItem> result = LibraryManager.GetItemsResult(query);
        var dtos = DtoService.GetBaseItemDtos(result.Items, dtoOptions, user);
        return Task.FromResult(new WidgetResult(dtos, result.TotalRecordCount));
    }

    /// <inheritdoc/>
    public WidgetDescriptor GetDescriptor() => new()
    {
        WidgetType = WidgetType,
        DisplayName = _displayName ?? DefaultDisplayName,
        Category = Category,
        ViewMode = DefaultViewMode,
        AdditionalData = _value,
        MinItems = DefaultMinItems
    };

    /// <summary>
    /// Reads the row-level "exclude already watched" setting from <see cref="WidgetPayload.ExtraParams"/>.
    /// Defaults to <see langword="true"/> when not explicitly set to "false".
    /// </summary>
    /// <param name="payload">The request payload.</param>
    /// <returns><see langword="true"/> unless the administrator explicitly disabled the exclusion.</returns>
    private static bool ExcludeWatched(WidgetPayload payload)
    {
        if (payload.ExtraParams is not null &&
            payload.ExtraParams.TryGetValue("excludeWatched", out var raw) &&
            bool.TryParse(raw, out var value))
        {
            return value;
        }

        return true;
    }

    /// <summary>
    /// Builds a standard <see cref="DtoOptions"/> for personalized widget queries.
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
