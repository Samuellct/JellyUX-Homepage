using Jellyfin.Plugin.JuxHomepage.Widgets;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests;

public sealed class WidgetPackLoaderTests : IDisposable
{
    private readonly string _packDirectory;

    public WidgetPackLoaderTests()
    {
        _packDirectory = Path.Combine(Path.GetTempPath(), "jux-widget-pack-tests", Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_packDirectory))
        {
            Directory.Delete(_packDirectory, recursive: true);
        }
    }

    [Fact]
    public void LoadInto_DirectoryDoesNotExist_ReturnsNoErrorsAndCreatesDirectory()
    {
        var registry = new WidgetRegistry();

        var errors = WidgetPackLoader.LoadInto(
            registry,
            new Mock<IServiceProvider>().Object,
            _packDirectory,
            NullLogger.Instance);

        Assert.Empty(errors);
        Assert.True(Directory.Exists(_packDirectory));
        Assert.Empty(registry.GetAll());
    }

    [Fact]
    public void LoadInto_MalformedDll_ReturnsSingleErrorNamingTheFile()
    {
        Directory.CreateDirectory(_packDirectory);
        var brokenDllPath = Path.Combine(_packDirectory, "broken.dll");
        File.WriteAllBytes(brokenDllPath, [0x01, 0x02, 0x03, 0x04, 0x05]);

        var registry = new WidgetRegistry();

        var errors = WidgetPackLoader.LoadInto(
            registry,
            new Mock<IServiceProvider>().Object,
            _packDirectory,
            NullLogger.Instance);

        var error = Assert.Single(errors);
        Assert.Equal("broken.dll", error.FileName);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
        Assert.Empty(registry.GetAll());
    }

    [Fact]
    public void LoadInto_DllInParentDirectory_IsNotScannedNonRecursively()
    {
        var parentDirectory = Path.GetDirectoryName(_packDirectory)!;
        Directory.CreateDirectory(parentDirectory);
        var siblingDllPath = Path.Combine(parentDirectory, "sibling.dll");
        File.WriteAllBytes(siblingDllPath, [0x01, 0x02, 0x03]);

        try
        {
            var registry = new WidgetRegistry();

            var errors = WidgetPackLoader.LoadInto(
                registry,
                new Mock<IServiceProvider>().Object,
                _packDirectory,
                NullLogger.Instance);

            Assert.Empty(errors);
        }
        finally
        {
            File.Delete(siblingDllPath);
        }
    }
}
