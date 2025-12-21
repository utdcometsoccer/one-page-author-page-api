using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InkStainedWretchFunctions;

/// <summary>
/// Azure Function HTTP endpoints for referral program management.
/// Supports creating referrals and retrieving referral statistics.
/// </summary>
public class ReferralFunction
{
    private readonly ILogger<ReferralFunction> _logger;
    private readonly IReferralService _referralService;

    /// <summary>
    /// Creates a new <see cref="ReferralFunction"/> handler.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="referralService">Referral service for business logic.</param>
    public ReferralFunction(ILogger<ReferralFunction> logger, IReferralService referralService)
    {
        _logger = logger;
        _referralService = referralService;
    }

    /// <summary>
    /// POST /api/referrals - Creates a new referral
    /// </summary>
    /// <remarks>
    /// Example request:
    /// POST /api/referrals
    /// {
    ///   "referrerId": "user-123",
    ///   "referredEmail": "friend@example.com"
    /// }
    /// 
    /// Example response:
    /// {
    ///   "referralCode": "ABC12345",
    ///   "referralUrl": "https://inkstainedwretches.com/signup?ref=ABC12345"
    /// }
    /// </remarks>
    [Function("CreateReferral")]
    public async Task<HttpResponseData> CreateReferral(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "referrals")] HttpRequestData req)
    {
        _logger.LogInformation("Processing CreateReferral request");

        try
        {
            // Parse request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<CreateReferralRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null)
            {
                _logger.LogWarning("Invalid request body");
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request body");
                return badResponse;
            }

            // Create the referral
            var result = await _referralService.CreateReferralAsync(request);

            // Return success response
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in CreateReferral");
            var response = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await response.WriteStringAsync(ex.Message);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business logic error in CreateReferral");
            var response = req.CreateResponse(System.Net.HttpStatusCode.Conflict);
            await response.WriteStringAsync(ex.Message);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating referral");
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("An error occurred while creating the referral");
            return response;
        }
    }

    /// <summary>
    /// GET /api/referrals/{userId} - Gets referral statistics for a user
    /// </summary>
    /// <remarks>
    /// Example request:
    /// GET /api/referrals/user-123
    /// 
    /// Example response:
    /// {
    ///   "totalReferrals": 10,
    ///   "successfulReferrals": 3,
    ///   "pendingCredits": 3,
    ///   "redeemedCredits": 0
    /// }
    /// </remarks>
    [Function("GetReferralStats")]
    public async Task<HttpResponseData> GetReferralStats(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "referrals/{userId}")] HttpRequestData req,
        string userId)
    {
        _logger.LogInformation("Processing GetReferralStats request for userId: {UserId}", userId);

        try
        {
            // Get referral statistics
            var stats = await _referralService.GetReferralStatsAsync(userId);

            // Return success response
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(stats);
            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in GetReferralStats");
            var response = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await response.WriteStringAsync(ex.Message);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting referral stats for userId: {UserId}", userId);
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("An error occurred while retrieving referral statistics");
            return response;
        }
    }
}
