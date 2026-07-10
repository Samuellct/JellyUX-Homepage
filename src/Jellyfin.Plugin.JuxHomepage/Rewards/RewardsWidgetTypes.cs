namespace Jellyfin.Plugin.JuxHomepage.Rewards;

/// <summary>
/// Widget type identifier for the Rewards widget, kept independent of the <c>Widgets</c> namespace
/// exactly like <see cref="TMDb.TMDbWidgetTypes"/>.
/// </summary>
public static class RewardsWidgetTypes
{
    /// <summary>The widget type identifier for a customizable Rewards instance.</summary>
    public const string Rewards = "jux.connected.rewards";
}
