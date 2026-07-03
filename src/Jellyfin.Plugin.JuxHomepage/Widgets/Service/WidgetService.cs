using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Localization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Orchestrates widget execution for the JellyUX home screen.
/// <para>
/// <see cref="GetWidgetsForUser"/> resolves the widget layout for a user (merging global config
/// with per-user overrides), runs all widgets in parallel, applies MinItems filtering, and caches
/// the resulting descriptor list in <see cref="SessionCache"/>.
/// </para>
/// <para>
/// <see cref="GetWidgetItems"/> fetches items for a specific widget on demand (called per section
/// by the front end).
/// </para>
/// </summary>
public sealed class WidgetService
{
    private const int PageSize = 20;

    private readonly IWidgetRegistry _registry;
    private readonly SessionCache _sessionCache;
    private readonly IUserConfigurationStore _userConfigStore;
    private readonly ILocalizationService _localizationService;
    private readonly Func<PluginConfiguration?> _getConfiguration;
    private readonly ILogger<WidgetService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetService"/> class.
    /// </summary>
    /// <param name="registry">The widget registry.</param>
    /// <param name="sessionCache">The session layout cache.</param>
    /// <param name="userConfigStore">The per-user configuration store.</param>
    /// <param name="localizationService">Widget display-name translation service.</param>
    /// <param name="getConfiguration">
    /// Factory that returns the current plugin configuration.
    /// Defaults to <c>Plugin.Instance?.Configuration</c> in production.
    /// </param>
    /// <param name="logger">Logger.</param>
    public WidgetService(
        IWidgetRegistry registry,
        SessionCache sessionCache,
        IUserConfigurationStore userConfigStore,
        ILocalizationService localizationService,
        Func<PluginConfiguration?> getConfiguration,
        ILogger<WidgetService> logger)
    {
        _registry = registry;
        _sessionCache = sessionCache;
        _userConfigStore = userConfigStore;
        _localizationService = localizationService;
        _getConfiguration = getConfiguration;
        _logger = logger;
    }

    /// <summary>
    /// Returns the ordered list of widget descriptors for the user's home screen.
    /// Widgets that return fewer items than their MinItems threshold are excluded.
    /// Results are cached per-user; the cache is invalidated when the TTL expires.
    /// </summary>
    /// <param name="userId">The requesting user's identifier.</param>
    /// <param name="page">Zero-based page index (each page contains up to <c>20</c> descriptors).</param>
    /// <param name="lang">
    /// The requested language code (e.g. "fr"), or null for the default (English). Determines the
    /// language of translated widget display names and is part of the session cache key, so each
    /// language gets its own cached layout.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A page of <see cref="WidgetDescriptor"/> objects describing the home screen layout.</returns>
    public async Task<IReadOnlyList<WidgetDescriptor>> GetWidgetsForUser(
        Guid userId,
        int page,
        string? lang,
        CancellationToken cancellationToken)
    {
        var ttlMinutes = _getConfiguration()?.Cache?.SessionTtlMinutes ?? 15;
        var ttl = TimeSpan.FromMinutes(ttlMinutes);

        if (_sessionCache.TryGet(userId, lang, ttl, out var cached) && cached is not null)
        {
            return Paginate(cached, page);
        }

        var descriptors = await BuildDescriptors(userId, lang, cancellationToken).ConfigureAwait(false);
        _sessionCache.Set(userId, lang, descriptors);

        return Paginate(descriptors, page);
    }

