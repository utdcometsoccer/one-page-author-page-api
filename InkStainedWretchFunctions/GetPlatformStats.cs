using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using System.Net;

namespace InkStainedWretchFunctions;

/// <summary>
/// Azure Function HTTP endpoint that returns aggregated platform statistics.
/// This endpoint is public (no authentication required) and provides social proof
/// for the landing page. Response is cached for 1 hour to reduce database load.
/// </summary>
public class GetPlatformStats
{
    private readonly ILogger<GetPlatformStats> _logger;
    private readonly IPlatformStatsService _platformStatsService;

    /// <summary>
    /// Creates a new GetPlatformStats function handler.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="platformStatsService">Platform statistics service.</param>
    public GetPlatformStats(
        ILogger<GetPlatformStats> logger,
        IPlatformStatsService platformStatsService)
    {
        _logger = logger;
        _platformStatsService = platformStatsService;
    }

    /// <summary>
    /// Handles HTTP GET requests for platform statistics.
    /// </summary>
    /// <param name="req">The incoming HTTP request.</param>
    /// <returns>200 with JSON payload of platform stats.</returns>
    [Function("GetPlatformStats")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "stats/platform")] HttpRequestData req)
    {
        _logger.LogInformation("Received request for platform statistics");
        
        // Service handles errors gracefully and never throws
        var stats = await _platformStatsService.GetPlatformStatsAsync();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        
        // Add cache control header to enable browser caching for 1 hour
        response.Headers.Add("Cache-Control", "public, max-age=3600");
        
        await response.WriteAsJsonAsync(new
        {
            activeAuthors = stats.ActiveAuthors,
            booksPublished = stats.BooksPublished,
            totalRevenue = stats.TotalRevenue,
            averageRating = stats.AverageRating,
            countriesServed = stats.CountriesServed,
            lastUpdated = stats.LastUpdated
        });
        
        _logger.LogInformation("Successfully returned platform statistics");
        return response;
    }
}
