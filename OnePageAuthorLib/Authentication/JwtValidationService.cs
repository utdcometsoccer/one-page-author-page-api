using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace InkStainedWretch.OnePageAuthorAPI.Authentication;

public interface IJwtValidationService
{
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
}

public class JwtValidationService : IJwtValidationService, IDisposable
{
    private readonly ILogger<JwtValidationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly ITokenIntrospectionService _tokenIntrospectionService;
    private ConfigurationManager<OpenIdConnectConfiguration>? _configurationManager;
    private readonly SemaphoreSlim _configurationManagerLock = new(1, 1);
    private bool _disposed;

    public JwtValidationService(ILogger<JwtValidationService> logger, IConfiguration configuration, ITokenIntrospectionService tokenIntrospectionService)
    {
        _logger = logger;
        _configuration = configuration;
        _tokenHandler = new JwtSecurityTokenHandler();
        _tokenIntrospectionService = tokenIntrospectionService;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _configurationManagerLock.Dispose();
            _disposed = true;
        }
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

    private async Task<ConfigurationManager<OpenIdConnectConfiguration>> GetOrCreateConfigurationManagerAsync()
    {
        if (_configurationManager != null)
        {
            return _configurationManager;
        }

        await _configurationManagerLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            // This is the double-check locking pattern - another thread may have initialized
            // _configurationManager between the first check and acquiring the lock
#pragma warning disable CS0472 // The result of the expression is always the same - this is intentional for double-check locking
            if (_configurationManager != null)
#pragma warning restore CS0472
            {
                return _configurationManager;
            }

            var tenantId = _configuration["AAD_TENANT_ID"];
            var openIdMetadataUrl = _configuration["OPEN_ID_CONNECT_METADATA_URL"] ??
                                    (!string.IsNullOrWhiteSpace(tenantId)
                                        ? $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration"
                                        : null);

            if (string.IsNullOrWhiteSpace(openIdMetadataUrl))
            {
                throw new InvalidOperationException("OpenID Connect metadata URL is not configured. Set AAD_TENANT_ID or OPEN_ID_CONNECT_METADATA_URL.");
            }

            _logger.LogInformation("Initializing ConfigurationManager with metadata URL: {MetadataUrl}", 
                Utility.MaskUrl(openIdMetadataUrl));

            // Create configuration manager with automatic refresh settings
            // This matches the configuration in Program.cs files to prevent key rotation issues
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                openIdMetadataUrl,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever())
            {
                // Refresh metadata every 6 hours (default is 24 hours)
                // This proactively updates signing keys before they become stale
                AutomaticRefreshInterval = TimeSpan.FromHours(6),
                // Minimum time between refreshes to prevent hammering the endpoint
                // This rate limits metadata refreshes during key rotation events
                RefreshInterval = TimeSpan.FromMinutes(30)
            };

            return _configurationManager;
        }
        finally
        {
            _configurationManagerLock.Release();
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

        // Get or create the singleton configuration manager with automatic refresh
        var configurationManager = await GetOrCreateConfigurationManagerAsync();

        // Allow multiple issuers via comma-delimited env var AAD_VALID_ISSUERS
        var validIssuersRaw = _configuration["AAD_VALID_ISSUERS"];
        string[]? validIssuers = Utility.ParseValidIssuers(validIssuersRaw);

        // Local helper to construct TokenValidationParameters from the given configuration
        TokenValidationParameters CreateTokenValidationParameters(OpenIdConnectConfiguration config)
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                // If multiple issuers provided, use ValidIssuers; otherwise fall back to single ValidIssuer
                ValidIssuer = validIssuers is null ? authority.TrimEnd('/') : null,
                ValidIssuers = validIssuers,
                ValidAudiences = new[] { audience },
                IssuerSigningKeys = config.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(5)
            };
        }

        // Try validation with current configuration
        try
        {
            var openIdConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);
            var validationParameters = CreateTokenValidationParameters(openIdConfig);

            _logger.LogDebug("Attempting to validate JWT token with {SegmentCount} segments", 3);
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            _logger.LogInformation("JWT token validated successfully for user: {UserId}",
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown");

            return principal;
        }
        catch (SecurityTokenSignatureKeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Signing key not found in current configuration. Attempting to refresh metadata and retry validation.");
            
            // Force refresh the configuration to get latest signing keys
            // This handles the case where Azure AD has rotated keys
            // Note: Only one retry attempt is made. If the refreshed metadata still doesn't contain
            // the required key, the exception will bubble up and be caught by the outer exception handler.
            configurationManager.RequestRefresh();
            var refreshedConfig = await configurationManager.GetConfigurationAsync(CancellationToken.None);
            var validationParameters = CreateTokenValidationParameters(refreshedConfig);

            _logger.LogDebug("Retrying JWT token validation with refreshed signing keys");
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            _logger.LogInformation("JWT token validated successfully after metadata refresh for user: {UserId}",
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown");

            return principal;
        }
    }
}