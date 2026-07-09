using Jellyfin.Plugin.JuxHomepage.IO;
using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.Widgets.Native;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.JuxHomepage.Inject;

/// <summary>
/// Hosted service that runs once at server startup to register web transformations and detect
/// required Jellyfin assets. Registered explicitly via <c>AddHostedService</c> in
/// <see cref="PluginServiceRegistrator"/> (not auto-discovered -- unlike an <c>IScheduledTask</c>,
/// an <see cref="IHostedService"/> does not show up in Dashboard &gt; Scheduled Tasks).
/// </summary>
public sealed class StartupService : IHostedService
{
    private static readonly TMDbCacheType[] TMDbCacheTypes =
    [
        TMDbCacheType.TrendingMovies,
        TMDbCacheType.TrendingShows,
        TMDbCacheType.AiringToday
    ];

    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<StartupService> _logger;
    private readonly FileTransformationDetector _detector;
    private readonly ITMDbCacheService _tmdbCacheService;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupService"/> class.
    /// </summary>
    /// <param name="applicationPaths">Application paths, including WebPath.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="detector">FileTransformation reflection bridge.</param>
    /// <param name="tmdbCacheService">TMDb cache service, used to refresh a missing/stale cache immediately.</param>
    /// <param name="fileSystem">File system abstraction, used to locate and read the loadSections chunk.</param>
    public StartupService(
        IApplicationPaths applicationPaths,
        ILogger<StartupService> logger,
        FileTransformationDetector detector,
        ITMDbCacheService tmdbCacheService,
        IFileSystem fileSystem)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
        _detector = detector;
        _tmdbCacheService = tmdbCacheService;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        SeedDefaultWidgetConfiguration();

        await RefreshStaleTMDbCacheAsync(cancellationToken).ConfigureAwait(false);

        if (!_detector.IsAvailable())
        {
            const string warning =
                "FileTransformation plugin is not installed. "
                + "JellyUX Homepage cannot inject resources into index.html. "
                + "Install jellyfin-plugin-file-transformation and restart Jellyfin.";

            _logger.LogError(warning);

            if (Plugin.Instance is not null)
            {
                Plugin.Instance.Configuration.StartupWarning = warning;
                Plugin.Instance.SaveConfiguration();
            }

            return;
        }

        if (Plugin.Instance?.Configuration.StartupWarning is not null)
        {
            Plugin.Instance.Configuration.StartupWarning = null;
            Plugin.Instance.SaveConfiguration();
        }

        RegisterIndexHtmlTransformation();
        RegisterLoadSectionsTransformations();
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Immediately refreshes any TMDb cache type that is missing or older than the configured
    /// refresh interval, so a fresh install (or one that restarted after the daily 3 AM window was
    /// missed) does not have to wait for the next scheduled run. Best-effort: a failure here must
    /// never prevent the FileTransformation registration that follows.
    /// </summary>
    private async Task RefreshStaleTMDbCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            var refreshed = 0;
            foreach (var type in TMDbCacheTypes)
            {
                if (!_tmdbCacheService.IsStale(type))
                {
                    continue;
                }

                await RefreshTMDbCacheType(type, cancellationToken).ConfigureAwait(false);
                refreshed++;
            }

            if (refreshed > 0)
            {
                _logger.LogInformation("Refreshed {Count} stale TMDb cache type(s) at startup.", refreshed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh TMDb cache at startup.");
        }
    }

    private Task RefreshTMDbCacheType(TMDbCacheType type, CancellationToken cancellationToken) => type switch
    {
        TMDbCacheType.TrendingMovies => _tmdbCacheService.RefreshTrendingMoviesAsync(cancellationToken),
        TMDbCacheType.TrendingShows => _tmdbCacheService.RefreshTrendingShowsAsync(cancellationToken),
        TMDbCacheType.AiringToday => _tmdbCacheService.RefreshAiringTodayAsync(cancellationToken),
        _ => Task.CompletedTask
    };

