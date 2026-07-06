using Jellyfin.Plugin.JuxHomepage.Controllers;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Controllers;

public sealed class UserAccessGuardTests
{
    [Fact]
    public void IsAuthorizedForUser_RequestedUserMatchesAuthenticatedUser_ReturnsTrue()
    {
        var userId = Guid.NewGuid();

        var result = UserAccessGuard.IsAuthorizedForUser(
            requestedUserId: userId,
            authenticatedUserId: userId,
            isApiKey: false,
            isAdministrator: false);

        Assert.True(result);
    }

    [Fact]
    public void IsAuthorizedForUser_RequestedUserDiffersFromAuthenticatedUser_ReturnsFalse()
    {
        var result = UserAccessGuard.IsAuthorizedForUser(
            requestedUserId: Guid.NewGuid(),
            authenticatedUserId: Guid.NewGuid(),
            isApiKey: false,
            isAdministrator: false);

        Assert.False(result);
    }

    [Fact]
    public void IsAuthorizedForUser_Administrator_BypassesUserMatch()
    {
        var result = UserAccessGuard.IsAuthorizedForUser(
            requestedUserId: Guid.NewGuid(),
            authenticatedUserId: Guid.NewGuid(),
            isApiKey: false,
            isAdministrator: true);

        Assert.True(result);
    }

    [Fact]
    public void IsAuthorizedForUser_ServerApiKey_BypassesUserMatch()
    {
        var result = UserAccessGuard.IsAuthorizedForUser(
            requestedUserId: Guid.NewGuid(),
            authenticatedUserId: Guid.Empty,
            isApiKey: true,
            isAdministrator: false);

        Assert.True(result);
    }

    [Fact]
    public void IsAuthorizedForUser_RequestedUserIdIsEmpty_ReturnsFalse()
    {
        var result = UserAccessGuard.IsAuthorizedForUser(
            requestedUserId: Guid.Empty,
            authenticatedUserId: Guid.Empty,
            isApiKey: false,
            isAdministrator: false);

        Assert.False(result);
    }
}
