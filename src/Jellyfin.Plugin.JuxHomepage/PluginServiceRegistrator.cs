using System.Reflection;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Inject;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage;

/// <summary>
/// Registers plugin services into the Jellyfin DI container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc/>
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<FileTransformationDetector>();
        serviceCollection.AddSingleton<SessionCache>();
        serviceCollection.AddSingleton<UserConfigurationStore>();
        serviceCollection.AddSingleton<WidgetService>();

        serviceCollection.AddSingleton<IWidgetRegistry>(serviceProvider =>
        {
            var registry = new WidgetRegistry();
            var logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<WidgetRegistry>();

            var applicationPaths = serviceProvider.GetRequiredService<IApplicationPaths>();
            var pluginDir = Path.Combine(
                applicationPaths.PluginConfigurationsPath,
                "Jellyfin.Plugin.JuxHomepage");

            Directory.CreateDirectory(pluginDir);

            // Discover and register IWidget implementations from external DLLs placed in the
            // plugin configuration directory. This allows third-party widget packs without
            // modifying the core plugin.
            foreach (var dllPath in Directory.GetFiles(pluginDir, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    var assembly = Assembly.LoadFile(dllPath);
                    foreach (var type in assembly.GetTypes()
                        .Where(t => !t.IsAbstract && !t.IsInterface && t.IsAssignableTo(typeof(IWidget))))
                    {
                        try
                        {
                            var widget = (IWidget)ActivatorUtilities.CreateInstance(serviceProvider, type);
                            registry.Register(widget);
                            logger.LogInformation(
                                "Registered external widget '{WidgetType}' from {Dll}.",
                                widget.WidgetType,
                                Path.GetFileName(dllPath));
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(
                                ex,
                                "Failed to register widget type {Type} from {Dll}.",
                                type.FullName,
                                Path.GetFileName(dllPath));
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to load external widget DLL: {Dll}.", dllPath);
                }
            }

            return registry;
        });
    }
}
