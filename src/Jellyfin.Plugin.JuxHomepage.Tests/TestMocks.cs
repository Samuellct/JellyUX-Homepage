using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Moq;

namespace Jellyfin.Plugin.JuxHomepage.Tests;

/// <summary>
/// Shared mock factories extracted from setup code duplicated across widget test files (Phase 2 of
/// TODO_V2.md). Each call returns a fresh instance -- do not cache/share across tests, since some
/// callers rely on a distinct Guid Id per test.
/// </summary>
internal static class TestMocks
{
    /// <summary>Returns a new default test user (fresh Guid Id each call).</summary>
    internal static User DefaultUser() => new("test", "Default", "Default");

    /// <summary>Returns a mock IUserManager whose GetUserById always returns null (unknown user).</summary>
    internal static Mock<IUserManager> UserManagerReturningNull()
    {
        var mock = new Mock<IUserManager>();
        mock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns((User?)null);
        return mock;
    }

    /// <summary>Returns a mock IUserManager whose GetUserById returns the given user for any id.</summary>
    internal static Mock<IUserManager> UserManagerReturning(User user)
    {
        var mock = new Mock<IUserManager>();
        mock.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(user);
        return mock;
    }

    /// <summary>Returns a mock IDtoService whose GetBaseItemDtos always returns an empty list.</summary>
    internal static Mock<IDtoService> DtoServiceReturningEmpty()
    {
        var mock = new Mock<IDtoService>();
        mock.Setup(m => m.GetBaseItemDtos(
                It.IsAny<IReadOnlyList<BaseItem>>(),
                It.IsAny<DtoOptions>(),
                It.IsAny<User>(),
                It.IsAny<BaseItem>()))
            .Returns([]);
        return mock;
    }
}
