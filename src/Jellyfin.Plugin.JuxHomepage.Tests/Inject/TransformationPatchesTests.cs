using Jellyfin.Plugin.JuxHomepage.Inject;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Inject;

// Covers TransformationPatches.TryPatchLoadSections, extracted in Phase 9 of TODO_V2.md from the
// reflective PatchLoadSections entry point specifically so this splicing logic -- the most fragile
// mechanism in the project (see CLAUDE.md "Jellyfin Update Procedure") -- could be unit tested without
// reflection or embedded-resource I/O. Fixtures below are short representative stand-ins for the real
// minified chunk; update them if a future Jellyfin Web bump changes the shape of the surrounding code
// around ",loadSections:".
public sealed class TransformationPatchesTests
{
    private const string FragmentTemplate = "/*{{cardbuilder}}{{layoutmanager}}{{shapes}}{{approuter}}{{globalize}}{{this_hook}}*/";

    [Fact]
    public void TryPatchLoadSections_ValidChunkWithHook_SplicesFragmentAndReplacesTokens()
    {
        const string raw = "var u={default:1};var n={A:1};,loadSections:function(){},other:2";

        var (content, outcome) = TransformationPatches.TryPatchLoadSections(raw, FragmentTemplate);

        Assert.Equal(LoadSectionsOutcome.Patched, outcome);
        Assert.Contains(",originalLoadSections:", content, StringComparison.Ordinal);
        // The last "var X=" before ",loadSections:" is "n" -- confirms {{this_hook}} resolution.
        Assert.Contains("/*", content, StringComparison.Ordinal);
        Assert.DoesNotContain("{{this_hook}}", content, StringComparison.Ordinal);
        Assert.DoesNotContain("{{cardbuilder}}", content, StringComparison.Ordinal);
    }

    [Fact]
    public void TryPatchLoadSections_NoLoadSectionsMarker_ReturnsContentUnchanged()
    {
        const string raw = "var u={default:1};function unrelated(){return 1;}";

        var (content, outcome) = TransformationPatches.TryPatchLoadSections(raw, FragmentTemplate);

        Assert.Equal(LoadSectionsOutcome.NoMarker, outcome);
        Assert.Equal(raw, content);
    }

    [Fact]
    public void TryPatchLoadSections_MarkerPresentButNoVarDeclaration_ReturnsHookNotFoundAndContentUnchanged()
    {
        // Simulates a Jellyfin Web drift where the module self-reference is no longer declared via
        // "var X=" immediately before the marker (e.g. a minifier switch to a different pattern).
        // Regression test for the Phase 9 fix: this outcome must be reported (StartupService logs a
        // warning), never silently swallowed, and the content must remain untouched (safe fallback).
        const string raw = "someExpression.without.a.var.decl,loadSections:function(){},other:2";

        var (content, outcome) = TransformationPatches.TryPatchLoadSections(raw, FragmentTemplate);

        Assert.Equal(LoadSectionsOutcome.HookNotFound, outcome);
        Assert.Equal(raw, content);
    }
}
