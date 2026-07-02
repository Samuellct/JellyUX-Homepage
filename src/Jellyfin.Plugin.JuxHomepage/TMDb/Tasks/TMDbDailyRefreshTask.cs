using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.TMDb.Tasks;

/// <summary>
/// Scheduled task that refreshes all four TMDb caches once a day.
/// Auto-discovered by Jellyfin -- not registered in DI (its dependencies are DI-registered, but the
/// task itself is instantiated by Jellyfin's scheduled task subsystem, exactly like
/// <see cref="Inject.StartupService"/>).
/// </summary>
public sealed class TMDbDailyRefreshTask : IScheduledTask
{
    private readonly ITMDbCacheService _cacheService;
    private readonly ILogger<TMDbDailyRefreshTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TMDbDailyRefreshTask"/> class.
    /// </summary>
    /// <param name="cacheService">The TMDb cache service to refresh.</param>
    /// <param name="logger">Logger.</param>
    public TMDbDailyRefreshTask(ITMDbCacheService cacheService, ILogger<TMDbDailyRefreshTask> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string Name => "JellyUX - TMDb Refresh";

    /// <inheritdoc/>
    public string Key => "Jellyfin.Plugin.JuxHomepage.TMDbRefresh";

    /// <inheritdoc/>
    public string Description => "Refreshes cached TMDb trending, airing, and upcoming data.";

    /// <inheritdoc/>
    public string Category => "JellyUX Homepage";

    /// <inheritdoc/>
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        // RefreshAllAsync is independently fault-tolerant per cache type (TMDbCacheService logs and
        // swallows its own failures), and TMDbApiClient already skips silently when no API key is
        // configured, so no special-casing is needed here beyond delegating to it. Shared with the
        // admin "Refresh now" action (JuxHomepageController) so the 4-call sequence isn't duplicated.
        await _cacheService.RefreshAllAsync(progress, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("TMDb daily refresh completed.");
    }

    /// <inheritdoc/>
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.DailyTrigger,
            TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
        };
    }
}
