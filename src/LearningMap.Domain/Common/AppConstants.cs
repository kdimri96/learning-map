namespace LearningMap.Domain.Common;

/// <summary>Role names stored in Users.Role and put in the JWT "role" claim.</summary>
public static class AppRoles
{
    /// <summary>The seeded admin account.</summary>
    public const string Admin = "Admin";

    /// <summary>Default role for self-registered users. Currently has the same employee permissions as Admin.</summary>
    public const string Viewer = "Viewer";
}

/// <summary>
/// Claim names inside our JWT. We switch off ASP.NET's "inbound claim mapping"
/// (MapInboundClaims = false) so these short names arrive unchanged in User.Claims.
/// </summary>
public static class AppClaimTypes
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
}
