namespace Jellyfin.Plugin.JuxHomepage.Widgets.Connected;

/// <summary>
/// Provides the four seasonal preset <see cref="WidgetConfig"/> rows shipped with the plugin
/// (Halloween, Christmas, Valentine's Day, New Year). Mirrors <see cref="Native.NativeWidgetDefaults"/>,
/// but every row is disabled by default -- an admin opts in to whichever seasons they want.
/// <para>
/// Each row's <c>ExtraParams["value"]</c> uses a fixed, hardcoded GUID rather than a freshly generated
/// one, so a later migration (<see cref="Plugin.MigrateConfiguration"/>, schema v5-to-v6) can detect
/// whether a given preset has already been appended to an existing installation's configuration and
/// stays idempotent if it runs more than once.
/// </para>
/// </summary>
public static class SeasonalWidgetDefaults
{
    /// <summary>The fixed instance id for the Halloween preset row.</summary>
    public static readonly Guid HalloweenId = Guid.Parse("8f3a1b2c-0001-4b1e-9c3a-1f2e3d4c5b6a");

    /// <summary>The fixed instance id for the Christmas preset row.</summary>
    public static readonly Guid ChristmasId = Guid.Parse("8f3a1b2c-0002-4b1e-9c3a-1f2e3d4c5b6a");

    /// <summary>The fixed instance id for the Valentine's Day preset row.</summary>
    public static readonly Guid ValentinesId = Guid.Parse("8f3a1b2c-0003-4b1e-9c3a-1f2e3d4c5b6a");

    /// <summary>The fixed instance id for the New Year preset row.</summary>
    public static readonly Guid NewYearId = Guid.Parse("8f3a1b2c-0004-4b1e-9c3a-1f2e3d4c5b6a");

    /// <summary>
    /// Builds the default seasonal preset configuration rows. All four are disabled by default and
    /// carry no genre/tag filter (an admin-chosen genre could easily match no item in a given
    /// library, since genre taxonomies vary widely; leaving it blank is a safer starting point).
    /// </summary>
    /// <returns>A fixed-order array of four <see cref="WidgetConfig"/> entries.</returns>
    public static WidgetConfig[] Build() =>
    [
        BuildRow(HalloweenId, "Halloween", "10-01", "10-31", "halloween", order: 200),
        BuildRow(ChristmasId, "Christmas", "12-01", "12-25", "christmas", order: 210),
        BuildRow(ValentinesId, "Valentine's Day", "02-07", "02-14", "valentines", order: 220),
        BuildRow(NewYearId, "New Year", "12-26", "01-06", "newyear", order: 230)
    ];

    private static WidgetConfig BuildRow(
        Guid id,
        string displayName,
        string startDate,
        string endDate,
        string theme,
        int order) => new()
    {
        WidgetType = "jux.connected.seasonal",
        CustomDisplayName = displayName,
        Enabled = false,
        Order = order,
        MinItems = 4,
        MaxItems = 20,
        ViewMode = WidgetViewMode.Portrait,
        MinInstances = 1,
        MaxInstances = 1,
        AllowUserOverride = false,
        ExtraParams =
        [
            new WidgetExtraParam { Key = "value", Value = id.ToString() },
            new WidgetExtraParam { Key = "startDate", Value = startDate },
            new WidgetExtraParam { Key = "endDate", Value = endDate },
            new WidgetExtraParam { Key = "theme", Value = theme }
        ]
    };
}