    private void SeedDefaultWidgetConfiguration()
    {
        if (Plugin.Instance is null)
        {
            return;
        }

        if (Plugin.Instance.Configuration.Widgets.Length > 0)
        {
            return;
        }

        Plugin.Instance.Configuration.Widgets = NativeWidgetDefaults.Build();
        Plugin.Instance.SaveConfiguration();
        _logger.LogInformation(
            "Seeded default configuration with {Count} native widgets.",
            Plugin.Instance.Configuration.Widgets.Length);
    }

    private void RegisterIndexHtmlTransformation()
    {
        var payload = new JObject
        {
            ["id"] = Guid.Parse("3adf1f1f-1541-4e47-b9e3-34d2d2968af6"),
            ["fileNamePattern"] = "index.html",
            ["callbackAssembly"] = GetType().Assembly.FullName,
            ["callbackClass"] = typeof(TransformationPatches).FullName,
            ["callbackMethod"] = nameof(TransformationPatches.IndexHtml)
        };

        _detector.RegisterTransformation(payload);
        _logger.LogInformation("Registered index.html transformation for JellyUX Homepage.");
    }

    private void RegisterLoadSectionsTransformations()
    {
        var chunks = TransformationPatches.FindLoadSectionsChunks(_applicationPaths.WebPath, _fileSystem, _logger);

        if (chunks.Count == 0)
        {
            return;
        }

        var fragmentTemplate = TransformationPatches.LoadInjectFragmentTemplate();

        foreach (var chunkPath in chunks)
        {
            var fileName = Path.GetFileName(chunkPath);

            WarnIfLoadSectionsHookHasDrifted(chunkPath, fileName, fragmentTemplate);

            // Build a regex pattern that matches the same chunk regardless of content-hash
            // e.g. "56213.a6cde3c8ba80d7030952.chunk.js" -> "56213\.[^.]+\.chunk\.js"
            var nameParts = fileName.Split('.');
            var prefix = nameParts.Length >= 3 ? nameParts[0] : fileName;
            var fileNamePattern = $"{prefix}\\.[^.]+\\.chunk\\.js";

            var chunkId = Guid.NewGuid();
            var payload = new JObject
            {
                ["id"] = chunkId.ToString(),
                ["fileNamePattern"] = fileNamePattern,
                ["callbackAssembly"] = GetType().Assembly.FullName,
                ["callbackClass"] = typeof(TransformationPatches).FullName,
                ["callbackMethod"] = nameof(TransformationPatches.PatchLoadSections)
            };

            _detector.RegisterTransformation(payload);
            _logger.LogInformation(
                "Registered loadSections chunk transformation for {FileName} (pattern: {Pattern}, id: {Id}).",
                fileName,
                fileNamePattern,
                chunkId);
        }
    }

    /// <summary>
    /// Runs a dry splice attempt against the detected chunk at startup so that a Jellyfin Web bundle
    /// drift (the minified module self-reference no longer resolving) is surfaced as an explicit log
    /// warning immediately, instead of silently falling back to native rendering and only being
    /// noticed later when the home page fails to show JellyUX sections.
    /// </summary>
    private void WarnIfLoadSectionsHookHasDrifted(string chunkPath, string fileName, string? fragmentTemplate)
    {
        if (fragmentTemplate is null)
        {
            return;
        }

        string content;
        try
        {
            content = _fileSystem.ReadAllText(chunkPath);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not read chunk file for drift self-check: {File}", chunkPath);
            return;
        }

        var outcome = TransformationPatches.TryPatchLoadSections(content, fragmentTemplate).Outcome;
        if (outcome == LoadSectionsOutcome.HookNotFound)
        {
            _logger.LogWarning(
                "Chunk {FileName} contains ',loadSections:' but the minified hook could not be resolved. "
                + "Jellyfin Web may have drifted; the home page will fall back to native rendering. "
                + "See CLAUDE.md 'Jellyfin Update Procedure'.",
                fileName);
        }
    }
}
