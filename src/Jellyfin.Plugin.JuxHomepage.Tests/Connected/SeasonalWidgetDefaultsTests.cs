using Jellyfin.Plugin.JuxHomepage.Widgets.Connected;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Connected;

public sealed class SeasonalWidgetDefaultsTests
{
    [Fact]
    public void Build_ReturnsFourDisabledPresets()
    {
        var presets = SeasonalWidgetDefaults.Build();

        Assert.Equal(4, presets.Length);
        Assert.All(presets, p =>
        {
            Assert.Equal("jux.connected.seasonal", p.WidgetType);
            Assert.False(p.Enabled);
        });
    }

    [Fact]
    public void Build_EachPresetHasAUniqueFixedValueGuid()
    {
        var presets = SeasonalWidgetDefaults.Build();

        var ids = presets
            .Select(p => p.ExtraParams.Single(e => e.Key == "value").Value)
            .ToList();

        Assert.Equal(4, ids.Distinct().Count());

        // Fixed, not freshly generated each call -- required for the Plugin.cs migration
        // (AppendSeasonalPresetsIfMissing) to stay idempotent across repeated calls.
        var idsSecondCall = SeasonalWidgetDefaults.Build()
            .Select(p => p.ExtraParams.Single(e => e.Key == "value").Value)
            .ToList();
        Assert.Equal(ids, idsSecondCall);
    }

    [Theory]
    [InlineData("Halloween", "10-01", "10-31", "halloween")]
    [InlineData("Christmas", "12-01", "12-25", "christmas")]
    [InlineData("Valentine's Day", "02-07", "02-14", "valentines")]
    [InlineData("New Year", "12-26", "01-06", "newyear")]
    public void Build_PresetHasExpectedDatesAndTheme(
        string displayName,
        string expectedStart,
        string expectedEnd,
        string expectedTheme)
    {
        var preset = SeasonalWidgetDefaults.Build().Single(p => p.CustomDisplayName == displayName);

        Assert.Equal(expectedStart, preset.ExtraParams.Single(e => e.Key == "startDate").Value);
        Assert.Equal(expectedEnd, preset.ExtraParams.Single(e => e.Key == "endDate").Value);
        Assert.Equal(expectedTheme, preset.ExtraParams.Single(e => e.Key == "theme").Value);
    }
}
