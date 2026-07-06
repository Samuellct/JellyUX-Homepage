using Jellyfin.Plugin.JuxHomepage.Widgets;
using Jellyfin.Plugin.JuxHomepage.Widgets.Admin;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Admin;

public sealed class AdminWidgetTests
{
    // -------------------------------------------------------------------------
    // Descriptor tests
    // -------------------------------------------------------------------------

    [Fact]
    public void Genre_GetDescriptor_HasExpectedProperties()
    {
        var widget = new GenreWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.admin.genre", d.WidgetType);
        Assert.Equal(WidgetCategory.Admin, d.Category);
        Assert.Equal(WidgetViewMode.Portrait, d.ViewMode);
        Assert.Equal(4, d.MinItems);
        Assert.Null(d.Route);
    }

    [Fact]
    public void Actor_GetDescriptor_HasExpectedProperties()
    {
        var widget = new ActorWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.admin.actor", d.WidgetType);
        Assert.Equal(WidgetCategory.Admin, d.Category);
        Assert.Equal(WidgetViewMode.Portrait, d.ViewMode);
        Assert.Equal(4, d.MinItems);
    }

    [Fact]
    public void Director_GetDescriptor_HasExpectedProperties()
    {
        var widget = new DirectorWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.admin.director", d.WidgetType);
        Assert.Equal(WidgetCategory.Admin, d.Category);
    }

    [Fact]
    public void Studio_GetDescriptor_HasExpectedProperties()
    {
        var widget = new StudioWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.admin.studio", d.WidgetType);
        Assert.Equal(WidgetCategory.Admin, d.Category);
    }

    [Fact]
    public void Collection_GetDescriptor_HasExpectedProperties()
    {
        var widget = new CollectionWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.admin.collection", d.WidgetType);
        Assert.Equal(WidgetCategory.Admin, d.Category);
    }

    [Fact]
    public void Tag_GetDescriptor_HasExpectedProperties()
    {
        var widget = new TagWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.admin.tag", d.WidgetType);
        Assert.Equal(WidgetCategory.Admin, d.Category);
    }

    [Fact]
    public void Year_GetDescriptor_HasExpectedProperties()
    {
        var widget = new YearWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var d = widget.GetDescriptor();

        Assert.Equal("jux.admin.year", d.WidgetType);
        Assert.Equal(WidgetCategory.Admin, d.Category);
    }

    // -------------------------------------------------------------------------
    // MaxInstances tests
    // -------------------------------------------------------------------------

    [Fact]
    public void AdminWidgets_MaxInstancesIsAtLeastTwo()
    {
        IWidget[] widgets =
        [
            new GenreWidget(new Mock<IUserManager>().Object, new Mock<ILibraryManager>().Object, new Mock<IDtoService>().Object),
            new ActorWidget(new Mock<IUserManager>().Object, new Mock<ILibraryManager>().Object, new Mock<IDtoService>().Object),
            new DirectorWidget(new Mock<IUserManager>().Object, new Mock<ILibraryManager>().Object, new Mock<IDtoService>().Object),
            new StudioWidget(new Mock<IUserManager>().Object, new Mock<ILibraryManager>().Object, new Mock<IDtoService>().Object),
            new CollectionWidget(new Mock<IUserManager>().Object, new Mock<ILibraryManager>().Object, new Mock<IDtoService>().Object),
            new TagWidget(new Mock<IUserManager>().Object, new Mock<ILibraryManager>().Object, new Mock<IDtoService>().Object),
            new YearWidget(new Mock<IUserManager>().Object, new Mock<ILibraryManager>().Object, new Mock<IDtoService>().Object)
        ];

        Assert.All(widgets, w => Assert.True(w.MaxInstances >= 2));
    }

    // -------------------------------------------------------------------------
    // Early-out tests: null user and empty value
    // -------------------------------------------------------------------------

    // GenreWidget does not override GetItemsAsync -- this exercises the null-user check shared,
    // unmodified, by every AdminWidgetBase subclass (Actor/Director/Studio/Collection/Tag/Year).
    // Do not add a per-widget duplicate of this test; it would exercise the exact same code path
    // (see Phase 2 test triage, TODO_V2.md).
    [Fact]
    public async Task Genre_UserNotFound_ReturnsEmpty()
    {
        var widget = new GenreWidget(
            TestMocks.UserManagerReturningNull().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), AdditionalData = "Action" },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        Assert.Empty(result.Items);
    }

    // Same rationale as above: the empty-AdditionalData early-out lives in the shared
    // AdminWidgetBase.GetItemsAsync, not in GenreWidget itself.
    [Fact]
    public async Task Genre_EmptyAdditionalData_ReturnsEmpty()
    {
        var widget = new GenreWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var result = await widget.GetItemsAsync(
            new WidgetPayload { UserId = Guid.NewGuid(), AdditionalData = null },
            CancellationToken.None);

        Assert.Equal(0, result.TotalRecordCount);
        Assert.Empty(result.Items);
    }

    // -------------------------------------------------------------------------
    // CreateInstances test
    // -------------------------------------------------------------------------

    [Fact]
    public void Genre_CreateInstances_ReturnsSameInstance()
    {
        var widget = new GenreWidget(
            new Mock<IUserManager>().Object,
            new Mock<ILibraryManager>().Object,
            new Mock<IDtoService>().Object);

        var instances = widget.CreateInstances(Guid.NewGuid(), new WidgetInstanceConfig(), 3).ToList();

        var single = Assert.Single(instances);
        Assert.Same(widget, single);
    }
}
