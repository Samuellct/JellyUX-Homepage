using Jellyfin.Plugin.JuxHomepage.Widgets.Native;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.JuxHomepage.Inject;

/// <summary>
/// Scheduled task that runs at server startup to register web transformations
/// and detect required Jellyfin assets.
/// Auto-discovered by Jellyfin — not registered in DI.
/// </summary>
public class StartupService : IScheduledTask
{
    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<StartupService> _logger;
    private readonly FileTransformationDetector _detector;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupService"/> class.
    /// </summary>
    /// <param name="applicationPaths">Application paths, including WebPath.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="detector">FileTransformation reflection bridge.</param>
    public StartupService(
        IApplicationPaths applicationPaths,
        ILogger<StartupService> logger,
        FileTransformationDetector detector)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
        _detector = detector;
    }

    /// <inheritdoc/>
    public string Name => "JellyUX Homepage Startup";

    /// <inheritdoc/>
    public string Key => "Jellyfin.Plugin.JuxHomepage.Startup";

    /// <inheritdoc/>
    public string Description => "Registers JellyUX Homepage web transformations and detects Jellyfin assets.";

    /// <inheritdoc/>
    public string Category => "Startup Services";

    /// <inheritdoc/>
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0);
        SeedDefaultWidgetConfiguration();

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

        progress.Report(20);

        RegisterIndexHtmlTransformation();

        progress.Report(60);

        RegisterLoadSectionsTransformations();

        progress.Report(100);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.StartupTrigger
        };
    }

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
        var chunks = TransformationPatches.FindLoadSectionsChunks(_applicationPaths.WebPath, _logger);

        if (chunks.Count == 0)
        {
            return;
        }

        foreach (var chunkPath in chunks)
        {
            var fileName = Path.GetFileName(chunkPath);

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
}
