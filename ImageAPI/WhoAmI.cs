using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Authentication;

namespace ImageAPI;

public class WhoAmI
{
    private readonly ILogger<WhoAmI> _logger;
    private readonly IJwtValidationService _jwtValidationService;
    private readonly IUserProfileService _userProfileService;

    public WhoAmI(ILogger<WhoAmI> logger, IJwtValidationService jwtValidationService, IUserProfileService userProfileService)
    {
        _logger = logger;
        _jwtValidationService = jwtValidationService;
        _userProfileService = userProfileService;
    }

    [Function("WhoAmI")]
    [Authorize(Policy = "RequireScope.Read")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        // Validate JWT token and get authenticated user
        var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
        if (authError != null)
        {
            return authError;
        }

        try
        {
            // Ensure user profile exists (optional for WhoAmI, just for consistency)
            await _userProfileService.EnsureUserProfileAsync(authenticatedUser!);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User profile validation failed for WhoAmI - continuing anyway");
            // Don't return error for WhoAmI, just log and continue
        }

        var user = authenticatedUser!;

        string GetClaim(params string[] types)
            => types.Select(t => user.FindFirst(t)?.Value).FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? string.Empty;

        var name = GetClaim("name", "given_name");
        var preferredUsername = GetClaim("preferred_username", "upn");
        var subject = GetClaim("sub", "oid");
        var tenantId = GetClaim("tid");
        var roles = user.FindAll("roles").Select(c => c.Value).ToArray();
        var scopes = (user.FindFirst("scp")?.Value ?? string.Empty)
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);

        var claims = user.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Value).Distinct().ToArray());

        var result = new
        {
            name,
            preferredUsername,
            subject,
            tenantId,
            roles,
            scopes,
            claims
        };

        _logger.LogInformation("WhoAmI requested for subject {Subject}", subject);
        return new OkObjectResult(result);
    }
}