    /// <summary>
    /// Fetches items for a specific widget on demand.
    /// Called by <c>GET /JuxHomepage/Section/{widgetType}</c> when the front end loads a section.
    /// </summary>
    /// <param name="userId">The requesting user's identifier.</param>
    /// <param name="widgetType">The widget type identifier.</param>
    /// <param name="additionalData">Optional instance-specific data (e.g. library ID).</param>
    /// <param name="startIndex">Zero-based start index for pagination.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="WidgetResult"/>, or null if the widget type is not registered.</returns>
    public async Task<WidgetResult?> GetWidgetItems(
        Guid userId,
        string widgetType,
        string? additionalData,
        int startIndex,
        int limit,
        CancellationToken cancellationToken)
    {
        var widget = _registry.GetByType(widgetType);
        if (widget is null)
        {
            return null;
        }

        // Forward the configured ExtraParams (e.g. "excludeWatched" for personalized widgets, or a
        // Discover instance's filter parameters) so on-demand section fetches honor the same
        // settings as the layout probe.
        var configRow = ResolveConfigRow(widgetType, additionalData);
        IReadOnlyDictionary<string, string>? extra = configRow is not null && configRow.ExtraParams.Length > 0
            ? configRow.ExtraParams.ToDictionary(p => p.Key, p => p.Value)
            : null;

        var payload = new WidgetPayload
        {
            UserId = userId,
            AdditionalData = additionalData,
            StartIndex = startIndex,
            Limit = limit,
            ExtraParams = extra
        };

        try
        {
            return await widget.GetItemsAsync(payload, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Widget '{WidgetType}' threw an exception during GetWidgetItems.", widgetType);
            return WidgetResult.Empty;
        }
    }

    private async Task<IReadOnlyList<WidgetDescriptor>> BuildDescriptors(
        Guid userId,
        string? lang,
        CancellationToken cancellationToken)
    {
        var globalConfig = _getConfiguration()?.Widgets ?? [];
        var userConfig = _userConfigStore.GetUserConfiguration(userId);
        var overrides = userConfig?.WidgetOverrides ?? [];

        // Merge: apply user override when global config allows it
        var effectiveConfigs = globalConfig
            .Select(g =>
            {
                if (!g.AllowUserOverride)
                {
                    return g;
                }

                var userOverride = Array.Find(overrides, o => o.WidgetType == g.WidgetType);
                return userOverride ?? g;
            })
            .Where(c => c.Enabled)
            .OrderBy(c => c.Order)
            .ToList();

        // Resolve widget instances for each enabled config entry
        var tasks = effectiveConfigs
            .Select(config => ResolveAndFetch(userId, config, lang, cancellationToken))
            .ToList();

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // Flatten the per-config descriptor lists (a config row may fan out into several
        // instances, e.g. personalized widgets), keeping the overall layout order.
        return results
            .SelectMany(r => r)
            .OrderBy(d => d.Order)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Resolves a single <see cref="WidgetConfig"/> row into zero or more descriptors.
    /// Most widgets are single-instance and produce at most one descriptor. Widgets whose
    /// <see cref="IWidget.CreateInstances"/> fans out into several instances (e.g. personalized
    /// widgets producing one section per scored value) produce one descriptor per instance.
    /// </summary>
    private async Task<IReadOnlyList<WidgetDescriptor>> ResolveAndFetch(
        Guid userId,
        WidgetConfig config,
        string? lang,
        CancellationToken cancellationToken)
    {
        var widget = _registry.GetByType(config.WidgetType);
        if (widget is null)
        {
            _logger.LogDebug(
                "Widget type '{WidgetType}' is configured but not registered — skipping.",
                config.WidgetType);
            return [];
        }

        var instanceConfig = BuildInstanceConfig(config, widget, lang, _localizationService);

        List<IWidget> instances;
        try
        {
            instances = widget.CreateInstances(userId, instanceConfig, config.MaxInstances).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Widget '{WidgetType}' threw an exception during layout build — skipping.",
                config.WidgetType);
            return [];
        }

        var descriptors = new List<WidgetDescriptor>();
        for (var i = 0; i < instances.Count; i++)
        {
            var descriptor = await FetchInstanceDescriptor(
                userId,
                config,
                instanceConfig,
                instances[i],
                i,
                cancellationToken).ConfigureAwait(false);

            if (descriptor is not null)
            {
                descriptors.Add(descriptor);
            }
        }

        return descriptors;
    }

    private async Task<WidgetDescriptor?> FetchInstanceDescriptor(
        Guid userId,
        WidgetConfig config,
        WidgetInstanceConfig instanceConfig,
        IWidget instance,
        int index,
        CancellationToken cancellationToken)
    {
        var instanceDescriptor = instance.GetDescriptor();

        // Instances produced by a fan-out (e.g. personalized widgets) self-identify via a
        // non-null AdditionalData on their own descriptor; single-instance widgets fall back
        // to the config row's resolved AdditionalData (ExtraParams["value"]).
        var selfIdentifies = instanceDescriptor.AdditionalData is not null;
        var additionalData = selfIdentifies ? instanceDescriptor.AdditionalData : instanceConfig.AdditionalData;
        var displayName = selfIdentifies ? instanceDescriptor.DisplayName : instanceConfig.DisplayName;

        // Fetch a small probe to determine the total record count for MinItems filtering.
        // Widgets must set TotalRecordCount to the full count regardless of the Limit.
        var payload = new WidgetPayload
        {
            UserId = userId,
            AdditionalData = additionalData,
            ExtraParams = instanceConfig.ExtraParams,
            StartIndex = 0,
            Limit = 1
        };

        WidgetResult result;
        try
        {
            result = await instance.GetItemsAsync(payload, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Widget '{WidgetType}' threw an exception during layout build — skipping.",
                config.WidgetType);
            return null;
        }

        if (result.TotalRecordCount < config.MinItems)
        {
            _logger.LogDebug(
                "Widget '{WidgetType}' has {Count} items (MinItems={Min}) — excluded from layout.",
                config.WidgetType,
                result.TotalRecordCount,
                config.MinItems);
            return null;
        }

        return new WidgetDescriptor
        {
            WidgetType = instanceDescriptor.WidgetType,
            DisplayName = displayName,
            Category = instanceDescriptor.Category,
            ViewMode = config.ViewMode,
            Route = instanceDescriptor.Route,
            AdditionalData = additionalData,
            Order = config.Order + index,
            MinItems = config.MinItems
        };
    }

    /// <summary>
    /// Resolves the config row for an on-demand section fetch. When multiple rows share the same
    /// <paramref name="widgetType"/> (e.g. several Discover instances with different filters), the
    /// row whose <c>ExtraParams["value"]</c> matches <paramref name="additionalData"/> is preferred
    /// so each instance's own parameters are forwarded rather than always the first configured row.
    /// Falls back to the first match by type, preserving prior behavior for single-instance widgets.
    /// </summary>
    private WidgetConfig? ResolveConfigRow(string widgetType, string? additionalData)
    {
        var candidates = _getConfiguration()?.Widgets?.Where(c => c.WidgetType == widgetType).ToList();
        if (candidates is null || candidates.Count == 0)
        {
            return null;
        }

        if (additionalData is not null)
        {
            var match = candidates.FirstOrDefault(c => c.ExtraParams.Any(p =>
                p.Key == "value" && p.Value == additionalData));
            if (match is not null)
            {
                return match;
            }
        }

        return candidates[0];
    }

    private static WidgetInstanceConfig BuildInstanceConfig(
        WidgetConfig config,
        IWidget widget,
        string? lang,
        ILocalizationService localizationService)
    {
        IReadOnlyDictionary<string, string>? extra = config.ExtraParams.Length > 0
            ? config.ExtraParams.ToDictionary(p => p.Key, p => p.Value)
            : null;
        string? additionalData = extra is not null && extra.TryGetValue("value", out var v) ? v : null;
        return new WidgetInstanceConfig
        {
            DisplayName = config.CustomDisplayName ?? localizationService.Translate(widget.WidgetType, lang),
            MinItems = config.MinItems,
            MaxItems = config.MaxItems,
            ViewMode = config.ViewMode,
            Order = config.Order,
            ExtraParams = extra,
            AdditionalData = additionalData,
            Lang = lang
        };
    }

    private static IReadOnlyList<WidgetDescriptor> Paginate(IReadOnlyList<WidgetDescriptor> all, int page)
    {
        var skip = Math.Max(0, page) * PageSize;
        return all.Skip(skip).Take(PageSize).ToList().AsReadOnly();
    }
}
