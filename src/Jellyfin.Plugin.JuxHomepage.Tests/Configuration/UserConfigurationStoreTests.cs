using Jellyfin.Plugin.JuxHomepage.Configuration;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Configuration;

public sealed class UserConfigurationStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly UserConfigurationStore _store;

    public UserConfigurationStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "jux-user-config-tests-" + Guid.NewGuid());

        var applicationPathsMock = new Mock<IApplicationPaths>();
        applicationPathsMock.Setup(p => p.PluginConfigurationsPath).Returns(_tempDir);

        _store = new UserConfigurationStore(
            applicationPathsMock.Object,
            NullLogger<UserConfigurationStore>.Instance);
    }

    [Fact]
    public void GetUserConfiguration_NeverSaved_ReturnsNull()
    {
        var result = _store.GetUserConfiguration(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void SaveThenGet_RoundTripsEquivalentData()
    {
        var userId = Guid.NewGuid();
        var config = new UserConfiguration
        {
            UserId = userId,
            Enabled = false,
            WidgetOverrides = [new Widgets.WidgetConfig { WidgetType = "jux.admin.genre", MinItems = 2 }]
        };

        _store.SaveUserConfiguration(config);
        var result = _store.GetUserConfiguration(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(false, result.Enabled);
        Assert.Single(result.WidgetOverrides);
        Assert.Equal("jux.admin.genre", result.WidgetOverrides[0].WidgetType);
        Assert.Equal(2, result.WidgetOverrides[0].MinItems);
    }

    [Fact]
    public void SaveUserConfiguration_LeavesNoStrayTempFileBehind()
    {
        var userId = Guid.NewGuid();
        _store.SaveUserConfiguration(new UserConfiguration { UserId = userId });

        var usersDir = Path.Combine(_tempDir, "Jellyfin.Plugin.JuxHomepage", "users");
        var tmpFiles = Directory.GetFiles(usersDir, "*.tmp");

        Assert.Empty(tmpFiles);
    }

    public void Dispose()
    {
        _store.Dispose();

        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; leftover temp dirs under %TEMP% are harmless.
        }
    }
}
