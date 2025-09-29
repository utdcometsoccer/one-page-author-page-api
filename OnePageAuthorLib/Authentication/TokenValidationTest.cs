using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Authentication;

/// <summary>
/// Simple test class to verify token validation logic without requiring a full test framework
/// </summary>
public class TokenValidationTest
{
    private readonly IJwtValidationService _jwtValidationService;
    private readonly ILogger<TokenValidationTest> _logger;

    public TokenValidationTest(IJwtValidationService jwtValidationService, ILogger<TokenValidationTest> logger)
    {
        _jwtValidationService = jwtValidationService;
        _logger = logger;
    }

    /// <summary>
    /// Test token format detection logic
    /// </summary>
    public async Task<bool> TestTokenFormatDetectionAsync()
    {
        try
        {
            // Test opaque token (1 segment)
            var opaqueToken = "abcdefghijklmnopqrstuvwxyz123456789";
            var result1 = await _jwtValidationService.ValidateTokenAsync(opaqueToken);
            _logger.LogInformation("Opaque token test - Result: {IsValid}", result1 != null);

            // Test invalid JWT (2 segments)
            var invalidJwt = "header.payload";
            var result2 = await _jwtValidationService.ValidateTokenAsync(invalidJwt);
            _logger.LogInformation("Invalid JWT token test - Result: {IsValid}", result2 != null);

            // Test potentially valid JWT format (3 segments) - will fail validation but should reach JWT validation logic
            var validFormatJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            var result3 = await _jwtValidationService.ValidateTokenAsync(validFormatJwt);
            _logger.LogInformation("Valid format JWT token test - Result: {IsValid}", result3 != null);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation test failed");
            return false;
        }
    }

    /// <summary>
    /// Test configuration validation
    /// </summary>
    public bool TestConfiguration(IConfiguration configuration)
    {
        var tenantId = configuration["AAD_TENANT_ID"];
        var audience = configuration["AAD_AUDIENCE"] ?? configuration["AAD_CLIENT_ID"];

        _logger.LogInformation("Configuration test - TenantId: {HasTenantId}, Audience: {HasAudience}",
            !string.IsNullOrWhiteSpace(tenantId),
            !string.IsNullOrWhiteSpace(audience));

        return !string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(audience);
    }
}