using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests;

public sealed class WidgetServiceTests : IDisposable
{
    private readonly SessionCache _sessionCache = new();
    private readonly Mock<IUserConfigurationStore> _userConfigStoreMock = new();

    // -------------------------------------------------------------------------
    // MinItems filtering
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetWidgetsForUser_WidgetMeetsMinItems_IncludedInResult()
    {
        const string widgetType = "enough-items";
        const int minItems = 4;

        var widget = MakeWidget(widgetType, totalRecordCount: 10);
        var service = BuildService(
            registeredWidgets: [widget],
            globalWidgets:
            [
                new WidgetConfig { WidgetType = widgetType, Enabled = true, MinItems = minItems, Order = 0 }
            ]);

        var result = await service.GetWidgetsForUser(Guid.NewGuid(), page: 0, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(widgetType, result[0].WidgetType);
    }

    [Fact]
    public async Task GetWidgetsForUser_WidgetBelowMinItems_ExcludedFromResult()
    {
        const string widgetType = "few-items";
        const int minItems = 4;

        var widget = MakeWidget(widgetType, totalRecordCount: 2);
        var service = BuildService(
            registeredWidgets: [widget],
            globalWidgets:
            [
                new WidgetConfig { WidgetType = widgetType, Enabled = true, MinItems = minItems, Order = 0 }
            ]);

        var result = await service.GetWidgetsForUser(Guid.NewGuid(), page: 0, CancellationToken.None);

        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // Order sorting
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetWidgetsForUser_WidgetsSortedByOrder()
    {
        var wA = MakeWidget("a", totalRecordCount: 10);
        var wB = MakeWidget("b", totalRecordCount: 10);
        var wC = MakeWidget("c", totalRecordCount: 10);

        var service = BuildService(
            registeredWidgets: [wA, wB, wC],
            globalWidgets:
            [
                new WidgetConfig { WidgetType = "a", Enabled = true, MinItems = 1, Order = 20 },
                new WidgetConfig { WidgetType = "b", Enabled = true, MinItems = 1, Order = 5 },
                new WidgetConfig { WidgetType = "c", Enabled = true, MinItems = 1, Order = 10 }
            ]);

        var result = await service.GetWidgetsForUser(Guid.NewGuid(), page: 0, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal("b", result[0].WidgetType);
        Assert.Equal("c", result[1].WidgetType);
        Assert.Equal("a", result[2].WidgetType);
    }

    // -------------------------------------------------------------------------
    // Pagination
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetWidgetsForUser_Pagination_Page0Returns20()
    {
        var widgets = Enumerable.Range(0, 25)
            .Select(i => MakeWidget($"w{i:D2}", totalRecordCount: 10))
            .ToArray();

        var configs = widgets
            .Select((w, i) => new WidgetConfig
            {
                WidgetType = $"w{i:D2}",
                Enabled = true,
                MinItems = 1,
                Order = i
            })
            .ToArray();

        var service = BuildService(registeredWidgets: widgets, globalWidgets: configs);
        var userId = Guid.NewGuid();

        var page0 = await service.GetWidgetsForUser(userId, page: 0, CancellationToken.None);
        var page1 = await service.GetWidgetsForUser(userId, page: 1, CancellationToken.None);

        Assert.Equal(20, page0.Count);
        Assert.Equal(5, page1.Count);
    }

    // -------------------------------------------------------------------------
    // GetWidgetItems
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetWidgetItems_KnownWidget_ReturnsResult()
    {
        const string widgetType = "known";
        var widget = MakeWidget(widgetType, totalRecordCount: 5);

        var service = BuildService(registeredWidgets: [widget], globalWidgets: []);

        var result = await service.GetWidgetItems(
            Guid.NewGuid(), widgetType, additionalData: null,
            startIndex: 0, limit: 20, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetWidgetItems_UnknownWidget_ReturnsNull()
    {
        var service = BuildService(registeredWidgets: [], globalWidgets: []);

        var result = await service.GetWidgetItems(
            Guid.NewGuid(), "ghost", additionalData: null,
            startIndex: 0, limit: 20, CancellationToken.None);

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private WidgetService BuildService(IWidget[] registeredWidgets, WidgetConfig[] globalWidgets)
    {
        var registry = new WidgetRegistry();
        foreach (var w in registeredWidgets)
        {
            registry.Register(w);
        }

        _userConfigStoreMock
            .Setup(s => s.GetUserConfiguration(It.IsAny<Guid>()))
            .Returns((UserConfiguration?)null);

        var config = new PluginConfiguration { Widgets = globalWidgets };

        return new WidgetService(
            registry,
            _sessionCache,
            _userConfigStoreMock.Object,
            () => config,
            NullLogger<WidgetService>.Instance);
    }

    private static IWidget MakeWidget(string widgetType, int totalRecordCount)
    {
        var mock = new Mock<IWidget>();
        mock.Setup(w => w.WidgetType).Returns(widgetType);
        mock.Setup(w => w.DefaultDisplayName).Returns(widgetType);
        mock.Setup(w => w.MaxInstances).Returns(1);

        mock.Setup(w => w.CreateInstances(
                It.IsAny<Guid>(),
                It.IsAny<WidgetInstanceConfig>(),
                It.IsAny<int>()))
            .Returns(() => [mock.Object]);

        mock.Setup(w => w.GetItemsAsync(It.IsAny<WidgetPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WidgetResult([], totalRecordCount));

        mock.Setup(w => w.GetDescriptor())
            .Returns(new WidgetDescriptor { WidgetType = widgetType });

        return mock.Object;
    }

    public void Dispose() => _sessionCache.Dispose();
}
