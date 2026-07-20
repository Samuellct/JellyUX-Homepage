namespace Jellyfin.Plugin.JuxHomepage.Watchlist.Models;

/// <summary>
/// A single watched movie for one user, cached exhaustively (the full watch history, not a capped
/// sample) by <see cref="MovieHistoryCacheService"/>.
/// </summary>
public sealed class MovieHistoryEntry
{
    /// <summary>Gets or sets the movie's item identifier.</summary>
    public Guid ItemId { get; set; }

    /// <summary>Gets or sets the movie's display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the date this user last played the movie, or null if not recorded.</summary>
    public DateTime? LastPlayedDate { get; set; }
}
