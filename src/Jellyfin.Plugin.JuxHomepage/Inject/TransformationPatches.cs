using Jellyfin.Plugin.JuxHomepage.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.Inject;

/// <summary>
/// Static transformation callbacks invoked by the FileTransformation plugin via reflection.
/// Each method receives a <see cref="PatchRequestPayload"/> and returns the transformed content.
/// </summary>
public static class TransformationPatches
{
    /// <summary>
    /// Injects the JellyUX CSS link and JS script tags into Jellyfin's index.html.
    /// Called by FileTransformation via reflection — must remain public and static.
    /// </summary>
    /// <param name="content">Payload containing the raw index.html contents.</param>
    /// <returns>Transformed HTML with JUX resources injected.</returns>
    public static string IndexHtml(PatchRequestPayload content)
    {
        var version = Plugin.Instance?.Version?.ToString() ?? "0";
        var cacheParam = $"?v={version}";

        var linkTag = $"<link rel=\"stylesheet\" href=\"/JuxHomepage/jux-homepage.css{cacheParam}\" />";
        var scriptTag = $"<script src=\"/JuxHomepage/jux-homepage.js{cacheParam}\" defer></script>";

        return (content.Contents ?? string.Empty)
            .Replace("</head>", $"{linkTag}\n</head>", StringComparison.OrdinalIgnoreCase)
            .Replace("</body>", $"{scriptTag}\n</body>", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Scans the web path for the minified chunk.js that defines Jellyfin's loadSections.
    /// Phase 2: detection and logging only. The actual transformation is registered in Phase 5
    /// once the JUX render function and window.JellyfinAPI exist.
    /// </summary>
    /// <param name="webPath">Path to the Jellyfin web directory.</param>
    /// <param name="logger">Logger for reporting results.</param>
    /// <returns>List of matching chunk file paths.</returns>
    public static IReadOnlyList<string> FindLoadSectionsChunks(string webPath, ILogger logger)
    {
        if (!Directory.Exists(webPath))
        {
            logger.LogWarning("Web path does not exist: {WebPath}", webPath);
            return Array.Empty<string>();
        }

        var matches = Directory
            .GetFiles(webPath, "*.chunk.js", SearchOption.AllDirectories)
            .Where(f =>
            {
                try
                {
                    return File.ReadAllText(f).Contains(",loadSections:", StringComparison.Ordinal);
                }
                catch (IOException ex)
                {
                    logger.LogWarning(ex, "Could not read chunk file: {File}", f);
                    return false;
                }
            })
            .ToList();

        if (matches.Count == 0)
        {
            logger.LogWarning(
                "No chunk.js containing ,loadSections: found in {WebPath}. "
                + "This is expected if Jellyfin Web is not installed.",
                webPath);
        }
        else
        {
            foreach (var match in matches)
            {
                logger.LogInformation(
                    "Found loadSections chunk: {FileName} (patch will be registered in Phase 5).",
                    Path.GetFileName(match));
            }
        }

        return matches;
    }
}
