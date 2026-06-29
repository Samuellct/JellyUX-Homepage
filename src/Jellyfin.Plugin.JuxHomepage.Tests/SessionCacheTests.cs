using Jellyfin.Plugin.JuxHomepage.Widgets;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests;

public sealed class SessionCacheTests : IDisposable
{
    private readonly SessionCache _cache = new();

    [Fact]
    public void Set_ThenTryGet_WithinTtl_ReturnsHit()
    {
        var userId = Guid.NewGuid();
        var descriptors = new List<WidgetDescriptor> { new() { WidgetType = "w1" } };

        _cache.Set(userId, descriptors);
        var hit = _cache.TryGet(userId, TimeSpan.FromMinutes(15), out var result);

        Assert.True(hit);
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public void TryGet_ExpiredTtl_ReturnsMiss()
    {
        var userId = Guid.NewGuid();
        _cache.Set(userId, [new WidgetDescriptor { WidgetType = "w1" }]);

        // TimeSpan.Zero means any entry - no matter how fresh - is considered expired.
        var hit = _cache.TryGet(userId, TimeSpan.Zero, out var result);

        Assert.False(hit);
        Assert.Null(result);
    }

    [Fact]
    public void TryGet_NoEntry_ReturnsMiss()
    {
        var hit = _cache.TryGet(Guid.NewGuid(), TimeSpan.FromMinutes(15), out var result);

        Assert.False(hit);
        Assert.Null(result);
    }

    [Fact]
    public void Invalidate_RemovesEntry_SubsequentTryGetReturnsMiss()
    {
        var userId = Guid.NewGuid();
        _cache.Set(userId, [new WidgetDescriptor { WidgetType = "w1" }]);

        _cache.Invalidate(userId);
        var hit = _cache.TryGet(userId, TimeSpan.FromMinutes(15), out _);

        Assert.False(hit);
    }

    [Fact]
    public void Set_OverwritesExistingEntry()
    {
        var userId = Guid.NewGuid();
        _cache.Set(userId, [new WidgetDescriptor { WidgetType = "old" }]);
        _cache.Set(userId, [new WidgetDescriptor { WidgetType = "new" }]);

        _cache.TryGet(userId, TimeSpan.FromMinutes(15), out var result);

        Assert.NotNull(result);
        Assert.Equal("new", result![0].WidgetType);
    }

    public void Dispose() => _cache.Dispose();
}
