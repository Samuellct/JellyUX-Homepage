using Jellyfin.Plugin.JuxHomepage.Inject;
using Jellyfin.Plugin.JuxHomepage.IO;
using Jellyfin.Plugin.JuxHomepage.TMDb;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Inject;

// Regression test for the Phase 10.2 fix (TODO_V2.md): StartAsync must never wait for the
// best-effort TMDb refresh before returning, so FileTransformation registration is never delayed
// behind a slow or unreachable TMDb. If this behavior regresses (e.g. someone re-adds an await on
// the refresh), this test times out waiting on the never-completing refresh task instead of passing.
public sealed class StartupServiceTests
{
    [Fact]
    public async Task StartAsync_TMDbRefreshNeverCompletes_StillReturnsPromptly()
    {
        var neverCompletes = new TaskCompletionSource().Task;

        var tmdbCacheServiceMock = new Mock<ITMDbCacheService>();
        tmdbCacheServiceMock.Setup(s => s.IsStale(It.IsAny<TMDbCacheType>())).Returns(true);
        tmdbCacheServiceMock
            .Setup(s => s.RefreshTrendingMoviesAsync(It.IsAny<CancellationToken>()))
            .Returns(neverCompletes);
        tmdbCacheServiceMock
            .Setup(s => s.RefreshTrendingShowsAsync(It.IsAny<CancellationToken>()))
            .Returns(neverCompletes);
        tmdbCacheServiceMock
            .Setup(s => s.RefreshAiringTodayAsync(It.IsAny<CancellationToken>()))
            .Returns(neverCompletes);

        var applicationPathsMock = new Mock<IApplicationPaths>();
        applicationPathsMock.Setup(p => p.WebPath).Returns("C:\\nonexistent-jellyux-test-path");

        var fileSystemMock = new Mock<IFileSystem>();
        fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

        // IsAvailable() resolves to false in the test process (no FileTransformation assembly
        // loaded), exercising the warning branch. Plugin.Instance is null in tests, so that branch's
        // Plugin.Instance.SaveConfiguration() call is safely skipped -- the same pattern already
        // relied upon by FileTransformationDetectorTests.
        var detector = new FileTransformationDetector(NullLogger<FileTransformationDetector>.Instance);

        var service = new StartupService(
            applicationPathsMock.Object,
            NullLogger<StartupService>.Instance,
            detector,
            tmdbCacheServiceMock.Object,
            fileSystemMock.Object);

        var startTask = service.StartAsync(CancellationToken.None);
        var completed = await Task.WhenAny(startTask, Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.Same(startTask, completed);
        Assert.True(startTask.IsCompletedSuccessfully);
    }
}
