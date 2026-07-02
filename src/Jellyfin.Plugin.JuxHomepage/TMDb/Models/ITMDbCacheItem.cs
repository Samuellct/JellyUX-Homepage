namespace Jellyfin.Plugin.JuxHomepage.TMDb.Models;

/// <summary>
/// Common shape shared by <see cref="TMDbMovie"/> and <see cref="TMDbShow"/>, allowing
/// <see cref="ITMDbCacheService"/> to cross-reference either type against the local library with a
/// single generic implementation.
/// </summary>
public interface ITMDbCacheItem
{
    /// <summary>Gets the TMDb identifier for this item.</summary>
    int Id { get; }

    /// <summary>Gets or sets the matching Jellyfin library item identifier, if found.</summary>
    Guid? LibraryItemId { get; set; }
}
