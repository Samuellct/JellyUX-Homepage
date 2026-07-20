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
}
