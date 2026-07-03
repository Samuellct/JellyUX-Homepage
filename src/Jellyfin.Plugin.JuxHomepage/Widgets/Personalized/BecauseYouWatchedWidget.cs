using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.JuxHomepage.Localization;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;

/// <summary>
/// Displays movies related to films the requesting user recently watched, sharing at least one
/// genre with the reference film. Fans out into one section per recently watched film (see
/// <see cref="ScoringService.GetRecentlyWatched"/>).
/// </summary>
public sealed class BecauseYouWatchedWidget : PersonalizedWidgetBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BecauseYouWatchedWidget"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="dtoService">Jellyfin DTO projection service.</param>
    /// <param name="scoringService">User preference scoring service.</param>
    /// <param name="localizationService">Widget display-name translation service.</param>
    public BecauseYouWatchedWidget(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ScoringService scoringService,
        ILocalizationService localizationService)
        : base(userManager, libraryManager, dtoService, scoringService, localizationService)
    {
    }

    /// <inheritdoc/>
    public override string WidgetType => "jux.personalized.because-you-watched";

    /// <inheritdoc/>
    public override string DefaultDisplayName => "Because You Watched";

    /// <inheritdoc/>
    public override string DefaultViewMode => WidgetViewMode.Portrait;

    /// <inheritdoc/>
    protected override BaseItemKind[] IncludeItemTypes => [BaseItemKind.Movie];

    /// <inheritdoc/>
    protected override IReadOnlyList<ScoredValue> GetScoredValues(Guid userId, int count) =>
        ScoringService.GetRecentlyWatched(userId, count);

    /// <inheritdoc/>
    protected override string FormatDisplayName(string label, string? lang) =>
        LocalizationService.Translate("jux.personalized.because-you-watched.format", lang, label);

    /// <inheritdoc/>
    protected override void ApplyFilter(InternalItemsQuery query, string value, User user)
    {
        if (!Guid.TryParse(value, out var referenceId))
        {
            return;
        }

        var reference = LibraryManager.GetItemById(referenceId);
        if (reference is null)
        {
            return;
        }

        // Recommend films sharing at least one genre with the reference film, in random order,
        // always excluding the reference film itself.
        query.Genres = reference.Genres;
        query.ExcludeItemIds = [referenceId];
        query.OrderBy = [(ItemSortBy.Random, SortOrder.Ascending)];
    }
}
