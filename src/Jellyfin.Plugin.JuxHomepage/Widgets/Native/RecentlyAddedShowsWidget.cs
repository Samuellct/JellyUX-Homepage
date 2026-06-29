using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.JuxHomepage.Widgets.Native;

/// <summary>
/// Native widget that shows recently added TV series, ordered by creation date.
/// </summary>
public sealed class RecentlyAddedShowsWidget : RecentlyAddedWidgetBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecentlyAddedShowsWidget"/> class.
    /// </summary>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="dtoService">Jellyfin DTO projection service.</param>
    public RecentlyAddedShowsWidget(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService)
        : base(userManager, libraryManager, dtoService)
    {
    }

    /// <inheritdoc/>
    public override string WidgetType => "jux.native.recently-added-shows";

    /// <inheritdoc/>
    public override string DefaultDisplayName => "Recently Added Shows";

    /// <inheritdoc/>
    public override int DefaultMinItems => 4;

    /// <inheritdoc/>
    public override string DefaultViewMode => WidgetViewMode.Portrait;

    /// <inheritdoc/>
    public override string? Route => "tvshows";

    /// <inheritdoc/>
    protected override BaseItemKind ItemKind => BaseItemKind.Series;
}
