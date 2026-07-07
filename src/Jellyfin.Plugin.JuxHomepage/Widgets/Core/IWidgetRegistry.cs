namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Registry that holds all available widget implementations discovered at startup.
/// </summary>
public interface IWidgetRegistry
{
    /// <summary>
    /// Registers a widget implementation.
    /// </summary>
    /// <param name="widget">The widget to register.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a widget with the same <see cref="IWidget.WidgetType"/> is already registered.
    /// </exception>
    void Register(IWidget widget);

    /// <summary>
    /// Returns all registered widgets.
    /// </summary>
    /// <returns>A read-only collection of all registered widgets.</returns>
    IReadOnlyCollection<IWidget> GetAll();

    /// <summary>
    /// Looks up a widget by its type identifier.
    /// </summary>
    /// <param name="widgetType">The widget type identifier (case-sensitive).</param>
    /// <returns>The matching widget, or null if not found.</returns>
    IWidget? GetByType(string widgetType);

    /// <summary>
    /// Gets the external widget-pack load failures recorded by the most recent
    /// <see cref="SetLoadErrors"/> call, empty if every pack (if any) loaded cleanly. Recomputed at
    /// every startup; never persisted to the plugin configuration.
    /// </summary>
    IReadOnlyList<WidgetPackLoadError> LoadErrors { get; }

    /// <summary>
    /// Records the external widget-pack load failures encountered during startup discovery, for
    /// display in the admin configuration page.
    /// </summary>
    /// <param name="errors">The load failures to record.</param>
    void SetLoadErrors(IReadOnlyList<WidgetPackLoadError> errors);
}
