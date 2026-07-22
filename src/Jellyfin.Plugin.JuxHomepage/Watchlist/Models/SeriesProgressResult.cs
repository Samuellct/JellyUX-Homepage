namespace Jellyfin.Plugin.JuxHomepage.Watchlist.Models;

/// <summary>
/// A page of <see cref="SeriesProgressItem"/> results, with the total record count for pagination.
/// </summary>
public sealed class SeriesProgressResult
{
    /// <summary>Gets the page of items.</summary>
    public required IReadOnlyList<SeriesProgressItem> Items { get; init; }

    /// <summary>Gets the total number of series in progress for this user, before pagination.</summary>
    public int TotalRecordCount { get; init; }

    /// <summary>Gets an empty result.</summary>
    public static SeriesProgressResult Empty { get; } = new() { Items = [], TotalRecordCount = 0 };
}
