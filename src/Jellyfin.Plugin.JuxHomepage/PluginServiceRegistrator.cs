using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Inject;
using Jellyfin.Plugin.JuxHomepage.IO;
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
        serviceCollection.AddSingleton<IFileSystem, FileSystem>();
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
            serviceProvider.GetRequiredService<ILocalizationService>(),
            () => Plugin.Instance?.Configuration,
            serviceProvider.GetRequiredService<ILogger<WidgetService>>()));

        serviceCollection.AddSingleton<IWidgetRegistry>(serviceProvider =>
        {
            var registry = new WidgetRegistry();
            var logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<WidgetRegistry>();

            // Register native widgets in display order before loading external DLL widgets.
            RegisterWidget<ContinueWatchingWidget>(registry, serviceProvider, logger);
            RegisterWidget<NextUpWidget>(registry, serviceProvider, logger);
            RegisterWidget<RecentlyAddedMoviesWidget>(registry, serviceProvider, logger);
            RegisterWidget<RecentlyAddedShowsWidget>(registry, serviceProvider, logger);
            RegisterWidget<MyMediaWidget>(registry, serviceProvider, logger);

            // Register admin widgets. These have no default WidgetConfig rows; the admin
            // adds instances explicitly via the configuration page.
            RegisterWidget<GenreWidget>(registry, serviceProvider, logger);
            RegisterWidget<ActorWidget>(registry, serviceProvider, logger);
            RegisterWidget<DirectorWidget>(registry, serviceProvider, logger);
            RegisterWidget<StudioWidget>(registry, serviceProvider, logger);
            RegisterWidget<CollectionWidget>(registry, serviceProvider, logger);
            RegisterWidget<TagWidget>(registry, serviceProvider, logger);
            RegisterWidget<YearWidget>(registry, serviceProvider, logger);

            // Register personalized widgets. Like admin widgets, these have no default
            // WidgetConfig rows; the admin adds instances explicitly via the configuration page.
            // Unlike admin widgets, the section's value is computed per user from ScoringService
            // rather than chosen by the admin.
            RegisterWidget<FavoriteGenreWidget>(registry, serviceProvider, logger);
            RegisterWidget<FavoriteActorWidget>(registry, serviceProvider, logger);
            RegisterWidget<FavoriteDirectorWidget>(registry, serviceProvider, logger);
            RegisterWidget<BecauseYouWatchedWidget>(registry, serviceProvider, logger);

            // Register connected widgets. Like admin/personalized widgets, these have no default
            // WidgetConfig rows; the admin adds instances explicitly via the configuration page.
            // Each displays TMDb data cross-referenced against the local library by ITMDbCacheService.
            RegisterWidget<TrendingMoviesWidget>(registry, serviceProvider, logger);
            RegisterWidget<TrendingShowsWidget>(registry, serviceProvider, logger);
            RegisterWidget<AiringTodayShowsWidget>(registry, serviceProvider, logger);
            RegisterWidget<TopRatedMoviesWidget>(registry, serviceProvider, logger);
            RegisterWidget<TopRatedShowsWidget>(registry, serviceProvider, logger);
            RegisterWidget<NowPlayingMoviesWidget>(registry, serviceProvider, logger);
            RegisterWidget<DiscoverMoviesWidget>(registry, serviceProvider, logger);

            var applicationPaths = serviceProvider.GetRequiredService<IApplicationPaths>();

            // Discover and register IWidget implementations from external DLLs placed in a dedicated
            // widget-pack directory (not recursive: this must not descend into unrelated
            // subdirectories such as cache/tmdb/, which lives under the same plugin configuration
            // root). This allows third-party widget packs without modifying the core plugin. See
            // WidgetPackLoader for the security posture of this mechanism.
            var packDir = Path.Combine(
                applicationPaths.PluginConfigurationsPath,
                "Jellyfin.Plugin.JuxHomepage",
                "widget-packs");
            var loadErrors = WidgetPackLoader.LoadInto(registry, serviceProvider, packDir, logger);
            registry.SetLoadErrors(loadErrors);

            return registry;
        });
    }

    private static void RegisterWidget<TWidget>(
        WidgetRegistry registry,
        IServiceProvider serviceProvider,
        ILogger logger)
        where TWidget : IWidget
    {
        try
        {
            var widget = (IWidget)ActivatorUtilities.CreateInstance(serviceProvider, typeof(TWidget));
            registry.Register(widget);
            logger.LogInformation("Registered widget '{WidgetType}'.", widget.WidgetType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register widget {Type}.", typeof(TWidget).Name);
        }
    }
}
