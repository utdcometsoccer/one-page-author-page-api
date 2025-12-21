using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace InkStainedWretchFunctions;

/// <summary>
/// Azure Function for lead capture API - public endpoint with rate limiting.
/// </summary>
public class LeadCaptureFunction
{
    private readonly ILogger<LeadCaptureFunction> _logger;
    private readonly ILeadService _leadService;
    private readonly IRateLimitService _rateLimitService;

    public LeadCaptureFunction(
        ILogger<LeadCaptureFunction> logger,
        ILeadService leadService,
        IRateLimitService rateLimitService)
    {
        _logger = logger;
        _leadService = leadService;
        _rateLimitService = rateLimitService;
    }

    /// <summary>
    /// Creates a new lead from landing page, blog, or other sources.
    /// Public endpoint with no authentication required.
    /// Rate limited to 10 requests per IP per minute.
    /// </summary>
    [Function("CreateLead")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leads")] HttpRequest req)
    {
        var ipAddress = GetClientIpAddress(req);
        _logger.LogInformation("Lead capture request received from IP: {IpAddress}", ipAddress ?? "unknown");

        try
        {
            // Check rate limit
            var isAllowed = await _rateLimitService.IsRequestAllowedAsync(ipAddress ?? "unknown", "leads");
            if (!isAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for IP: {IpAddress}", ipAddress);
                return new ObjectResult(new { error = "Rate limit exceeded. Please try again later." })
                {
                    StatusCode = StatusCodes.Status429TooManyRequests
                };
            }

            // Parse request body
            CreateLeadRequest? request;
            try
            {
                using var reader = new StreamReader(req.Body);
                var body = await reader.ReadToEndAsync();
                request = JsonSerializer.Deserialize<CreateLeadRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request == null)
                {
                    return new BadRequestObjectResult(new { error = "Invalid request body" });
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse request body");
                return new BadRequestObjectResult(new { error = "Invalid JSON format" });
            }

            // Validate request using data annotations
            var validationContext = new ValidationContext(request);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(v => v.ErrorMessage).ToList();
                _logger.LogWarning("Validation failed: {Errors}", string.Join(", ", errors));
                return new BadRequestObjectResult(new { error = "Validation failed", details = errors });
            }

            // Additional email format validation
            if (!_leadService.IsValidEmail(request.Email))
            {
                _logger.LogWarning("Invalid email format: {Email}", request.Email);
                return new BadRequestObjectResult(new { error = "Invalid email format" });
            }

            // Validate source
            if (!LeadSource.IsValid(request.Source))
            {
                _logger.LogWarning("Invalid source: {Source}", request.Source);
                return new BadRequestObjectResult(new 
                { 
                    error = "Invalid source", 
                    details = $"Source must be one of: {string.Join(", ", LeadSource.ValidSources)}" 
                });
            }

            // Record the request for rate limiting
            await _rateLimitService.RecordRequestAsync(ipAddress ?? "unknown", "leads");

            // Create or retrieve existing lead
            var response = await _leadService.CreateLeadAsync(request, ipAddress);

            _logger.LogInformation(
                "Lead {Status}: {LeadId} for email: {Email}",
                response.Status, response.Id, request.Email);

            var statusCode = response.Status == LeadCreationStatus.Created 
                ? StatusCodes.Status201Created 
                : StatusCodes.Status200OK;

            return new ObjectResult(response)
            {
                StatusCode = statusCode
            };
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in lead capture");
            return new BadRequestObjectResult(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing lead capture request");
            return new ObjectResult(new { error = "Internal server error" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    /// <summary>
    /// Extracts the client IP address from the HTTP request.
    /// Handles X-Forwarded-For header for proxied requests.
    /// </summary>
    private string? GetClientIpAddress(HttpRequest req)
    {
        // Check X-Forwarded-For header (common in Azure/proxied environments)
        if (req.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ips = forwardedFor.ToString().Split(',');
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Check X-Real-IP header
        if (req.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            return realIp.ToString();
        }

        // Fall back to remote IP address
        return req.HttpContext?.Connection?.RemoteIpAddress?.ToString();
    }
}
