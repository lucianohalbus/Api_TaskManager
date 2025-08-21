// File: Extensions/ClaimsPrincipalExtensions.cs
using System.Security.Claims;

namespace Api_TaskManager.Extensions
{
    /// <summary>
    /// Extension methods for extracting information from ClaimsPrincipal (JWT claims).
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Get the UserId from JWT claims.
        /// Returns null if claim is missing or invalid.
        /// </summary>
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null) return null;

            return int.TryParse(userIdClaim.Value, out var id) ? id : null;
        }
    }
}
