using System.Security.Claims;

namespace Gocgun.Extensions;

/// <summary>Claim reads. The caller chooses the claim type.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Reads a claim value.</summary>
    /// <param name="principal">The principal. Null returns null.</param>
    /// <param name="claimType">Claim type, for example <c>sub</c>.</param>
    /// <returns>The value, or null.</returns>
    public static string? GetStringClaim(this ClaimsPrincipal? principal, string claimType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);
        return principal?.FindFirst(claimType)?.Value;
    }

    /// <summary>Reads a claim as a GUID.</summary>
    /// <param name="principal">The principal.</param>
    /// <param name="claimType">Claim type, for example <c>sub</c>.</param>
    /// <returns>The GUID, or null when the claim is missing or not a GUID.</returns>
    public static Guid? GetGuidClaim(this ClaimsPrincipal? principal, string claimType)
    {
        var value = principal.GetStringClaim(claimType);
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}
