using System.Reflection;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Controllers;
using Jellyfin.Plugin.JuxHomepage.Localization;
using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Controllers;

// Covers JuxHomepageController. Endpoints requiring HttpContext/authorization (Sections, Section) are
// intentionally out of scope here -- they are already exercised end to end by UserAccessGuardTests
// plus manual testing (Phase 0).
public sealed class JuxHomepageControllerTests
{
    [Fact]
    public void GetTMDbItemCount_UnhandledCacheType_ReturnsZeroAndLogsWarning()
    {
        // Guards against the switch silently reporting 0 for a future TMDbCacheType value that was
        // added to the enum but never wired into this switch (Phase 9 fix, TODO_V2.md).
        var loggerMock = new Mock<ILogger<JuxHomepageController>>();
        var controller = BuildController(logger: loggerMock.Object);

        var method = typeof(JuxHomepageController).GetMethod(
            "GetTMDbItemCount",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var result = (int)method.Invoke(controller, [(TMDbCacheType)999])!;

        Assert.Equal(0, result);
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static JuxHomepageController BuildController(
        Mock<IWidgetRegistry>? registryMock = null,
        Mock<IUserManager>? userManagerMock = null,
        Mock<ITMDbCacheService>? tmdbCacheServiceMock = null,
        Mock<ITMDbApiClient>? tmdbApiClientMock = null,
        ILogger<JuxHomepageController>? logger = null)
    {
        var registry = new WidgetRegistry();
        var widgetService = new WidgetService(
            registry,
            new SessionCache(),
            new WidgetLayoutResolver(
                registry,
                Mock.Of<IUserConfigurationStore>(),
                new LocalizationService(new Dictionary<string, IReadOnlyDictionary<string, string>>()),
                () => new PluginConfiguration(),
                NullLogger<WidgetLayoutResolver>.Instance),
            () => new PluginConfiguration(),
            NullLogger<WidgetService>.Instance);

        return new JuxHomepageController(
            (registryMock ?? new Mock<IWidgetRegistry>()).Object,
            widgetService,
            (userManagerMock ?? new Mock<IUserManager>()).Object,
            (tmdbCacheServiceMock ?? new Mock<ITMDbCacheService>()).Object,
            (tmdbApiClientMock ?? new Mock<ITMDbApiClient>()).Object,
            new LocalizationService(new Dictionary<string, IReadOnlyDictionary<string, string>>()),
            Mock.Of<IAuthorizationContext>(),
            logger ?? NullLogger<JuxHomepageController>.Instance);
    }
}
