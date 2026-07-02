namespace Jellyfin.Plugin.JuxHomepage.TMDb;

/// <summary>
/// Widget type identifiers that <see cref="TMDbCacheService"/> needs to recognize without taking a
/// dependency on the <c>Widgets</c> namespace (which itself depends on <c>TMDb</c>).
/// </summary>
public static class TMDbWidgetTypes
{
    /// <summary>The widget type identifier for a customizable Discover Movies instance.</summary>
    public const string DiscoverMovies = "jux.connected.discover-movies";
}
