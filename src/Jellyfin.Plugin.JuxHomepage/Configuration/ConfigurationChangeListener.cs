using Jellyfin.Plugin.JuxHomepage.Widgets;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.JuxHomepage.Configuration;

/// <summary>
/// Hosted service that invalidates the session cache whenever the plugin configuration is saved.
/// Subscribes to the plugin's <c>ConfigurationChanged</c> event in <see cref="StartAsync"/> so
/// that any call to <see cref="Plugin.UpdateConfiguration"/> -- including saves from the Jellyfin
/// admin dashboard -- immediately clears the cached home screen layouts for all users.
/// </summary>
public sealed class ConfigurationChangeListener : IHostedService
{
    private readonly SessionCache _sessionCache;
    private EventHandler<BasePluginConfiguration>? _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationChangeListener"/> class.
    /// </summary>
    /// <param name="sessionCache">The session cache to clear on configuration change.</param>
    public ConfigurationChangeListener(SessionCache sessionCache)
    {
        _sessionCache = sessionCache;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _handler = (_, _) => _sessionCache.Clear();

        if (Plugin.Instance is not null)
        {
            Plugin.Instance.ConfigurationChanged += _handler;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (Plugin.Instance is not null && _handler is not null)
        {
            Plugin.Instance.ConfigurationChanged -= _handler;
        }

        return Task.CompletedTask;
    }
}
