namespace Jellyfin.Plugin.JuxHomepage.Library.Models;

/// <summary>
/// The set of collections (BoxSets) a single library item belongs to, one entry per item that
/// belongs to at least one collection. Cached globally (not per-user) by
/// <see cref="CollectionsIndexCacheService"/> as a reverse index (item -&gt; collections), for the
/// "Included In" feature on an item's detail page (TODO_V3.md Phase 7.2).
/// </summary>
public sealed class CollectionMembership
{
    /// <summary>Gets or sets the library item's identifier.</summary>
    public Guid ItemId { get; set; }

    /// <summary>Gets or sets every collection this item belongs to.</summary>
    public IReadOnlyList<CollectionRef> Collections { get; set; } = [];
}

/// <summary>A lightweight reference to a collection (BoxSet), for display without a further lookup.</summary>
public sealed class CollectionRef
{
    /// <summary>Gets or sets the collection's item identifier.</summary>
    public Guid CollectionId { get; set; }

    /// <summary>Gets or sets the collection's display name.</summary>
    public string CollectionName { get; set; } = string.Empty;
}
