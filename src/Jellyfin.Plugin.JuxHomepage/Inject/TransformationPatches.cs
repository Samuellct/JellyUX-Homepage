using System.Reflection;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.JuxHomepage.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.Inject;

/// <summary>
/// Static transformation callbacks invoked by the FileTransformation plugin via reflection.
/// Each method receives a <see cref="PatchRequestPayload"/> and returns the transformed content.
/// </summary>
public static class TransformationPatches
{
    // Minified variable names in the Jellyfin 10.11.10 home chunk (56213.*.chunk.js).
    // Verified by extracting the web bundle from jellyfin/jellyfin:10.11.10 and inspecting
    // the module-level var declarations around ",loadSections:".
    // Update these constants when targeting a new Jellyfin web release.
    private const string HookCardBuilder = "u";    // u.default  = cardBuilder
    private const string HookLayoutManager = "n";  // n.A        = layoutManager
    private const string HookShapes = "y";         // y.UI / y.xK / y.zP = backdrop / portrait / square
    private const string HookAppRouter = "p";      // p.appRouter = appRouter
    private const string HookGlobalize = "s";      // s.Ay       = globalize

    private static readonly Regex LastVarRegex =
        new Regex(@"var\s+([a-zA-Z_$][a-zA-Z0-9_$]*)\s*=", RegexOptions.Compiled);

    private const string InjectResourceSuffix = "Web.jux-loadsections-inject.js";

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
    /// Patches the Jellyfin home chunk.js to override loadSections with JUX rendering.
    /// Splices the embedded inject fragment at the ",loadSections:" site, capturing the
    /// minified in-scope modules into window.JellyfinAPI and delegating to window.JUXHomepage.
    /// Called by FileTransformation via reflection — must remain public and static.
    /// </summary>
    /// <param name="content">Payload containing the raw chunk.js contents.</param>
    /// <returns>Transformed JS with loadSections overridden.</returns>
    public static string PatchLoadSections(PatchRequestPayload content)
    {
        var raw = content.Contents ?? string.Empty;

        if (!raw.Contains(",loadSections:", StringComparison.Ordinal))
        {
            return raw;
        }

        var parts = raw.Split(",loadSections:", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return raw;
        }

        // The module self-reference (this) resolves to the last var X= before ,loadSections:
        var matches = LastVarRegex.Matches(parts[0]);
        if (matches.Count == 0)
        {
            return raw;
        }

        var thisHook = matches[^1].Groups[1].Value.Trim();

        var fragmentStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(
                Assembly.GetExecutingAssembly().GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(InjectResourceSuffix, StringComparison.OrdinalIgnoreCase))
                ?? string.Empty);

        if (fragmentStream is null)
        {
            return raw;
        }

        using var reader = new StreamReader(fragmentStream);
        var fragment = reader.ReadToEnd()
            .Replace("{{cardbuilder}}", HookCardBuilder, StringComparison.Ordinal)
            .Replace("{{layoutmanager}}", HookLayoutManager, StringComparison.Ordinal)
            .Replace("{{shapes}}", HookShapes, StringComparison.Ordinal)
            .Replace("{{approuter}}", HookAppRouter, StringComparison.Ordinal)
            .Replace("{{globalize}}", HookGlobalize, StringComparison.Ordinal)
            .Replace("{{this_hook}}", thisHook, StringComparison.Ordinal);

        return raw.Replace(",loadSections:", $",loadSections:{fragment},originalLoadSections:", StringComparison.Ordinal);
    }

    /// <summary>
    /// Scans the web path for the minified chunk.js that defines Jellyfin's loadSections.
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
                    "Found loadSections chunk: {FileName}.",
                    Path.GetFileName(match));
            }
        }

        return matches;
    }
}
