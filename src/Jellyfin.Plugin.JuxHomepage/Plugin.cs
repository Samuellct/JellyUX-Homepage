using Jellyfin.Plugin.JuxHomepage.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.JuxHomepage;

/// <summary>
/// JellyUX Homepage plugin entry point.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasPluginConfiguration
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
}
