namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// A failure encountered while loading an external widget pack DLL, surfaced to the admin
/// configuration page by <see cref="IWidgetRegistry.LoadErrors"/> instead of being visible only in
/// the server logs.
/// </summary>
/// <param name="FileName">The offending DLL file name (no path).</param>
/// <param name="Message">A human-readable, actionable reason for the failure.</param>
public sealed record WidgetPackLoadError(string FileName, string Message);
