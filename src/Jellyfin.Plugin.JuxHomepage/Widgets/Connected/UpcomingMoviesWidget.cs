using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.JuxHomepage.Widgets.Connected;

/// <summary>
/// Displays upcoming movie releases (from TMDb) that are present in the local library.
/// </summary>
public sealed class UpcomingMoviesWidget : ConnectedWidgetBase<TMDbMovie>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpcomingMoviesWidget"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="dtoService">Jellyfin DTO projection service.</param>
    /// <param name="cacheService">TMDb disk cache service.</param>
    public UpcomingMoviesWidget(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ITMDbCacheService cacheService)
        : base(userManager, libraryManager, dtoService, cacheService)
    {
    }

    /// <inheritdoc/>
    public override string WidgetType => "jux.connected.upcoming-movies";

    /// <inheritdoc/>
    public override string DefaultDisplayName => "Upcoming Movies";

    /// <inheritdoc/>
    public override string DefaultViewMode => WidgetViewMode.Portrait;

    /// <inheritdoc/>
    protected override IReadOnlyList<TMDbMovie> GetCachedItems() => CacheService.GetUpcomingMovies();
}
