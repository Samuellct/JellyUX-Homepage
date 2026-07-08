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
        var config = new PluginConfiguration { SchemaVersion = 2 };

        var exception = Record.Exception(() => Plugin.MigrateConfiguration(config));

        Assert.Null(exception);
        Assert.Equal(2, config.SchemaVersion);
    }

    [Fact]
    public void MigrateConfiguration_PersonalizedRowWithMaxInstancesGreaterThanOne_ExplodesIntoIndependentRows()
    {
        var config = new PluginConfiguration
        {
            SchemaVersion = 1,
            Widgets =
            [
                new WidgetConfig
                {
                    WidgetType = "jux.personalized.favorite-genre",
                    CustomDisplayName = "My Genres",
                    Order = 20,
                    MaxInstances = 3,
                    ExtraParams = [new WidgetExtraParam { Key = "excludeWatched", Value = "false" }]
                }
            ]
        };

        Plugin.MigrateConfiguration(config);

        Assert.Equal(2, config.SchemaVersion);
        Assert.Equal(3, config.Widgets.Length);

        Assert.Equal("jux.personalized.favorite-genre", config.Widgets[0].WidgetType);
        Assert.Equal("My Genres", config.Widgets[0].CustomDisplayName);
        Assert.Equal(20, config.Widgets[0].Order);
        Assert.Equal(1, config.Widgets[0].MaxInstances);
        Assert.Single(config.Widgets[0].ExtraParams);

        Assert.Equal("jux.personalized.favorite-genre", config.Widgets[1].WidgetType);
        Assert.Null(config.Widgets[1].CustomDisplayName);
        Assert.Equal(30, config.Widgets[1].Order);
        Assert.Equal(1, config.Widgets[1].MaxInstances);
        Assert.Empty(config.Widgets[1].ExtraParams);

        Assert.Equal("jux.personalized.favorite-genre", config.Widgets[2].WidgetType);
        Assert.Null(config.Widgets[2].CustomDisplayName);
        Assert.Equal(40, config.Widgets[2].Order);
        Assert.Equal(1, config.Widgets[2].MaxInstances);
    }

    [Fact]
    public void MigrateConfiguration_NonPersonalizedRowWithMaxInstancesGreaterThanOne_LeftUnchanged()
    {
        var config = new PluginConfiguration
        {
            SchemaVersion = 1,
            Widgets = [new WidgetConfig { WidgetType = "jux.admin.genre", MaxInstances = 5 }]
        };

        Plugin.MigrateConfiguration(config);

        Assert.Equal(2, config.SchemaVersion);
        var widget = Assert.Single(config.Widgets);
        Assert.Equal("jux.admin.genre", widget.WidgetType);
        Assert.Equal(5, widget.MaxInstances);
    }
}
