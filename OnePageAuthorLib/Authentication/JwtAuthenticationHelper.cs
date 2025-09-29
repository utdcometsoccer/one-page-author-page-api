using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace InkStainedWretch.OnePageAuthorAPI.Authentication;

/// <summary>
/// Helper class for JWT authentication in Azure Functions
/// </summary>
public static class JwtAuthenticationHelper
{
    /// <summary>
    /// Validates JWT token from Authorization header and returns authenticated user
    /// </summary>
    /// <param name="request">HTTP request containing Authorization header</param>
    /// <param name="jwtValidationService">JWT validation service</param>
    /// <param name="logger">Logger for authentication events</param>
    /// <returns>Authenticated user principal if valid, or error result if invalid/missing</returns>
    public static async Task<(ClaimsPrincipal? user, IActionResult? errorResult)> ValidateJwtTokenAsync(
        HttpRequest request,
        IJwtValidationService jwtValidationService,
        ILogger logger)
    {
        try
        {
            if (!request.Headers.ContainsKey("Authorization"))
            {
                logger.LogWarning("No Authorization header provided");
                return (null, new UnauthorizedObjectResult(new { error = "Authorization header is required" }));
            }

            var authHeader = request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader))
            {
                logger.LogWarning("Authorization header is empty");
                return (null, new UnauthorizedObjectResult(new { error = "Authorization header is empty" }));
            }

            if (!authHeader.StartsWith("Bearer "))
            {
                logger.LogWarning("Invalid Authorization header format. Header: {HeaderPreview}",
                    authHeader.Length > 20 ? $"{authHeader[..20]}..." : authHeader);
                return (null, new UnauthorizedObjectResult(new { error = "Authorization header must start with 'Bearer '" }));
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            // Log token info for debugging (without exposing the actual token)
            logger.LogDebug("Extracted token - Length: {TokenLength}, Segments: {SegmentCount}",
                token.Length, token.Split('.').Length);

            if (string.IsNullOrWhiteSpace(token))
            {
                logger.LogWarning("Token is empty after Bearer prefix removal");
                return (null, new UnauthorizedObjectResult(new { error = "Token is empty" }));
            }
            var authenticatedUser = await jwtValidationService.ValidateTokenAsync(token);

            if (authenticatedUser == null)
            {
                logger.LogWarning("Invalid JWT token provided");
                return (null, new UnauthorizedObjectResult(new { error = "Invalid or expired token" }));
            }

            logger.LogInformation("User authenticated successfully: {UserId}",
                authenticatedUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown");

            return (authenticatedUser, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during JWT token validation");
            return (null, new ObjectResult(new { error = "Authentication error" })
            { StatusCode = StatusCodes.Status500InternalServerError });
        }
    }
}