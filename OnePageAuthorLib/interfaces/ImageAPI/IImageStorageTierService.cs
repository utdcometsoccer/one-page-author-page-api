using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;

namespace InkStainedWretch.OnePageAuthorAPI.API.ImageAPI
{
    /// <summary>
    /// Service for determining user's image storage tier from Entra ID roles.
    /// </summary>
    public interface IImageStorageTierService
    {
        /// <summary>
        /// Gets the user's image storage tier from their JWT claims.
        /// If the user has no tier role, returns the default tier (Starter or lowest cost tier).
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <returns>The user's image storage tier</returns>
        Task<ImageStorageTier?> GetUserTierAsync(ClaimsPrincipal user);
        
        /// <summary>
        /// Gets the user's image storage tier from their JWT claims.
        /// If the user has no tier role, returns the default tier (Starter or lowest cost tier).
        /// </summary>
        /// <param name="userProfileId">The user's profile ID (OID)</param>
        /// <param name="roles">The user's role claims</param>
        /// <returns>The user's image storage tier</returns>
        Task<ImageStorageTier?> GetUserTierByRolesAsync(string userProfileId, string[] roles);
    }
}
