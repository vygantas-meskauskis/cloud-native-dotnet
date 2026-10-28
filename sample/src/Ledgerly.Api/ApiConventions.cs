using System.Security.Claims;

namespace Ledgerly.Api;

public static class ApiPolicies
{
    public const string Imports = "imports";
    public const string Spa = "spa";
}

public static class Features
{
    public const string PriceAlerts = "PriceAlerts";
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Stable user id. Entra ID puts the object id in "oid"; dev tokens from `dotnet user-jwts` use "sub"
    /// (mapped to NameIdentifier by the JWT handler).
    /// </summary>
    public static string GetOwnerId(this ClaimsPrincipal user) =>
        user.FindFirstValue("oid")
        ?? user.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub")
        ?? throw new InvalidOperationException("Authenticated user has no id claim.");
}
