namespace Jellyfin.Plugin.JuxHomepage.Configuration;

/// <summary>
/// Cache tuning parameters for the widget engine.
/// </summary>
public sealed class CacheConfig
{
    /// <summary>
    /// Gets or sets the session cache TTL in minutes.
    /// After this period of inactivity the cached widget layout for a user is discarded.
    /// </summary>
    public int SessionTtlMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets how often TMDb data is refreshed, in hours.
    /// Increase to reduce external API usage; decrease for fresher trending content.
    /// </summary>
    public int TMDbRefreshIntervalHours { get; set; } = 24;
}
