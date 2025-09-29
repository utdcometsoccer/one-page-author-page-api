using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.API;
using System.Security.Claims;

namespace InkStainedWretchStripe;

public class GetStripePriceInformation
{
    private readonly ILogger<GetStripePriceInformation> _logger;
    private readonly IPriceServiceWrapper _priceService;
    private readonly IJwtValidationService _jwtValidationService;
    private readonly IUserProfileService _userProfileService;

    public GetStripePriceInformation(ILogger<GetStripePriceInformation> logger, IPriceServiceWrapper priceService, IJwtValidationService jwtValidationService, IUserProfileService userProfileService)
    {
        _logger = logger;
        _priceService = priceService;
        _jwtValidationService = jwtValidationService;
        _userProfileService = userProfileService;
    }

    [Function("GetStripePriceInformation")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [FromBody] PriceListRequest request)
    {
        _logger.LogInformation("Processing POST request to get Stripe price information");

        // Validate JWT token and get authenticated user
        var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
        if (authError != null)
        {
            return authError;
        }

        try
        {
            // Ensure user profile exists
            await _userProfileService.EnsureUserProfileAsync(authenticatedUser!);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User profile validation failed for GetStripePriceInformation");
            return new UnauthorizedObjectResult(new { error = "User profile validation failed" });
        }

        try
        {
            if (request is null)
            {
                return new BadRequestObjectResult(new { error = "Invalid request payload" });
            }

            var result = await _priceService.GetPricesAsync(request);
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe price information request");
            return new ObjectResult(new { error = "An error occurred processing your request" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
