using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Discovers and registers <see cref="IWidget"/> implementations from external DLLs placed in a
/// dedicated widget-pack directory, without modifying the core plugin.
/// <para>
/// <b>Security posture:</b> this is <i>not</i> a sandbox. Every discovered assembly is loaded with
/// <see cref="Assembly.LoadFile(string)"/> into the default load context and executes with the same
/// trust as the host plugin -- there is no signature verification, no permission restriction, and no
/// process isolation. A dedicated <see cref="System.Runtime.Loader.AssemblyLoadContext"/> per pack
/// would only isolate dependency resolution (two packs referencing different versions of the same
/// library), which has no real consumer today (zero external widget packs exist); it would not add
/// any actual security boundary. A genuine sandbox would require assembly signing or a separate
/// process/WASM host -- out of scope at the current single-author scale, and accepted as a risk until
/// a real third-party widget pack ecosystem becomes a concrete goal (see Audit_after_v1.md).
/// </para>
/// <para>
/// <b>Directory placement is load-bearing, not cosmetic.</b> The <c>packDirectory</c> argument to
/// <see cref="LoadInto"/> must never be rooted under
/// <see cref="MediaBrowser.Common.Configuration.IApplicationPaths.PluginConfigurationsPath"/> --
/// that path lives inside Jellyfin's own <c>/config/plugins</c> tree, which the core
/// <c>PluginManager</c> scans recursively (any <c>*.dll</c> anywhere under any top-level subdirectory,
/// including <c>configurations/</c>) and treats as belonging to a candidate plugin. A malformed DLL
/// placed there once caused Jellyfin's core plugin manager to disable and wipe the entire
/// <c>configurations</c> directory tree at the next restart, destroying unrelated plugin data (TMDb
/// cache, per-user overrides) that happened to live alongside it. Always root this directory under
/// <see cref="MediaBrowser.Common.Configuration.IApplicationPaths.DataPath"/> instead.
/// </para>
/// </summary>
public static class WidgetPackLoader
{
    /// <summary>
    /// Scans the top level of <paramref name="packDirectory"/> for <c>*.dll</c> files, loads each one,
    /// and registers every non-abstract <see cref="IWidget"/> implementation found into
    /// <paramref name="registry"/>. Creates the directory if it does not exist yet, so it becomes a
    /// self-documenting drop point for administrators. Never throws: every failure (a malformed DLL,
    /// a type that fails to construct, a duplicate widget type) is captured, logged, and returned.
    /// </summary>
    /// <param name="registry">The widget registry to register discovered widgets into.</param>
    /// <param name="serviceProvider">The DI service provider used to construct widget instances.</param>
    /// <param name="packDirectory">The directory to scan for widget pack DLLs (not recursive).</param>
    /// <param name="logger">Logger for registration successes and failures.</param>
    /// <returns>The list of load failures encountered, empty if every pack loaded cleanly.</returns>
    public static IReadOnlyList<WidgetPackLoadError> LoadInto(
        IWidgetRegistry registry,
        IServiceProvider serviceProvider,
        string packDirectory,
        ILogger logger)
    {
        var errors = new List<WidgetPackLoadError>();
        Directory.CreateDirectory(packDirectory);

        foreach (var dllPath in Directory.GetFiles(packDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(dllPath);

            try
            {
                var assembly = Assembly.LoadFile(dllPath);
                foreach (var type in assembly.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface && t.IsAssignableTo(typeof(IWidget))))
                {
                    RegisterPackWidget(registry, serviceProvider, type, fileName, logger, errors);
                }
            }
            catch (BadImageFormatException)
            {
                const string message = "Not a valid .NET assembly, or built for an incompatible architecture.";
                logger.LogError("Failed to load external widget DLL {Dll}: {Message}", fileName, message);
                errors.Add(new WidgetPackLoadError(fileName, message));
            }
            catch (ReflectionTypeLoadException)
            {
                const string message = "Compiled against an incompatible plugin version; recompile against the current release.";
                logger.LogError("Failed to load external widget DLL {Dll}: {Message}", fileName, message);
                errors.Add(new WidgetPackLoadError(fileName, message));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load external widget DLL: {Dll}.", fileName);
                errors.Add(new WidgetPackLoadError(fileName, ex.Message));
            }
        }

        return errors;
    }

    private static void RegisterPackWidget(
        IWidgetRegistry registry,
        IServiceProvider serviceProvider,
        Type type,
        string fileName,
        ILogger logger,
        List<WidgetPackLoadError> errors)
    {
        try
        {
            var widget = (IWidget)ActivatorUtilities.CreateInstance(serviceProvider, type);
            registry.Register(widget);
            logger.LogInformation(
                "Registered external widget '{WidgetType}' from {Dll}.",
                widget.WidgetType,
                fileName);
        }
        catch (InvalidOperationException ex)
        {
            // Thrown by WidgetRegistry.Register when the widget type is already registered.
            logger.LogError(ex, "Failed to register widget type {Type} from {Dll}.", type.FullName, fileName);
            errors.Add(new WidgetPackLoadError(fileName, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register widget type {Type} from {Dll}.", type.FullName, fileName);
            errors.Add(new WidgetPackLoadError(fileName, $"{type.FullName}: {ex.Message}"));
        }
    }
}
