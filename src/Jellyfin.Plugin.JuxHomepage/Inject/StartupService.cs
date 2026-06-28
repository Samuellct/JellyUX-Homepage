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

        TransformationPatches.FindLoadSectionsChunks(_applicationPaths.WebPath, _logger);

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
}
