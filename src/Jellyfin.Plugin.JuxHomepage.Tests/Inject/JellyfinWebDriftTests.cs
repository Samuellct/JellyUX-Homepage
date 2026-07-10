using Jellyfin.Plugin.JuxHomepage.Inject;
using Jellyfin.Plugin.JuxHomepage.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Inject;

// CI guard (Phase 12.2 of TODO_V2.md): proactively detects a Jellyfin Web bundle drift against the
// project's most fragile mechanism (loadSections splicing) before it silently breaks for users. Reuses
// the exact pure functions extracted in Phase 9 (TransformationPatches.FindLoadSectionsChunks/
// LoadInjectFragmentTemplate/TryPatchLoadSections) against a real jellyfin/jellyfin:latest image,
// instead of re-implementing the detection logic separately (which would risk testing something
// different from the real production code path).
//
// Requires Docker and network access to pull jellyfin/jellyfin:latest -- tagged so it is excluded from
// the normal `dotnet test` run in ci.yml (see the "Category!=RequiresDocker" filter there) and only
// runs in the dedicated .github/workflows/jellyfin-web-drift.yml (cron + workflow_dispatch).
[Trait("Category", "RequiresDocker")]
public sealed class JellyfinWebDriftTests
{
    [Fact]
    public void CurrentLoadSectionsPatterns_StillMatchLatestJellyfinWeb()
    {
        var webPath = Environment.GetEnvironmentVariable("JELLYFIN_WEB_EXTRACT_PATH");
        if (string.IsNullOrEmpty(webPath))
        {
            throw new InvalidOperationException(
                "JELLYFIN_WEB_EXTRACT_PATH must be set to the extracted jellyfin-web directory. "
                + "See .github/workflows/jellyfin-web-drift.yml, which sets this before running this test.");
        }

        var fileSystem = new FileSystem();
        var chunks = TransformationPatches.FindLoadSectionsChunks(webPath, fileSystem, NullLogger.Instance);

        Assert.NotEmpty(chunks);

        var template = TransformationPatches.LoadInjectFragmentTemplate();
        Assert.NotNull(template);

        foreach (var chunkPath in chunks)
        {
            var content = fileSystem.ReadAllText(chunkPath);
            var (_, outcome) = TransformationPatches.TryPatchLoadSections(content, template!);

            // HookNotFound means the minified hook constants (TransformationPatches.cs, HookCardBuilder
            // etc.) no longer resolve against this Jellyfin Web build -- exactly the drift this job
            // exists to catch. See CLAUDE.md "Jellyfin Update Procedure" for how to re-extract them.
            Assert.Equal(LoadSectionsOutcome.Patched, outcome);
        }
    }
}
