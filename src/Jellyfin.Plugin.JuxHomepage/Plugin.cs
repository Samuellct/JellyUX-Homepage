using Jellyfin.Plugin.JuxHomepage.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.JuxHomepage;

/// <summary>
/// JellyUX Homepage plugin entry point.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasPluginConfiguration, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Application paths.</param>
    /// <param name="xmlSerializer">XML serializer.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the singleton plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc/>
    public override string Name => "JellyUX Homepage";

    /// <inheritdoc/>
    public override Guid Id => Guid.Parse("3adf1f1f-1541-4e47-b9e3-34d2d2968af6");

    /// <inheritdoc/>
    public override string Description =>
        "Replaces and enhances the Jellyfin home screen with configurable widgets.";

    /// <inheritdoc/>
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = $"{GetType().Namespace}.Web.config.html"
        };
    }

    /// <inheritdoc/>
    public override void UpdateConfiguration(MediaBrowser.Model.Plugins.BasePluginConfiguration configuration)
    {
        if (configuration is PluginConfiguration config)
        {
            // Clamp cache values to valid ranges.
            if (config.Cache is not null)
            {
                config.Cache.SessionTtlMinutes = Math.Max(1, config.Cache.SessionTtlMinutes);
                config.Cache.TMDbRefreshIntervalHours = Math.Max(1, config.Cache.TMDbRefreshIntervalHours);
            }

            // Clamp per-widget values and ensure MinItems does not exceed MaxItems.
            foreach (var widget in config.Widgets)
            {
                widget.MinItems = Math.Max(0, widget.MinItems);
                widget.MaxItems = Math.Max(1, widget.MaxItems);
                if (widget.MinItems > widget.MaxItems)
                {
                    widget.MinItems = widget.MaxItems;
                }

                widget.MinInstances = Math.Max(1, widget.MinInstances);
                widget.MaxInstances = Math.Max(1, widget.MaxInstances);
                if (widget.MinInstances > widget.MaxInstances)
                {
                    widget.MinInstances = widget.MaxInstances;
                }
            }

            // Trim API keys; empty string becomes null.
            if (config.ApiKeys is not null)
            {
                config.ApiKeys.TMDb = string.IsNullOrWhiteSpace(config.ApiKeys.TMDb)
                    ? null
                    : config.ApiKeys.TMDb.Trim();
            }

            // Clamp TMDb list page counts to a sane range (1 page = up to 20 items; more pages
            // means more cross-referencing API calls per refresh).
            if (config.TMDbLists is not null)
            {
                config.TMDbLists.TrendingMoviesPages = Math.Clamp(config.TMDbLists.TrendingMoviesPages, 1, 5);
                config.TMDbLists.TrendingShowsPages = Math.Clamp(config.TMDbLists.TrendingShowsPages, 1, 5);
                config.TMDbLists.AiringTodayPages = Math.Clamp(config.TMDbLists.AiringTodayPages, 1, 5);
                config.TMDbLists.UpcomingMoviesPages = Math.Clamp(config.TMDbLists.UpcomingMoviesPages, 1, 5);
            }
        }

        // Call base last so ConfigurationChanged fires with the clamped values.
        base.UpdateConfiguration(configuration);
    }
}
