namespace Jellyfin.Plugin.JuxHomepage.Controllers;

/// <summary>
/// Pure authorization decision for endpoints that accept a userId query parameter, to prevent an
/// authenticated user from reading another user's data by supplying an arbitrary userId (IDOR).
/// </summary>
internal static class UserAccessGuard
{
    /// <summary>
    /// Determines whether the caller is allowed to act on behalf of <paramref name="requestedUserId"/>.
    /// Administrators and server-level API key requests (which are not tied to a specific user) are
    /// always allowed; otherwise the requested user must match the authenticated user.
    /// </summary>
    /// <param name="requestedUserId">The userId supplied in the request query string.</param>
    /// <param name="authenticatedUserId">The userId resolved from the request's authorization info.</param>
    /// <param name="isApiKey">Whether the request was authenticated via a server API key.</param>
    /// <param name="isAdministrator">Whether the authenticated user is an administrator.</param>
    /// <returns>True if the request is authorized for the requested user.</returns>
    internal static bool IsAuthorizedForUser(
        Guid requestedUserId,
        Guid authenticatedUserId,
        bool isApiKey,
        bool isAdministrator)
    {
        if (isApiKey || isAdministrator)
        {
            return true;
        }

        return requestedUserId != Guid.Empty && requestedUserId == authenticatedUserId;
    }
}
