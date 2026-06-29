namespace Jellyfin.Plugin.JuxHomepage.Configuration;

/// <summary>
/// API keys for external data sources used by connected widgets.
/// </summary>
public sealed class ApiKeysConfig
{
    /// <summary>
    /// Gets or sets the TMDb (The Movie Database) API key.
    /// Required for widgets that fetch TMDb metadata or trending content.
    /// </summary>
    public string? TMDb { get; set; }
}
