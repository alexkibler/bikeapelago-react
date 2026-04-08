using Microsoft.AspNetCore.Authorization;

namespace Bikeapelago.Api.Authorization;

/// <summary>
/// Restricts access to users with the "Admin" role via the "AdminOnly" authorization policy.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AdminAuthorizeAttribute : AuthorizeAttribute
{
    public AdminAuthorizeAttribute() : base("AdminOnly")
    {
    }
}
