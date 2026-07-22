using MediaBrowser.Model.Dto;

namespace Jellyfin.Plugin.JuxHomepage.Watchlist.Models;

/// <summary>
/// A single Series Progress view row: a hydrated <see cref="BaseItemDto"/> (for card rendering) plus
/// the progress fields from the cached <see cref="SeriesProgressEntry"/>. Not a
/// <see cref="Widgets.WidgetResult"/> -- that type only carries items + a total count, and this view
/// needs the extra per-item progress/last-episode fields alongside each item.
/// </summary>
public sealed class SeriesProgressItem
{
    /// <summary>Gets the hydrated series item, for card rendering (poster, name, etc.).</summary>
    public required BaseItemDto Item { get; init; }

    /// <summary>Gets the number of episodes this user has watched.</summary>
    public int WatchedEpisodes { get; init; }

    /// <summary>Gets the total number of episodes in the series.</summary>
    public int TotalEpisodes { get; init; }

    /// <summary>Gets the most recent play date across this series' episodes for this user.</summary>
    public DateTime? LastPlayedDate { get; init; }

    /// <summary>Gets the display name of the last-watched episode, or null if unknown.</summary>
    public string? LastEpisodeName { get; init; }

    /// <summary>Gets the season number of the last-watched episode, or null if unknown.</summary>
    public int? LastEpisodeSeasonNumber { get; init; }

    /// <summary>Gets the episode number of the last-watched episode, or null if unknown.</summary>
    public int? LastEpisodeIndexNumber { get; init; }
}
