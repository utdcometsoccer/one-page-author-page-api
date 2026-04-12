using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.Services;
using InkStainedWretch.OnePageAuthorLib.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Functions;

/// <summary>
/// Azure Function that checks whether a domain name is available for registration using RDAP.
/// </summary>
public class CheckDomainAvailability
{
    private readonly ILogger<CheckDomainAvailability> _logger;
    private readonly IRdapClient _rdapClient;

    /// <summary>
    /// Initializes a new instance of <see cref="CheckDomainAvailability"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="rdapClient">RDAP client used to query domain registration status.</param>
    public CheckDomainAvailability(
        ILogger<CheckDomainAvailability> logger,
        IRdapClient rdapClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rdapClient = rdapClient ?? throw new ArgumentNullException(nameof(rdapClient));
    }

    /// <summary>
    /// Checks whether the specified domain is available for registration.
    /// </summary>
    /// <param name="req">
    /// HTTP GET request. Must include a <c>domain</c> query parameter containing the root domain to check
    /// (e.g., <c>?domain=example.com</c>).
    /// </param>
    /// <returns>
    /// <list type="table">
    /// <item>
    ///   <term>200 OK</term>
    ///   <description>
    ///   Returns a <see cref="DomainAvailabilityResponse"/> with <c>available</c> set to
    ///   <c>true</c> (not yet registered) or <c>false</c> (already registered).
    ///   </description>
    /// </item>
    /// <item>
    ///   <term>400 Bad Request</term>
    ///   <description>The <c>domain</c> query parameter is missing or fails validation.</description>
    /// </item>
    /// <item>
    ///   <term>502 Bad Gateway</term>
    ///   <description>The RDAP service returned an unexpected response.</description>
    /// </item>
    /// </list>
    /// </returns>
    [Function("CheckDomainAvailability")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "domain-availability")] HttpRequest req)
    {
        var domain = req.Query["domain"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(domain))
        {
            _logger.LogWarning("CheckDomainAvailability called without a domain query parameter.");
            return new BadRequestObjectResult(new DomainAvailabilityErrorResponse
            {
                Error = "InvalidDomain",
                Message = "The 'domain' query parameter is required."
            });
        }

        if (!DomainAvailabilityValidator.IsValid(domain, out var validationError))
        {
            _logger.LogWarning("Domain validation failed for '{Domain}': {Error}", domain, validationError);
            return new BadRequestObjectResult(new DomainAvailabilityErrorResponse
            {
                Error = "InvalidDomain",
                Message = validationError ?? "The domain format is invalid."
            });
        }

        try
        {
            var result = await _rdapClient.CheckAvailabilityAsync(domain, req.HttpContext.RequestAborted)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Domain availability check complete: {Domain} available={Available}",
                result.Domain,
                result.Available);

            return new OkObjectResult(result);
        }
        catch (OperationCanceledException) when (req.HttpContext.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected before the RDAP call completed — not an RDAP error.
            _logger.LogInformation("Domain availability check cancelled by client for domain '{Domain}'.", domain);
            return new StatusCodeResult(StatusCodes.Status499ClientClosedRequest);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "RDAP lookup failed for domain '{Domain}'.", domain);
            return new ObjectResult(new DomainAvailabilityErrorResponse
            {
                Error = "RdapLookupFailed",
                Message = "The RDAP service returned an unexpected response."
            })
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "RDAP lookup timed out for domain '{Domain}'.", domain);
            return new ObjectResult(new DomainAvailabilityErrorResponse
            {
                Error = "RdapLookupFailed",
                Message = "The RDAP service did not respond in time."
            })
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }
    }
}
