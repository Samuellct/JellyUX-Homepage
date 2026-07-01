using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;

/// <summary>
/// Displays movies featuring the requesting user's favorite actors, derived from their watch
/// history. Fans out into one section per top-scored actor (see <see cref="ScoringService.GetTopActors"/>).
/// </summary>
public sealed class FavoriteActorWidget : PersonalizedPersonWidgetBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FavoriteActorWidget"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="dtoService">Jellyfin DTO projection service.</param>
    /// <param name="scoringService">User preference scoring service.</param>
    public FavoriteActorWidget(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ScoringService scoringService)
        : base(userManager, libraryManager, dtoService, scoringService)
    {
    }

    /// <inheritdoc/>
    public override string WidgetType => "jux.personalized.favorite-actor";

    /// <inheritdoc/>
    public override string DefaultDisplayName => "Favorite Actor";

    /// <inheritdoc/>
    protected override string PersonType => "Actor";

    /// <inheritdoc/>
    protected override IReadOnlyList<ScoredValue> GetScoredValues(Guid userId, int count) =>
        ScoringService.GetTopActors(userId, count);

    /// <inheritdoc/>
    protected override string FormatDisplayName(string label) => $"Movies with {label}";
}
