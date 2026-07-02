using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.JuxHomepage.Widgets.Connected;

/// <summary>
/// Displays TV shows currently on the air (from TMDb's <c>tv/on_the_air</c> endpoint -- an episode
/// airing within the next 7 days) that are present in the local library.
/// </summary>
public sealed class AiringTodayShowsWidget : ConnectedWidgetBase<TMDbShow>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiringTodayShowsWidget"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="dtoService">Jellyfin DTO projection service.</param>
    /// <param name="cacheService">TMDb disk cache service.</param>
    public AiringTodayShowsWidget(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ITMDbCacheService cacheService)
        : base(userManager, libraryManager, dtoService, cacheService)
    {
    }

    /// <inheritdoc/>
    public override string WidgetType => "jux.connected.airing-today";

    /// <inheritdoc/>
    public override string DefaultDisplayName => "On TV";

    /// <inheritdoc/>
    public override string DefaultViewMode => WidgetViewMode.Portrait;

    /// <inheritdoc/>
    protected override IReadOnlyList<TMDbShow> GetCachedItems(WidgetPayload payload) => CacheService.GetAiringToday();
}
