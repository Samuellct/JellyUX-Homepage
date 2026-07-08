using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Localization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Resolves the effective widget layout configuration for a user, independently of runtime
/// orchestration (cache, item fetching). Extracted from <see cref="WidgetService"/> (TODO_V2.md
/// Phase 7.1) so configuration resolution -- merging global config with per-user overrides, sorting,
/// fan-out rank -- has a single well-scoped owner, separate from <see cref="WidgetService"/>'s public
/// API and session cache.
/// </summary>
public sealed class WidgetLayoutResolver
{
    private readonly IWidgetRegistry _registry;
    private readonly IUserConfigurationStore _userConfigStore;
    private readonly ILocalizationService _localizationService;
    private readonly Func<PluginConfiguration?> _getConfiguration;
    private readonly ILogger<WidgetLayoutResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetLayoutResolver"/> class.
    /// </summary>
    /// <param name="registry">The widget registry.</param>
    /// <param name="userConfigStore">The per-user configuration store.</param>
    /// <param name="localizationService">Widget display-name translation service.</param>
    /// <param name="getConfiguration">
    /// Factory that returns the current plugin configuration.
    /// Defaults to <c>Plugin.Instance?.Configuration</c> in production.
    /// </param>
    /// <param name="logger">Logger.</param>
    public WidgetLayoutResolver(
        IWidgetRegistry registry,
        IUserConfigurationStore userConfigStore,
        ILocalizationService localizationService,
        Func<PluginConfiguration?> getConfiguration,
        ILogger<WidgetLayoutResolver> logger)
    {
        _registry = registry;
        _userConfigStore = userConfigStore;
        _localizationService = localizationService;
        _getConfiguration = getConfiguration;
        _logger = logger;
    }

    /// <summary>
    /// Builds the ordered, MinItems-filtered list of widget descriptors for a user's home screen,
    /// merging global plugin configuration with the user's own overrides.
    /// </summary>
    /// <param name="userId">The requesting user's identifier.</param>
    /// <param name="lang">The requested language code (e.g. "fr"), or null for the default (English).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ordered list of descriptors, not yet paginated.</returns>
    public async Task<IReadOnlyList<WidgetDescriptor>> BuildDescriptors(
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
    /// Resolves the config row for an on-demand section fetch. When multiple rows share the same
    /// <paramref name="widgetType"/> (e.g. several Discover instances with different filters), the
    /// row whose <c>ExtraParams["value"]</c> matches <paramref name="additionalData"/> is preferred
    /// so each instance's own parameters are forwarded rather than always the first configured row.
    /// Falls back to the first match by type, preserving prior behavior for single-instance widgets.
    /// </summary>
    /// <param name="widgetType">The widget type identifier.</param>
    /// <param name="additionalData">Optional instance-specific data (e.g. a Discover instance id).</param>
    /// <returns>The matching config row, or null if none is configured for this widget type.</returns>
    public WidgetConfig? ResolveConfigRow(string widgetType, string? additionalData)
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
                "Widget type '{WidgetType}' is configured but not registered - skipping (user {UserId}).",
                config.WidgetType,
                userId);
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
                "Widget '{WidgetType}' threw an exception during layout build for user {UserId} - skipping.",
                config.WidgetType,
                userId);
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
                "Widget '{WidgetType}' threw an exception during layout build for user {UserId} - skipping.",
                config.WidgetType,
                userId);
            return null;
        }

        if (result.TotalRecordCount < config.MinItems)
        {
            _logger.LogDebug(
                "Widget '{WidgetType}' has {Count} items (MinItems={Min}) for user {UserId} - excluded from layout.",
                config.WidgetType,
                result.TotalRecordCount,
                config.MinItems,
                userId);
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

    private static WidgetInstanceConfig BuildInstanceConfig(
        WidgetConfig config,
        IWidget widget,
        string? lang,
        ILocalizationService localizationService)
    {
        var extra = config.GetExtraParamsDictionary();
        extra.TryGetValue("value", out var additionalData);
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
}
