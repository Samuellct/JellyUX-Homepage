using Jellyfin.Plugin.JuxHomepage.Rewards.Models;

namespace Jellyfin.Plugin.JuxHomepage.Rewards;

/// <summary>
/// Reads and refreshes the on-disk Rewards cache, one file per admin-configured widget instance --
/// mirrors <see cref="TMDb.ITMDbCacheService"/>'s Discover Movies per-instance pattern.
/// </summary>
public interface IRewardsCacheService
{
    /// <summary>Returns the currently cached award winners for the given widget instance.</summary>
    /// <param name="instanceId">The widget instance id (a GUID, from <c>ExtraParams["value"]</c>).</param>
    /// <returns>The cached items, or an empty list if the instance id is invalid or no refresh has completed yet.</returns>
    IReadOnlyList<RewardsWinner> GetRewards(string instanceId);

    /// <summary>Refreshes a single Rewards widget instance's cache from Wikidata.</summary>
    /// <param name="instanceId">The widget instance id (a GUID, from <c>ExtraParams["value"]</c>).</param>
    /// <param name="filter">The instance's configured ceremony/category/year filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshInstanceAsync(string instanceId, RewardsFilter filter, CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes every configured Rewards widget instance. Shared with the admin "Refresh now" action
    /// (<see cref="Controllers.JuxHomepageController"/>) and the weekly scheduled task
    /// (<see cref="Tasks.RewardsWeeklyRefreshTask"/>), so the instance-enumeration logic isn't duplicated.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshAllInstancesAsync(CancellationToken cancellationToken);
}
