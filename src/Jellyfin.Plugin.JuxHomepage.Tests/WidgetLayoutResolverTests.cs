using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Localization;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests;

// Covers the global/user-override merge logic extracted into WidgetLayoutResolver (Phase 7.1 of
// TODO_V2.md). This is a real coverage gap fix: WidgetServiceTests' BuildService helper always mocks
// IUserConfigurationStore.GetUserConfiguration to return null, so the override-merge branch was never
// previously exercised by any test. Everything else (sorting, pagination, fan-out, MinItems
// filtering, translation) is already covered end-to-end via WidgetServiceTests' public entry points
// and is not duplicated here.
public sealed class WidgetLayoutResolverTests
{
    [Fact]
    public async Task BuildDescriptors_AllowUserOverrideTrue_UserOverrideWins()
    {
        var widget = MakeWidget("jux.test.widget", totalRecordCount: 10);
        var registry = new WidgetRegistry();
        registry.Register(widget);

        var globalConfig = new WidgetConfig
        {
            WidgetType = "jux.test.widget",
            CustomDisplayName = "Global Name",
            AllowUserOverride = true,
            Enabled = true,
            MinItems = 4
        };
        var userOverride = new WidgetConfig
        {
            WidgetType = "jux.test.widget",
            CustomDisplayName = "Override Name",
            AllowUserOverride = true,
            Enabled = true,
            MinItems = 4
        };

        var userConfigStoreMock = new Mock<IUserConfigurationStore>();
        userConfigStoreMock
            .Setup(s => s.GetUserConfiguration(It.IsAny<Guid>()))
            .Returns(new UserConfiguration { WidgetOverrides = [userOverride] });

        var resolver = new WidgetLayoutResolver(
            registry,
            userConfigStoreMock.Object,
            new LocalizationService(new Dictionary<string, IReadOnlyDictionary<string, string>>()),
            () => new PluginConfiguration { Widgets = [globalConfig] },
            NullLogger<WidgetLayoutResolver>.Instance);

        var descriptors = await resolver.BuildDescriptors(Guid.NewGuid(), lang: null, CancellationToken.None);

        var descriptor = Assert.Single(descriptors);
        Assert.Equal("Override Name", descriptor.DisplayName);
    }

    [Fact]
    public async Task BuildDescriptors_AllowUserOverrideFalse_UserOverrideIgnored()
    {
        var widget = MakeWidget("jux.test.widget", totalRecordCount: 10);
        var registry = new WidgetRegistry();
        registry.Register(widget);

        var globalConfig = new WidgetConfig
        {
            WidgetType = "jux.test.widget",
            CustomDisplayName = "Global Name",
            AllowUserOverride = false,
            Enabled = true,
            MinItems = 4
        };
        var userOverride = new WidgetConfig
        {
            WidgetType = "jux.test.widget",
            CustomDisplayName = "Override Name",
            AllowUserOverride = false,
            Enabled = true,
            MinItems = 4
        };

        var userConfigStoreMock = new Mock<IUserConfigurationStore>();
        userConfigStoreMock
            .Setup(s => s.GetUserConfiguration(It.IsAny<Guid>()))
            .Returns(new UserConfiguration { WidgetOverrides = [userOverride] });

        var resolver = new WidgetLayoutResolver(
            registry,
            userConfigStoreMock.Object,
            new LocalizationService(new Dictionary<string, IReadOnlyDictionary<string, string>>()),
            () => new PluginConfiguration { Widgets = [globalConfig] },
            NullLogger<WidgetLayoutResolver>.Instance);

        var descriptors = await resolver.BuildDescriptors(Guid.NewGuid(), lang: null, CancellationToken.None);

        var descriptor = Assert.Single(descriptors);
        Assert.Equal("Global Name", descriptor.DisplayName);
    }

    private static IWidget MakeWidget(string widgetType, int totalRecordCount)
    {
        var mock = new Mock<IWidget>();
        mock.Setup(w => w.WidgetType).Returns(widgetType);
        mock.Setup(w => w.DefaultDisplayName).Returns(widgetType);
        mock.Setup(w => w.MaxInstances).Returns(1);
        mock.Setup(w => w.CreateInstances(It.IsAny<Guid>(), It.IsAny<WidgetInstanceConfig>(), It.IsAny<int>()))
            .Returns((Guid _, WidgetInstanceConfig _, int _) => [mock.Object]);
        mock.Setup(w => w.GetItemsAsync(It.IsAny<WidgetPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WidgetResult([], totalRecordCount));
        mock.Setup(w => w.GetDescriptor()).Returns(new WidgetDescriptor { WidgetType = widgetType });
        return mock.Object;
    }
}
