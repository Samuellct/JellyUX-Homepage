namespace Jellyfin.Plugin.JuxHomepage.TMDb;

/// <summary>
/// On-disk envelope for a single TMDb cache file, wrapping the cached items with the timestamp of
/// the refresh that produced them.
/// </summary>
/// <typeparam name="T">The cached item type (<see cref="Models.TMDbMovie"/> or <see cref="Models.TMDbShow"/>).</typeparam>
public sealed class TMDbCacheEntry<T>
{
    /// <summary>Gets or sets the UTC timestamp of the refresh that produced this cache file.</summary>
    public DateTime RefreshedAtUtc { get; set; }

    /// <summary>Gets or sets the cached items.</summary>
    public IReadOnlyList<T> Items { get; set; } = [];
}
