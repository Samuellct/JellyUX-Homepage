namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Contract that all JellyUX Homepage widgets must implement.
/// Each widget type is registered once in <see cref="IWidgetRegistry"/>; instances are created
/// per user session via <see cref="CreateInstances"/>.
/// </summary>
public interface IWidget
{
    /// <summary>
    /// Gets the unique type identifier for this widget (e.g. "continue-watching").
    /// Must be stable across restarts as it is persisted in user configurations.
    /// </summary>
    string WidgetType { get; }

    /// <summary>Gets the default display name shown in the UI when no custom name is configured.</summary>
    string DefaultDisplayName { get; }

    /// <summary>Gets the category that classifies this widget's data source.</summary>
    WidgetCategory Category { get; }

    /// <summary>Gets the maximum number of instances this widget may have on a single home screen.</summary>
    int MaxInstances { get; }

    /// <summary>Gets the default minimum number of items required to display this widget.</summary>
    int DefaultMinItems { get; }

    /// <summary>
    /// Creates configured instances of this widget for a given user session.
    /// Single-instance widgets may return <c>this</c>; multi-instance widgets return distinct objects.
    /// </summary>
    /// <param name="userId">The requesting user's identifier.</param>
    /// <param name="config">The resolved per-instance configuration.</param>
    /// <param name="count">The number of instances to create.</param>
    /// <returns>An enumerable of widget instances (length must not exceed <paramref name="count"/>).</returns>
    IEnumerable<IWidget> CreateInstances(Guid userId, WidgetInstanceConfig config, int count);

    /// <summary>
    /// Asynchronously fetches items for this widget instance.
    /// </summary>
    /// <param name="payload">The request payload containing user context and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="WidgetResult"/> containing the item page and total record count.</returns>
    Task<WidgetResult> GetItemsAsync(WidgetPayload payload, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a read-only descriptor for this widget instance, used by the front end to render
    /// the home screen layout.
    /// </summary>
    /// <returns>A <see cref="WidgetDescriptor"/> describing this instance.</returns>
    WidgetDescriptor GetDescriptor();
}
