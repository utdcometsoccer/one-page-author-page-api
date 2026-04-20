using System.Text.Json;
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
/// Supports standard two-label root domains (e.g., <c>example.com</c>) as well as
/// three-label <c>.ng</c> second-level domains (e.g., <c>example.com.ng</c>).
/// </summary>
public class CheckDomainAvailability
{
    private readonly ILogger<CheckDomainAvailability> _logger;
    private readonly IRdapClient _rdapClient;
    private readonly ICiraRdapClient _ciraRdapClient;

    /// <summary>
    /// Initializes a new instance of <see cref="CheckDomainAvailability"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="rdapClient">RDAP client used to query domain registration status via the generic bootstrap proxy.</param>
    /// <param name="ciraRdapClient">RDAP client targeting CIRA's authoritative endpoint for <c>.CA</c> domain lookups.</param>
    public CheckDomainAvailability(
        ILogger<CheckDomainAvailability> logger,
        IRdapClient rdapClient,
        ICiraRdapClient ciraRdapClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rdapClient = rdapClient ?? throw new ArgumentNullException(nameof(rdapClient));
        _ciraRdapClient = ciraRdapClient ?? throw new ArgumentNullException(nameof(ciraRdapClient));
    }

    /// <summary>
    /// Checks whether the specified domain is available for registration.
    /// </summary>
    /// <param name="req">
    /// HTTP GET request. Must include a <c>domain</c> query parameter containing the root domain to check
    /// (e.g., <c>?domain=example.com</c> or <c>?domain=example.com.ng</c>).
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

        var normalizedDomain = domain.Trim().TrimEnd('.').ToLowerInvariant();

        try
        {
            // .CA domains are routed to CIRA's authoritative RDAP endpoint for more reliable lookups.
            var isCaDomain = normalizedDomain.EndsWith(".ca", StringComparison.OrdinalIgnoreCase);
            IRdapClient rdapClient = isCaDomain ? _ciraRdapClient : _rdapClient;

            if (isCaDomain)
                _logger.LogInformation("Domain '{Domain}' is a .CA domain — routing lookup to CIRA RDAP.", normalizedDomain);

            var result = await rdapClient.CheckAvailabilityAsync(normalizedDomain, req.HttpContext.RequestAborted)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Domain availability check complete: {Domain} available={Available}",
                SanitizeForLog(result.Domain),
                result.Available);

            return new OkObjectResult(result);
        }
        catch (OperationCanceledException) when (req.HttpContext.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected before the RDAP call completed — not an RDAP error.
            _logger.LogInformation("Domain availability check cancelled by client for domain '{Domain}'.", SanitizeForLog(normalizedDomain));
            return new StatusCodeResult(StatusCodes.Status499ClientClosedRequest);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "RDAP lookup failed for domain '{Domain}'.", SanitizeForLog(normalizedDomain));
            return new ObjectResult(new DomainAvailabilityErrorResponse
            {
                Error = "RdapLookupFailed",
                Message = "The RDAP service returned an unexpected response."
            })
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "RDAP response could not be parsed for domain '{Domain}'.", SanitizeForLog(normalizedDomain));
            return new ObjectResult(new DomainAvailabilityErrorResponse
            {
                Error = "RdapLookupFailed",
                Message = "The RDAP service returned an unexpected response."
            })
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }
        catch (OperationCanceledException ex) when (!req.HttpContext.RequestAborted.IsCancellationRequested)
        {
            // Not a client-initiated cancellation — the RDAP service timed out (after all retries).
            _logger.LogError(ex, "RDAP lookup timed out for domain '{Domain}'.", SanitizeForLog(normalizedDomain));
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

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Strips control characters from a value before it is written to a log entry,
    /// preventing log-forging attacks.
    /// </summary>
    private static string SanitizeForLog(string? value) =>
        value is null ? string.Empty : value.ReplaceLineEndings("_").Replace('\t', '_');
}
