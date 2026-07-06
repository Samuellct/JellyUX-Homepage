namespace Jellyfin.Plugin.JuxHomepage.Configuration;

/// <summary>
/// Per-list tuning for the TMDb data cache: how many result pages to fetch (1 page = up to 20
/// items) for each fixed list, plus the region used by "Now Playing".
/// Uses explicit named properties rather than a dictionary, matching the rest of this
/// configuration -- a <c>Dictionary</c> previously broke <c>XmlSerializer</c> compatibility
/// elsewhere in this plugin (see <see cref="Widgets.WidgetExtraParam"/>).
/// </summary>
public sealed class TMDbListsConfig
{
    /// <summary>Gets or sets the number of pages (1-5) to fetch for trending movies.</summary>
    public int TrendingMoviesPages { get; set; } = 1;

    /// <summary>Gets or sets the number of pages (1-5) to fetch for trending shows.</summary>
    public int TrendingShowsPages { get; set; } = 1;

    /// <summary>Gets or sets the number of pages (1-5) to fetch for shows airing today.</summary>
    public int AiringTodayPages { get; set; } = 1;

    /// <summary>Gets or sets the number of pages (1-5) to fetch for top rated movies.</summary>
    public int TopRatedMoviesPages { get; set; } = 1;

    /// <summary>Gets or sets the number of pages (1-5) to fetch for top rated shows.</summary>
    public int TopRatedShowsPages { get; set; } = 1;

    /// <summary>Gets or sets the number of pages (1-5) to fetch for now-playing movies.</summary>
    public int NowPlayingMoviesPages { get; set; } = 1;

    /// <summary>
    /// Gets or sets the ISO 3166-1 region code (e.g. "FR") used to scope "Now Playing" to a
    /// specific country's theatrical releases. Null uses TMDb's own default.
    /// </summary>
    public string? NowPlayingRegion { get; set; }

    /// <summary>
    /// Gets or sets the minimum vote count for Top Rated Movies/Shows (shared by both), passed as
    /// discover/movie|tv's <c>vote_count.gte</c>. Defaults to 200 to approximate the old fixed
    /// <c>top_rated</c> endpoint's implicit behavior without requiring admin action. This is a
    /// temporary fix for V2 (see TODO_V2.md Phase 3.4); a library-ranking-based approach is planned
    /// for V3.
    /// </summary>
    public int TopRatedVoteCountMin { get; set; } = 200;
}
