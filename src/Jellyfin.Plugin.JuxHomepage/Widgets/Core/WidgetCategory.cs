namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Classifies a widget by its data source and administrative context.
/// </summary>
public enum WidgetCategory
{
    /// <summary>Native Jellyfin library data (e.g. Continue Watching, Latest Media).</summary>
    Native,

    /// <summary>Requires administrator configuration (e.g. server statistics).</summary>
    Admin,

    /// <summary>Personalized per-user data (e.g. Favorites, Watchlist).</summary>
    Personalized,

    /// <summary>External data source requiring an API key (e.g. TMDb trending).</summary>
    Connected
}
