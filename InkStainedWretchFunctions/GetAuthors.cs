using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretchFunctions;

public class GetAuthors
{
    private readonly ILogger<GetAuthors> _logger;
    private readonly InkStainedWretch.OnePageAuthorAPI.API.IAuthorDataService _authorDataService;
    private readonly IJwtValidationService _jwtValidationService;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IScopeValidationService _scopeValidationService;

    public GetAuthors(
        ILogger<GetAuthors> logger, 
        InkStainedWretch.OnePageAuthorAPI.API.IAuthorDataService authorDataService,
        IJwtValidationService jwtValidationService,
        IUserIdentityService userIdentityService,
        IScopeValidationService scopeValidationService)
    {
        _logger = logger;
        _authorDataService = authorDataService;
        _jwtValidationService = jwtValidationService;
        _userIdentityService = userIdentityService;
        _scopeValidationService = scopeValidationService;
    }

    [Function("GetAuthors")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authors/{secondLevelDomain?}/{topLevelDomain?}")] HttpRequest req,
        string? secondLevelDomain,
        string? topLevelDomain)
    {
        bool hasDomainParams = !string.IsNullOrWhiteSpace(secondLevelDomain) && !string.IsNullOrWhiteSpace(topLevelDomain);

        // Scenario 2: Domain parameters present — return authors by domain, no authentication required.
        if (hasDomainParams)
        {
            try
            {
                _logger.LogInformation("Received request for authors with TLD: {TopLevelDomain}, SLD: {SecondLevelDomain}", topLevelDomain, secondLevelDomain);
                var domainAuthors = await _authorDataService.GetAuthorsByDomainAsync(topLevelDomain!, secondLevelDomain!);
                if (domainAuthors == null || !domainAuthors.Any())
                {
                    _logger.LogInformation("No authors found");
                    return new NotFoundObjectResult(new { error = "No authors found" });
                }
                _logger.LogInformation("Successfully retrieved {Count} author(s)", domainAuthors.Count);
                return new OkObjectResult(domainAuthors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving authors");
                return new ObjectResult(new { error = "Internal server error" })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        // No domain parameters — authentication is required.
        var (user, errorResult) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
        if (errorResult != null)
        {
            return errorResult;
        }

        try
        {
            List<InkStainedWretch.OnePageAuthorAPI.API.AuthorApiResponse> authors;

            if (_scopeValidationService.HasRequiredScope(user!, "Author.Read"))
            {
                // Scenario 1: Authenticated with Author.Read scope — return all authors with paging.
                int page = 1;
                if (req.Query.TryGetValue("page", out var pageStr) &&
                    int.TryParse(pageStr, out int parsedPage) &&
                    parsedPage > 0)
                {
                    page = parsedPage;
                }
                _logger.LogInformation("Received request for all authors (page {Page}) with Author.Read scope", page);
                authors = await _authorDataService.GetAllAuthorsPagedAsync(page);
            }
            else
            {
                // Scenario 3: Authenticated without Author.Read scope — return authors matching the user's email.
                var email = _userIdentityService.GetUserUpn(user!);
                _logger.LogInformation("Received request for all authors for user: {Email}", email);
                authors = await _authorDataService.GetAuthorsByEmailAsync(email);
            }

            if (authors == null || !authors.Any())
            {
                _logger.LogInformation("No authors found");
                return new NotFoundObjectResult(new { error = "No authors found" });
            }

            _logger.LogInformation("Successfully retrieved {Count} author(s)", authors.Count);
            return new OkObjectResult(authors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving authors");
            return new ObjectResult(new { error = "Internal server error" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}