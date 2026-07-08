using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;
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
    /// The current configuration schema version. See the "Configuration Schema Versioning" section
    /// in CLAUDE.md for the migration policy.
    /// </summary>
    private const int CurrentSchemaVersion = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Application paths.</param>
    /// <param name="xmlSerializer">XML serializer.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;

        // Configuration is lazy-loaded by BasePlugin<T>; accessing it here forces the load (or
        // default-instance creation) immediately, so migration and validation apply on load, not
        // only when an admin saves from the dashboard (which previously left a hand-edited XML
        // file's invalid values in effect until the next save).
        var priorSchemaVersion = Configuration.SchemaVersion;
        MigrateConfiguration(Configuration);
        ValidateConfiguration(Configuration);

        // Migration restructures data (not just clamps values) -- persist immediately rather than
        // waiting for the next admin save, so a hand-edited or pre-Phase-8 V1 config file on disk
        // gets corrected right away instead of silently staying stale until someone opens the
        // config page and saves.
        if (Configuration.SchemaVersion != priorSchemaVersion)
        {
            SaveConfiguration();
        }
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
            ValidateConfiguration(config);
        }

        // Call base last so ConfigurationChanged fires with the clamped values.
        base.UpdateConfiguration(configuration);
    }

    /// <summary>
    /// Applies version-gated migrations to a loaded or newly-created configuration, keyed off
    /// <see cref="PluginConfiguration.SchemaVersion"/>, then bumps it to
    /// <see cref="CurrentSchemaVersion"/> once applied. See the "Configuration Schema Versioning"
    /// section in CLAUDE.md.
    /// </summary>
    /// <param name="config">The configuration to migrate in place.</param>
    internal static void MigrateConfiguration(PluginConfiguration config)
    {
        if (config.SchemaVersion < 2)
        {
            config.Widgets = ExplodePersonalizedFanOut(config.Widgets);
            config.SchemaVersion = 2;
        }
    }

    /// <summary>
    /// Applies the same version-gated migrations as <see cref="MigrateConfiguration"/>, but to a
    /// per-user configuration's <see cref="UserConfiguration.WidgetOverrides"/> -- per the
    /// "Configuration Schema Versioning" policy in CLAUDE.md, any migration of widget configuration
    /// rows must apply to both models identically.
    /// </summary>
    /// <param name="config">The user configuration to migrate in place.</param>
    internal static void MigrateUserConfiguration(UserConfiguration config)
    {
        if (config.SchemaVersion < 2)
        {
            config.WidgetOverrides = ExplodePersonalizedFanOut(config.WidgetOverrides);
            config.SchemaVersion = 2;
        }
    }

    /// <summary>
    /// V1-to-V2 migration step (TODO_V2.md Phase 8.4): explodes any Personalized widget row that
    /// still has <c>MaxInstances &gt; 1</c> (the old fan-out setting) into that many independent
    /// rows, one per instance, rather than silently losing the setting now that Personalized widgets
    /// are single-instance ("1 row = 1 section", see <see cref="PersonalizedWidgetBase"/>).
    /// The first copy keeps the original row's <see cref="WidgetConfig.CustomDisplayName"/> and
    /// <see cref="WidgetConfig.ExtraParams"/>; subsequent copies start blank. Rows for other widget
    /// categories, or Personalized rows already at <c>MaxInstances &lt;= 1</c>, pass through unchanged.
    /// </summary>
    /// <param name="widgets">The widget configuration rows to migrate.</param>
    /// <returns>The migrated array of rows.</returns>
    private static WidgetConfig[] ExplodePersonalizedFanOut(WidgetConfig[] widgets)
    {
        var result = new List<WidgetConfig>(widgets.Length);

        foreach (var widget in widgets)
        {
            if (!PersonalizedWidgetTypes.All.Contains(widget.WidgetType) || widget.MaxInstances <= 1)
            {
                result.Add(widget);
                continue;
            }

            for (var i = 0; i < widget.MaxInstances; i++)
            {
                result.Add(new WidgetConfig
                {
                    WidgetType = widget.WidgetType,
                    CustomDisplayName = i == 0 ? widget.CustomDisplayName : null,
                    Enabled = widget.Enabled,
                    AllowUserOverride = widget.AllowUserOverride,
                    Order = widget.Order + (i * 10),
                    MinItems = widget.MinItems,
                    MaxItems = widget.MaxItems,
                    ViewMode = widget.ViewMode,
                    MinInstances = 1,
                    MaxInstances = 1,
                    ExtraParams = i == 0 ? widget.ExtraParams : []
                });
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Clamps all configuration values to valid ranges in place. Called both when the configuration
    /// is loaded (so a hand-edited XML file cannot bypass validation until the next admin save) and
    /// when an admin saves from the dashboard.
    /// </summary>
    /// <param name="config">The configuration to validate in place.</param>
    internal static void ValidateConfiguration(PluginConfiguration config)
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
            config.TMDbLists.TopRatedMoviesPages = Math.Clamp(config.TMDbLists.TopRatedMoviesPages, 1, 5);
            config.TMDbLists.TopRatedShowsPages = Math.Clamp(config.TMDbLists.TopRatedShowsPages, 1, 5);
            config.TMDbLists.NowPlayingMoviesPages = Math.Clamp(config.TMDbLists.NowPlayingMoviesPages, 1, 5);
            config.TMDbLists.NowPlayingRegion = string.IsNullOrWhiteSpace(config.TMDbLists.NowPlayingRegion)
                ? null
                : config.TMDbLists.NowPlayingRegion.Trim().ToUpperInvariant();
            config.TMDbLists.TopRatedVoteCountMin = Math.Max(0, config.TMDbLists.TopRatedVoteCountMin);
        }
    }
}
