using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace InkStainedWretch.OnePageAuthorAPI.Authentication;

public interface ITokenIntrospectionService
{
    Task<ClaimsPrincipal?> IntrospectTokenAsync(string token);
}

public class TokenIntrospectionService : ITokenIntrospectionService
{
    private readonly ILogger<TokenIntrospectionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public TokenIntrospectionService(ILogger<TokenIntrospectionService> logger, IConfiguration configuration, HttpClient httpClient)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<ClaimsPrincipal?> IntrospectTokenAsync(string token)
    {
        try
        {
            // First, try to determine if this is a JWT or opaque token
            var tokenSegments = token.Split('.');

            if (tokenSegments.Length == 3)
            {
                _logger.LogDebug("Token appears to be JWT format, attempting JWT validation");
                // This is likely a JWT token - you could fall back to JWT validation here
                // For now, we'll continue with introspection as a universal approach
            }
            else if (tokenSegments.Length == 1)
            {
                _logger.LogDebug("Token appears to be opaque format, using introspection");
            }
            else
            {
                _logger.LogWarning("Token has unexpected format with {SegmentCount} segments", tokenSegments.Length);
                return null;
            }

            // Use Microsoft Graph API to validate the token
            return await ValidateTokenWithMicrosoftGraphAsync(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token introspection");
            return null;
        }
    }

    private async Task<ClaimsPrincipal?> ValidateTokenWithMicrosoftGraphAsync(string token)
    {
        try
        {
            // Use Microsoft Graph /me endpoint to validate token and get user info
            var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Microsoft Graph validation failed with status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<JsonElement>(content);

            // Extract user information and create claims
            var claims = new List<Claim>();

            if (userInfo.TryGetProperty("id", out var id))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, id.GetString() ?? ""));

            if (userInfo.TryGetProperty("userPrincipalName", out var upn))
                claims.Add(new Claim(ClaimTypes.Name, upn.GetString() ?? ""));

            if (userInfo.TryGetProperty("mail", out var email))
                claims.Add(new Claim(ClaimTypes.Email, email.GetString() ?? ""));

            if (userInfo.TryGetProperty("displayName", out var displayName))
                claims.Add(new Claim(ClaimTypes.GivenName, displayName.GetString() ?? ""));

            // Add the raw token as a claim for reference
            claims.Add(new Claim("access_token", token));

            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);

            _logger.LogInformation("Token validated successfully via Microsoft Graph for user: {UserId}",
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown");

            return principal;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error during Microsoft Graph validation: {Message}", ex.Message);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON parsing error during Microsoft Graph validation: {Message}", ex.Message);
            return null;
        }
    }
}