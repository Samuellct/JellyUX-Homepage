using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.Widgets;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests;

public sealed class PluginValidationTests
{
    [Fact]
    public void ValidateConfiguration_ClampsCacheValues()
    {
        var config = new PluginConfiguration
        {
            Cache = new CacheConfig { SessionTtlMinutes = 0, TMDbRefreshIntervalHours = 0 }
        };

        Plugin.ValidateConfiguration(config);

        Assert.Equal(1, config.Cache.SessionTtlMinutes);
        Assert.Equal(1, config.Cache.TMDbRefreshIntervalHours);
    }

    [Fact]
    public void ValidateConfiguration_ClampsWidgetItemAndInstanceRanges()
    {
        var config = new PluginConfiguration
        {
            Widgets =
            [
                new WidgetConfig
                {
                    WidgetType = "jux.admin.genre",
                    MinItems = -5,
                    MaxItems = 0,
                    MinInstances = 0,
                    MaxInstances = 0
                }
            ]
        };

        Plugin.ValidateConfiguration(config);

        var widget = config.Widgets[0];
        Assert.Equal(0, widget.MinItems);
        Assert.Equal(1, widget.MaxItems);
        Assert.True(widget.MinItems <= widget.MaxItems);
        Assert.Equal(1, widget.MinInstances);
        Assert.Equal(1, widget.MaxInstances);
        Assert.True(widget.MinInstances <= widget.MaxInstances);
    }

    [Fact]
    public void ValidateConfiguration_ClampsMinItemsAboveMaxItemsDownToMaxItems()
    {
        var config = new PluginConfiguration
        {
            Widgets = [new WidgetConfig { WidgetType = "jux.admin.genre", MinItems = 50, MaxItems = 10 }]
        };

        Plugin.ValidateConfiguration(config);

        Assert.Equal(10, config.Widgets[0].MinItems);
        Assert.Equal(10, config.Widgets[0].MaxItems);
    }

    [Fact]
    public void ValidateConfiguration_TrimsApiKeyAndNullsBlankValue()
    {
        var config = new PluginConfiguration { ApiKeys = new ApiKeysConfig { TMDb = "  abc123  " } };
        Plugin.ValidateConfiguration(config);
        Assert.Equal("abc123", config.ApiKeys.TMDb);

        var blankConfig = new PluginConfiguration { ApiKeys = new ApiKeysConfig { TMDb = "   " } };
        Plugin.ValidateConfiguration(blankConfig);
        Assert.Null(blankConfig.ApiKeys.TMDb);
    }

    [Fact]
    public void ValidateConfiguration_ClampsTMDbListPageCountsAndNormalizesRegion()
    {
        var config = new PluginConfiguration
        {
            TMDbLists = new TMDbListsConfig
            {
                TrendingMoviesPages = 99,
                TrendingShowsPages = 0,
                AiringTodayPages = 99,
                TopRatedMoviesPages = 0,
                TopRatedShowsPages = 99,
                NowPlayingMoviesPages = 0,
                NowPlayingRegion = "  fr  "
            }
        };

        Plugin.ValidateConfiguration(config);

        Assert.Equal(5, config.TMDbLists.TrendingMoviesPages);
        Assert.Equal(1, config.TMDbLists.TrendingShowsPages);
        Assert.Equal(5, config.TMDbLists.AiringTodayPages);
        Assert.Equal(1, config.TMDbLists.TopRatedMoviesPages);
        Assert.Equal(5, config.TMDbLists.TopRatedShowsPages);
        Assert.Equal(1, config.TMDbLists.NowPlayingMoviesPages);
        Assert.Equal("FR", config.TMDbLists.NowPlayingRegion);
    }

    [Fact]
    public void MigrateConfiguration_NoOpForCurrentSchemaVersion_DoesNotThrow()
    {
        var config = new PluginConfiguration { SchemaVersion = 1 };

        var exception = Record.Exception(() => Plugin.MigrateConfiguration(config));

        Assert.Null(exception);
        Assert.Equal(1, config.SchemaVersion);
    }
}
