namespace Jellyfin.Plugin.JuxHomepage.TMDb.Models;

/// <summary>
/// Common shape shared by <see cref="TMDbMovie"/> and <see cref="TMDbShow"/>, allowing
/// <see cref="LibraryCrossReferencer"/> to cross-reference either type against the local library with
/// a single generic implementation. Named independently of TMDb so a future second external data
/// provider (TVDb, Trakt, OMDb) could implement it too, reusing the same cross-referencing logic.
/// </summary>
public interface IExternalCacheItem
{
    /// <summary>Gets the external provider's identifier for this item.</summary>
    int Id { get; }

    /// <summary>Gets or sets the matching Jellyfin library item identifier, if found.</summary>
    Guid? LibraryItemId { get; set; }
}
