using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace InkStainedWretchStripe;

public interface IJwtValidationService
{
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
}

public class JwtValidationService : IJwtValidationService
{
    private readonly ILogger<JwtValidationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly ITokenIntrospectionService _tokenIntrospectionService;

    public JwtValidationService(ILogger<JwtValidationService> logger, IConfiguration configuration, ITokenIntrospectionService tokenIntrospectionService)
    {
        _logger = logger;
        _configuration = configuration;
        _tokenHandler = new JwtSecurityTokenHandler();
        _tokenIntrospectionService = tokenIntrospectionService;
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            // First, validate the token format before attempting to parse it
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("JWT validation failed: Token is null or empty");
                return null;
            }

            // Check if token has the correct number of segments (should be 3 for JWS)
            var tokenParts = token.Split('.');
            
            // Handle opaque tokens (1 segment)
            if (tokenParts.Length == 1)
            {
                _logger.LogInformation("Token appears to be opaque (1 segment), using introspection service");
                return await _tokenIntrospectionService.IntrospectTokenAsync(token);
            }
            
            // Handle JWT tokens (3 segments)
            if (tokenParts.Length != 3)
            {
                _logger.LogWarning("JWT validation failed: Token has {SegmentCount} segments, expected 3. Token preview: {TokenPreview}",
                    tokenParts.Length, 
                    token.Length > 50 ? $"{token[..25]}...{token[^25..]}" : token);
                return null;
            }

            // Validate each segment is base64url encoded (basic check)
            foreach (var part in tokenParts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    _logger.LogWarning("JWT validation failed: Token contains empty segment");
                    return null;
                }
            }

            // Continue with JWT validation for 3-segment tokens
            return await ValidateJwtTokenAsync(token);
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning(ex, "JWT token validation failed: {Message}", ex.Message);
            
            // Add detailed token analysis for debugging
            var tokenAnalysis = JwtDebugHelper.AnalyzeToken(token);
            _logger.LogDebug("Token Analysis:\n{TokenAnalysis}", tokenAnalysis);
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during JWT token validation");
            return null;
        }
    }

    private async Task<ClaimsPrincipal?> ValidateJwtTokenAsync(string token)
    {
        var tenantId = _configuration["AAD_TENANT_ID"];
        var audience = _configuration["AAD_AUDIENCE"] ?? _configuration["AAD_CLIENT_ID"];
        /*var authority = _configuration["AAD_AUTHORITY"] ?? 
                       (!string.IsNullOrWhiteSpace(tenantId) ? $"https://login.microsoftonline.com/{tenantId}/v2.0" : null);*/
        var openIdMetadataUrl = _configuration["OPEN_ID_CONNECT_METADATA_URL"] ??
                                (!string.IsNullOrWhiteSpace(tenantId)
                                    ? $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration"
                                    : null);
        var authority = openIdMetadataUrl is null ? "" : openIdMetadataUrl.Replace("/.well-known/openid-configuration", "");
        
        if (string.IsNullOrWhiteSpace(openIdMetadataUrl) || string.IsNullOrWhiteSpace(audience))
        {
            _logger.LogError("JWT validation failed: Authority or Audience not configured properly. OpenIdMetadataUrl: {OpenIdMetadataUrl}, Audience: {Audience}",
                openIdMetadataUrl ?? "null", audience ?? "null");
            return null;
        }

        // Get OpenID Connect configuration for token validation
        //var openIdConnectUrl = $"{authority.TrimEnd('/')}/.well-known/openid_connect_configuration";
        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            openIdMetadataUrl, 
            new OpenIdConnectConfigurationRetriever());
        
        var openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authority.TrimEnd('/'),
            ValidAudiences = new[] { audience },
            IssuerSigningKeys = openIdConfig.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        _logger.LogDebug("Attempting to validate JWT token with {SegmentCount} segments", 3);
        var principal = _tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
        
        _logger.LogInformation("JWT token validated successfully for user: {UserId}", 
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown");
        
        return principal;
    }
}