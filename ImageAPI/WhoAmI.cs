using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Authentication;

namespace ImageAPI;

/// <summary>
/// Azure Function for retrieving information about the authenticated user.
/// Returns user identity and claims information from JWT token.
/// </summary>
/// <remarks>
/// <para><strong>HTTP Method:</strong> GET</para>
/// <para><strong>Route:</strong> /api/whoami</para>
/// <para><strong>Authentication:</strong> Required - Bearer JWT token</para>
/// <para><strong>Authorization:</strong> Any authenticated user</para>
/// 
/// <para><strong>TypeScript Example:</strong></para>
/// <code>
/// // Interface for user information response
/// interface UserInfo {
///   oid: string;           // Object ID (unique user identifier)
///   upn: string;          // User Principal Name (email)
///   name: string;         // Display name
///   given_name: string;   // First name
///   family_name: string;  // Last name
///   email: string;        // Email address
/// }
/// 
/// // Fetch current user information
/// const getCurrentUser = async (token: string): Promise&lt;UserInfo&gt; => {
///   const response = await fetch('/api/whoami', {
///     method: 'GET',
///     headers: {
///       'Authorization': `Bearer ${token}`,
///       'Content-Type': 'application/json'
///     }
///   });
///   
///   if (response.ok) {
///     return await response.json();
///   } else if (response.status === 401) {
///     throw new Error('Unauthorized - invalid or missing token');
///   }
///   
///   const error = await response.json();
///   throw new Error(error.error || 'Failed to fetch user info');
/// };
/// 
/// // Usage with React/TypeScript for user profile
/// const UserProfile: React.FC = () => {
///   const [user, setUser] = useState&lt;UserInfo | null&gt;(null);
///   const [loading, setLoading] = useState(true);
///   
///   useEffect(() => {
///     const loadUser = async () => {
///       try {
///         const userInfo = await getCurrentUser(userToken);
///         setUser(userInfo);
///       } catch (error) {
///         console.error('Failed to load user:', error);
///       } finally {
///         setLoading(false);
///       }
///     };
///     
///     loadUser();
///   }, [userToken]);
///   
///   if (loading) return &lt;div&gt;Loading user profile...&lt;/div&gt;;
///   if (!user) return &lt;div&gt;Failed to load user profile&lt;/div&gt;;
///   
///   return (
///     &lt;div className="user-profile"&gt;
///       &lt;h2&gt;Welcome, {user.name}&lt;/h2&gt;
///       &lt;p&gt;Email: {user.email}&lt;/p&gt;
///       &lt;p&gt;User ID: {user.oid}&lt;/p&gt;
///     &lt;/div&gt;
///   );
/// };
/// </code>
/// 
/// <para><strong>Response Format:</strong></para>
/// <code>
/// {
///   "oid": "12345678-1234-1234-1234-123456789abc",
///   "upn": "user@example.com",
///   "name": "John Doe",
///   "given_name": "John",
///   "family_name": "Doe",
///   "email": "user@example.com"
/// }
/// </code>
/// 
/// <para><strong>Response Codes:</strong></para>
/// <list type="bullet">
/// <item>200 OK: Returns user information extracted from JWT claims</item>
/// <item>401 Unauthorized: Missing or invalid JWT token</item>
/// <item>500 Internal Server Error: Unexpected server error</item>
/// </list>
/// 
/// <para><strong>Security Notes:</strong></para>
/// <list type="bullet">
/// <item>Information is extracted from JWT token claims</item>
/// <item>No database lookup required - claims are trusted</item>
/// <item>Useful for client-side user interface personalization</item>
/// <item>Does not expose sensitive information beyond what's in the token</item>
/// </list>
/// </remarks>
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

    /// <summary>
    /// Returns information about the authenticated user from JWT token claims.
    /// </summary>
    /// <param name="req">The HTTP request (no additional parameters required)</param>
    /// <returns>
    /// <list type="table">
    /// <item>
    /// <term>200 OK</term>
    /// <description>User information object with oid, upn, name, given_name, family_name, email</description>
    /// </item>
    /// <item>
    /// <term>401 Unauthorized</term>
    /// <description>Invalid or missing JWT token</description>
    /// </item>
    /// <item>
    /// <term>500 Internal Server Error</term>
    /// <description>Unexpected server error during processing</description>
    /// </item>
    /// </list>
    /// </returns>
    /// <example>
    /// This endpoint is commonly used for:
    /// - Displaying user name in navigation
    /// - Personalizing the user interface
    /// - Validating token freshness
    /// - Getting user identifier for other API calls
    /// </example>
    [Function("WhoAmI")]
    [Authorize(Policy = "RequireScope.Read")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
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
