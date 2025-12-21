using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InkStainedWretchFunctions;

/// <summary>
/// Azure Function HTTP endpoint for A/B testing experiment configuration.
/// Returns experiment assignments for users/sessions with consistent variant bucketing.
/// </summary>
public class GetExperiments
{
    private readonly ILogger<GetExperiments> _logger;
    private readonly IExperimentService _experimentService;

    /// <summary>
    /// Creates a new <see cref="GetExperiments"/> function handler.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="experimentService">Experiment service for variant assignment.</param>
    public GetExperiments(
        ILogger<GetExperiments> logger,
        IExperimentService experimentService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _experimentService = experimentService ?? throw new ArgumentNullException(nameof(experimentService));
    }

    /// <summary>
    /// Handles HTTP GET requests for experiment assignments.
    /// </summary>
    /// <param name="req">The incoming HTTP request.</param>
    /// <returns>200 with experiment assignments; 400 if request is invalid; 500 on internal errors.</returns>
    [Function("GetExperiments")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "experiments")] HttpRequest req)
    {
        _logger.LogInformation("Received request for experiment assignments");

        try
        {
            // Parse query parameters
            var userId = req.Query["userId"].FirstOrDefault();
            var page = req.Query["page"].FirstOrDefault();

            // Validate required parameters
            if (string.IsNullOrWhiteSpace(page))
            {
                _logger.LogWarning("Missing required 'page' parameter");
                return new BadRequestObjectResult(new
                {
                    error = "Missing required parameter: 'page'",
                    message = "Please provide a 'page' query parameter (e.g., 'landing', 'pricing')"
                });
            }

            // Create request object
            var request = new GetExperimentsRequest
            {
                UserId = userId,
                Page = page
            };

            _logger.LogInformation("Getting experiments for page: {Page}, userId: {UserId}", 
                page, userId ?? "(none)");

            // Get experiment assignments
            var response = await _experimentService.GetExperimentsAsync(request);

            _logger.LogInformation("Successfully assigned {Count} experiments for session: {SessionId}", 
                response.Experiments.Count, response.SessionId);

            return new OkObjectResult(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters");
            return new BadRequestObjectResult(new
            {
                error = "Invalid request",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing experiment assignment request");
            return new ObjectResult(new
            {
                error = "Internal server error",
                message = "An error occurred while processing your request"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
