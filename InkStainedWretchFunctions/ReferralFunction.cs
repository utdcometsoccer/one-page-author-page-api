using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretchFunctions.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
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
                return await req.CreateErrorResponseAsync(
                    HttpStatusCode.BadRequest,
                    "Invalid request body");
            }

            // Create the referral
            var result = await _referralService.CreateReferralAsync(request);

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            return await req.HandleExceptionAsync(ex, _logger);
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
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(stats);
            return response;
        }
        catch (Exception ex)
        {
            return await req.HandleExceptionAsync(ex, _logger);
        }
    }
}
