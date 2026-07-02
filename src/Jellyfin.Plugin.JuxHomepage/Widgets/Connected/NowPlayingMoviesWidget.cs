using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.JuxHomepage.Widgets.Connected;

/// <summary>
/// Displays movies currently playing in theatres (from TMDb, optionally filtered to a configured
/// region) that are present in the local library.
/// </summary>
public sealed class NowPlayingMoviesWidget : ConnectedWidgetBase<TMDbMovie>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NowPlayingMoviesWidget"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="dtoService">Jellyfin DTO projection service.</param>
    /// <param name="cacheService">TMDb disk cache service.</param>
    public NowPlayingMoviesWidget(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ITMDbCacheService cacheService)
        : base(userManager, libraryManager, dtoService, cacheService)
    {
    }

    /// <inheritdoc/>
    public override string WidgetType => "jux.connected.now-playing-movies";

    /// <inheritdoc/>
    public override string DefaultDisplayName => "Now Playing";

    /// <inheritdoc/>
    public override string DefaultViewMode => WidgetViewMode.Portrait;

    /// <inheritdoc/>
    protected override IReadOnlyList<TMDbMovie> GetCachedItems(WidgetPayload payload) => CacheService.GetNowPlayingMovies();
}
