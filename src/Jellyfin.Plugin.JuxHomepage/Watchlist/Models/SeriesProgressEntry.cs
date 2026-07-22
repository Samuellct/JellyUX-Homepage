namespace Jellyfin.Plugin.JuxHomepage.Watchlist.Models;

/// <summary>
/// A single series' watch progress for one user, cached exhaustively (every series with at least one
/// watched episode, not a capped sample) by <see cref="SeriesProgressCacheService"/>.
/// </summary>
public sealed class SeriesProgressEntry
{
    /// <summary>Gets or sets the series' item identifier.</summary>
    public Guid SeriesId { get; set; }

    /// <summary>Gets or sets the series' display name.</summary>
    public string SeriesName { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of episodes this user has watched.</summary>
    public int WatchedEpisodes { get; set; }

    /// <summary>Gets or sets the total number of episodes in the series.</summary>
    public int TotalEpisodes { get; set; }

    /// <summary>
    /// Gets or sets the most recent play date across this series' episodes for this user, or null if
    /// none have a recorded play date.
    /// </summary>
    public DateTime? LastPlayedDate { get; set; }

    /// <summary>
    /// Gets or sets the item identifier of the episode with the most recent
    /// <see cref="LastPlayedDate"/> among this series' watched episodes -- the "last episode watched"
    /// the Series Progress view (TODO_V3.md Phase 6.1) shows per series. Null if no watched episode
    /// has a recorded play date.
    /// </summary>
    public Guid? LastEpisodeId { get; set; }

    /// <summary>Gets or sets the display name of <see cref="LastEpisodeId"/>'s episode.</summary>
    public string? LastEpisodeName { get; set; }

    /// <summary>Gets or sets the season number (<c>Episode.ParentIndexNumber</c>) of <see cref="LastEpisodeId"/>'s episode.</summary>
    public int? LastEpisodeSeasonNumber { get; set; }

    /// <summary>Gets or sets the episode number (<c>Episode.IndexNumber</c>) of <see cref="LastEpisodeId"/>'s episode.</summary>
    public int? LastEpisodeIndexNumber { get; set; }
}
