using System.Reflection;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Inject;
using Jellyfin.Plugin.JuxHomepage.Localization;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using Jellyfin.Plugin.JuxHomepage.Widgets.Admin;
using Jellyfin.Plugin.JuxHomepage.Widgets.Connected;
using Jellyfin.Plugin.JuxHomepage.Widgets.Native;
using Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;
using Jellyfin.Plugin.JuxHomepage.TMDb;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        serviceCollection.AddHostedService<ConfigurationChangeListener>();
        serviceCollection.AddSingleton<IUserConfigurationStore, UserConfigurationStore>();
        serviceCollection.AddSingleton<ILocalizationService, LocalizationService>();
        serviceCollection.AddHttpClient("TMDb", client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        serviceCollection.AddSingleton<ITMDbApiClient>(serviceProvider => new TMDbApiClient(
            serviceProvider.GetRequiredService<IHttpClientFactory>(),
            () => Plugin.Instance?.Configuration,
            serviceProvider.GetRequiredService<ILogger<TMDbApiClient>>()));
        serviceCollection.AddSingleton<ITMDbCacheService>(serviceProvider => new TMDbCacheService(
            serviceProvider.GetRequiredService<IApplicationPaths>(),
            serviceProvider.GetRequiredService<ITMDbApiClient>(),
            serviceProvider.GetRequiredService<ILibraryManager>(),
            () => Plugin.Instance?.Configuration,
            serviceProvider.GetRequiredService<ILogger<TMDbCacheService>>()));
        serviceCollection.AddSingleton<ScoringService>(serviceProvider => new ScoringService(
            serviceProvider.GetRequiredService<IUserManager>(),
            serviceProvider.GetRequiredService<ILibraryManager>(),
            () => Plugin.Instance?.Configuration));
        serviceCollection.AddSingleton<WidgetService>(serviceProvider => new WidgetService(
            serviceProvider.GetRequiredService<IWidgetRegistry>(),
            serviceProvider.GetRequiredService<SessionCache>(),
            serviceProvider.GetRequiredService<IUserConfigurationStore>(),
            () => Plugin.Instance?.Configuration,
            serviceProvider.GetRequiredService<ILogger<WidgetService>>()));

        serviceCollection.AddSingleton<IWidgetRegistry>(serviceProvider =>
        {
            var registry = new WidgetRegistry();
            var logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<WidgetRegistry>();

            // Register native widgets in display order before loading external DLL widgets.
            RegisterNativeWidget<ContinueWatchingWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<NextUpWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<RecentlyAddedMoviesWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<RecentlyAddedShowsWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<MyMediaWidget>(registry, serviceProvider, logger);

            // Register admin widgets. These have no default WidgetConfig rows; the admin
            // adds instances explicitly via the configuration page.
            RegisterNativeWidget<GenreWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<ActorWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<DirectorWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<StudioWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<CollectionWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<TagWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<YearWidget>(registry, serviceProvider, logger);

            // Register personalized widgets. Like admin widgets, these have no default
            // WidgetConfig rows; the admin adds instances explicitly via the configuration page.
            // Unlike admin widgets, the section's value is computed per user from ScoringService
            // rather than chosen by the admin.
            RegisterNativeWidget<FavoriteGenreWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<FavoriteActorWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<FavoriteDirectorWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<BecauseYouWatchedWidget>(registry, serviceProvider, logger);

            // Register connected widgets. Like admin/personalized widgets, these have no default
            // WidgetConfig rows; the admin adds instances explicitly via the configuration page.
            // Each displays TMDb data cross-referenced against the local library by ITMDbCacheService.
            RegisterNativeWidget<TrendingMoviesWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<TrendingShowsWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<AiringTodayShowsWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<TopRatedMoviesWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<TopRatedShowsWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<NowPlayingMoviesWidget>(registry, serviceProvider, logger);
            RegisterNativeWidget<DiscoverMoviesWidget>(registry, serviceProvider, logger);

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

    private static void RegisterNativeWidget<TWidget>(
        WidgetRegistry registry,
        IServiceProvider serviceProvider,
        ILogger logger)
        where TWidget : IWidget
    {
        try
        {
            var widget = (IWidget)ActivatorUtilities.CreateInstance(serviceProvider, typeof(TWidget));
            registry.Register(widget);
            logger.LogInformation("Registered native widget '{WidgetType}'.", widget.WidgetType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register native widget {Type}.", typeof(TWidget).Name);
        }
    }
}
