using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Authentication;

namespace InkStainedWretch.Function;

public class GetAuthors
{
    private readonly ILogger<GetAuthors> _logger;
    private readonly InkStainedWretch.OnePageAuthorAPI.API.IAuthorDataService _authorDataService;
    private readonly IJwtValidationService _jwtValidationService;

    public GetAuthors(
        ILogger<GetAuthors> logger, 
        InkStainedWretch.OnePageAuthorAPI.API.IAuthorDataService authorDataService,
        IJwtValidationService jwtValidationService)
    {
        _logger = logger;
        _authorDataService = authorDataService;
        _jwtValidationService = jwtValidationService;
    }

    [Function("GetAuthors")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authors/{secondLevelDomain}/{topLevelDomain}")] HttpRequest req,
        string secondLevelDomain,
        string topLevelDomain)
    {
        _logger.LogInformation($"Received request for authors with TLD: {topLevelDomain}, SLD: {secondLevelDomain}");

        // Authenticate the request using JWT token
        var (user, errorResult) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Check if user has the required scope for author reading
        if (user != null && !HasRequiredScope(user))
        {
            _logger.LogWarning("User does not have required Author.Read scope");
            return new ObjectResult(new { error = "Insufficient permissions" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        try
        {
            var authors = await _authorDataService.GetAuthorsByDomainAsync(topLevelDomain, secondLevelDomain);
            
            if (authors == null || !authors.Any())
            {
                _logger.LogInformation($"No authors found for TLD: {topLevelDomain}, SLD: {secondLevelDomain}");
                return new NotFoundObjectResult(new { error = "Domain not found" });
            }

            _logger.LogInformation($"Successfully retrieved {authors.Count} authors for TLD: {topLevelDomain}, SLD: {secondLevelDomain}");
            return new OkObjectResult(authors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving authors for TLD: {TopLevelDomain}, SLD: {SecondLevelDomain}", topLevelDomain, secondLevelDomain);
            return new ObjectResult(new { error = "Internal server error" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    private bool HasRequiredScope(System.Security.Claims.ClaimsPrincipal user)
    {
        // Check for the required scope: api://<your-api-client-id>/Author.Read
        // This will match the scope pattern mentioned in the issue
        var scopes = user.FindAll("scp")?.Select(c => c.Value) ?? new List<string>();
        var allScopes = string.Join(" ", scopes);
        
        // Look for Author.Read scope in the scp claim
        return allScopes.Contains("Author.Read");
    }
}