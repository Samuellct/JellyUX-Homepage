using System.Collections.Concurrent;
using Jellyfin.Plugin.JuxHomepage.Configuration;

namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Thread-safe per-user session cache for the home screen widget layout.
/// Caches the list of <see cref="WidgetDescriptor"/> that passed MinItems filtering so that
/// <see cref="WidgetService.GetWidgetsForUser"/> avoids re-querying every widget on each page load.
/// Entries expire after a configurable TTL (see <see cref="CacheConfig.SessionTtlMinutes"/>).
/// A background timer garbage-collects entries that have not been accessed in over an hour.
/// </summary>
public sealed class SessionCache : IDisposable
{
    private const int CleanupIntervalMinutes = 5;
    private const int GarbageCollectAfterMinutes = 60;

    private readonly ConcurrentDictionary<Guid, SessionData> _cache = new();
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionCache"/> class.
    /// </summary>
    public SessionCache()
    {
        _cleanupTimer = new Timer(
            _ => Cleanup(),
            null,
            TimeSpan.FromMinutes(CleanupIntervalMinutes),
            TimeSpan.FromMinutes(CleanupIntervalMinutes));
    }

    /// <summary>
    /// Attempts to retrieve a cached descriptor list for the given user.
    /// Updates <see cref="SessionData.LastAccessed"/> on a cache hit to extend the entry's life.
    /// </summary>
    /// <param name="userId">The user whose layout to retrieve.</param>
    /// <param name="ttl">The maximum age of an entry before it is considered stale.</param>
    /// <param name="descriptors">The cached descriptors, or null on a miss.</param>
    /// <returns>True if a valid cached entry was found; otherwise false.</returns>
    public bool TryGet(Guid userId, TimeSpan ttl, out IReadOnlyList<WidgetDescriptor>? descriptors)
    {
        if (_cache.TryGetValue(userId, out var data) &&
            DateTime.UtcNow - data.LastAccessed < ttl)
        {
            data.LastAccessed = DateTime.UtcNow;
            descriptors = data.Descriptors;
            return true;
        }

        descriptors = null;
        return false;
    }

    /// <summary>
    /// Stores or replaces the descriptor list for the given user.
    /// </summary>
    /// <param name="userId">The user whose layout to cache.</param>
    /// <param name="descriptors">The ordered list of descriptors that passed MinItems filtering.</param>
    public void Set(Guid userId, IReadOnlyList<WidgetDescriptor> descriptors)
    {
        _cache[userId] = new SessionData
        {
            LastAccessed = DateTime.UtcNow,
            Descriptors = descriptors
        };
    }

    /// <summary>
    /// Removes the cached layout for the given user, forcing a fresh rebuild on next request.
    /// </summary>
    /// <param name="userId">The user whose cache entry to remove.</param>
    public void Invalidate(Guid userId) => _cache.TryRemove(userId, out _);

    /// <summary>
    /// Removes all cached layouts, forcing a fresh rebuild for every user on their next request.
    /// Called when the plugin configuration changes so that the new widget settings take effect
    /// immediately rather than waiting for the per-user TTL to expire.
    /// </summary>
    public void Clear() => _cache.Clear();

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer.Dispose();
            _disposed = true;
        }
    }

    private void Cleanup()
    {
        var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(GarbageCollectAfterMinutes);
        foreach (var key in _cache.Keys.ToList())
        {
            if (_cache.TryGetValue(key, out var data) && data.LastAccessed < cutoff)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }
}
