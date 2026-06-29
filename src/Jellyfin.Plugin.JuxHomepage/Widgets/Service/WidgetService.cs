using Jellyfin.Plugin.JuxHomepage.Configuration;
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
    private readonly UserConfigurationStore _userConfigStore;
    private readonly ILogger<WidgetService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetService"/> class.
    /// </summary>
    /// <param name="registry">The widget registry.</param>
    /// <param name="sessionCache">The session layout cache.</param>
    /// <param name="userConfigStore">The per-user configuration store.</param>
    /// <param name="logger">Logger.</param>
    public WidgetService(
        IWidgetRegistry registry,
        SessionCache sessionCache,
        UserConfigurationStore userConfigStore,
        ILogger<WidgetService> logger)
    {
        _registry = registry;
        _sessionCache = sessionCache;
        _userConfigStore = userConfigStore;
        _logger = logger;
    }

    /// <summary>
    /// Returns the ordered list of widget descriptors for the user's home screen.
    /// Widgets that return fewer items than their MinItems threshold are excluded.
    /// Results are cached per-user; the cache is invalidated when the TTL expires.
    /// </summary>
    /// <param name="userId">The requesting user's identifier.</param>
    /// <param name="page">Zero-based page index (each page contains up to <c>20</c> descriptors).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A page of <see cref="WidgetDescriptor"/> objects describing the home screen layout.</returns>
    public async Task<IReadOnlyList<WidgetDescriptor>> GetWidgetsForUser(
        Guid userId,
        int page,
        CancellationToken cancellationToken)
    {
        var ttlMinutes = Plugin.Instance?.Configuration?.Cache?.SessionTtlMinutes ?? 15;
        var ttl = TimeSpan.FromMinutes(ttlMinutes);

        if (_sessionCache.TryGet(userId, ttl, out var cached) && cached is not null)
        {
            return Paginate(cached, page);
        }

        var descriptors = await BuildDescriptors(userId, cancellationToken).ConfigureAwait(false);
        _sessionCache.Set(userId, descriptors);

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

        var payload = new WidgetPayload
        {
            UserId = userId,
            AdditionalData = additionalData,
            StartIndex = startIndex,
            Limit = limit
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
        CancellationToken cancellationToken)
    {
        var globalConfig = Plugin.Instance?.Configuration?.Widgets ?? [];
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
            .Select(config => ResolveAndFetch(userId, config, cancellationToken))
            .ToList();

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // Filter out instances that do not meet the MinItems threshold, then collect descriptors
        return results
            .Where(r => r is not null)
            .Select(r => r!)
            .ToList()
            .AsReadOnly();
    }

    private async Task<WidgetDescriptor?> ResolveAndFetch(
        Guid userId,
        WidgetConfig config,
        CancellationToken cancellationToken)
    {
        var widget = _registry.GetByType(config.WidgetType);
        if (widget is null)
        {
            _logger.LogDebug(
                "Widget type '{WidgetType}' is configured but not registered — skipping.",
                config.WidgetType);
            return null;
        }

        var instanceConfig = BuildInstanceConfig(config, widget);

        // Fetch a small probe to determine the total record count for MinItems filtering.
        // Widgets must set TotalRecordCount to the full count regardless of the Limit.
        var payload = new WidgetPayload
        {
            UserId = userId,
            AdditionalData = instanceConfig.AdditionalData,
            StartIndex = 0,
            Limit = 1
        };

        WidgetResult result;
        try
        {
            // CreateInstances yields the configured instances; use the first for the probe.
            var instances = widget.CreateInstances(userId, instanceConfig, config.MaxInstances).ToList();
            if (instances.Count == 0)
            {
                return null;
            }

            var instance = instances[0];
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

        return widget.GetDescriptor();
    }

    private static WidgetInstanceConfig BuildInstanceConfig(WidgetConfig config, IWidget widget) =>
        new()
        {
            DisplayName = config.CustomDisplayName ?? widget.DefaultDisplayName,
            MinItems = config.MinItems,
            MaxItems = config.MaxItems,
            ViewMode = config.ViewMode,
            Order = config.Order,
            ExtraParams = config.ExtraParams
        };

    private static IReadOnlyList<WidgetDescriptor> Paginate(IReadOnlyList<WidgetDescriptor> all, int page)
    {
        var skip = Math.Max(0, page) * PageSize;
        return all.Skip(skip).Take(PageSize).ToList().AsReadOnly();
    }
}
