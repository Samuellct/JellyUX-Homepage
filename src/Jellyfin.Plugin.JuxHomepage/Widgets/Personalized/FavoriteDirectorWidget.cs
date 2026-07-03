using Jellyfin.Plugin.JuxHomepage.Localization;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;

/// <summary>
/// Displays movies directed by the requesting user's favorite directors, derived from their watch
/// history. Fans out into one section per top-scored director (see
/// <see cref="ScoringService.GetTopDirectors"/>).
/// </summary>
public sealed class FavoriteDirectorWidget : PersonalizedPersonWidgetBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FavoriteDirectorWidget"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="dtoService">Jellyfin DTO projection service.</param>
    /// <param name="scoringService">User preference scoring service.</param>
    /// <param name="localizationService">Widget display-name translation service.</param>
    public FavoriteDirectorWidget(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ScoringService scoringService,
        ILocalizationService localizationService)
        : base(userManager, libraryManager, dtoService, scoringService, localizationService)
    {
    }

    /// <inheritdoc/>
    public override string WidgetType => "jux.personalized.favorite-director";

    /// <inheritdoc/>
    public override string DefaultDisplayName => "Favorite Director";

    /// <inheritdoc/>
    protected override string PersonType => "Director";

    /// <inheritdoc/>
    protected override IReadOnlyList<ScoredValue> GetScoredValues(Guid userId, int count) =>
        ScoringService.GetTopDirectors(userId, count);

    /// <inheritdoc/>
    protected override string FormatDisplayName(string label, string? lang) =>
        LocalizationService.Translate("jux.personalized.favorite-director.format", lang, label);
}
