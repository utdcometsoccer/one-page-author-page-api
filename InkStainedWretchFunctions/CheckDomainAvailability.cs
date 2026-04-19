using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.Services;
using InkStainedWretch.OnePageAuthorLib.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Functions;

/// <summary>
/// Azure Function that checks whether a domain name is available for registration.
/// Root domains with the <c>.ng</c> TLD (including second-level <c>.ng</c> domains such as
/// <c>example.com.ng</c>) are checked via the WHMCS API when it is configured; all other
/// domains use the RDAP service at <c>rdap.org</c>.
/// </summary>
public class CheckDomainAvailability
{
    private readonly ILogger<CheckDomainAvailability> _logger;
    private readonly IRdapClient _rdapClient;
    private readonly IWhmcsService _whmcsService;

    /// <summary>
    /// Initializes a new instance of <see cref="CheckDomainAvailability"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="rdapClient">RDAP client used to query domain registration status.</param>
    /// <param name="whmcsService">WHMCS client used to query .ng domain registration status.</param>
    public CheckDomainAvailability(
        ILogger<CheckDomainAvailability> logger,
        IRdapClient rdapClient,
        IWhmcsService whmcsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rdapClient = rdapClient ?? throw new ArgumentNullException(nameof(rdapClient));
        _whmcsService = whmcsService ?? throw new ArgumentNullException(nameof(whmcsService));
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
    ///   <description>The RDAP or WHMCS service returned an unexpected response.</description>
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

        // .ng TLD — route through WHMCS when configured; fall back to RDAP otherwise.
        if (IsNgDomain(domain))
        {
            if (_whmcsService.IsConfigured)
            {
                return await CheckNgDomainViaWhmcsAsync(domain, req).ConfigureAwait(false);
            }

            _logger.LogWarning(
                "WHMCS is not configured; falling back to RDAP for .ng domain '{Domain}'.", domain);
        }

        return await CheckDomainViaRdapAsync(domain, req).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns <c>true</c> when <paramref name="domain"/> ends with the <c>.ng</c> TLD
    /// (covers both <c>example.ng</c> and second-level domains such as <c>example.com.ng</c>).
    /// </summary>
    private static bool IsNgDomain(string domain) =>
        domain.Trim().TrimEnd('.').EndsWith(".ng", StringComparison.OrdinalIgnoreCase);

    private async Task<IActionResult> CheckNgDomainViaWhmcsAsync(string domain, HttpRequest req)
    {
        _logger.LogInformation(
            "Domain '{Domain}' uses .ng TLD — routing to WHMCS availability check.", domain);

        try
        {
            var isAvailable = await _whmcsService.CheckDomainAvailabilityAsync(domain)
                .ConfigureAwait(false);

            var normalizedDomain = domain.Trim().TrimEnd('.').ToLowerInvariant();
            _logger.LogInformation(
                "WHMCS .ng domain availability check complete: {Domain} available={Available}",
                normalizedDomain, isAvailable);

            return new OkObjectResult(new DomainAvailabilityResponse
            {
                Domain = normalizedDomain,
                Available = isAvailable,
                CheckedAt = DateTime.UtcNow,
                // Mirror RDAP status conventions: 404 = available (no record), 200 = registered.
                RdapStatus = isAvailable ? 404 : 200,
                RdapSource = "whmcs"
            });
        }
        catch (OperationCanceledException) when (req.HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "WHMCS .ng availability check cancelled by client for domain '{Domain}'.", domain);
            return new StatusCodeResult(StatusCodes.Status499ClientClosedRequest);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "WHMCS lookup failed for .ng domain '{Domain}'.", domain);
            return new ObjectResult(new DomainAvailabilityErrorResponse
            {
                Error = "WhmcsLookupFailed",
                Message = "The WHMCS service returned an unexpected response."
            })
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "WHMCS API error for .ng domain '{Domain}'.", domain);
            return new ObjectResult(new DomainAvailabilityErrorResponse
            {
                Error = "WhmcsLookupFailed",
                Message = "The WHMCS service was unable to complete the availability check."
            })
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }
    }

    private async Task<IActionResult> CheckDomainViaRdapAsync(string domain, HttpRequest req)
    {
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
        catch (OperationCanceledException ex) when (!req.HttpContext.RequestAborted.IsCancellationRequested)
        {
            // Not a client-initiated cancellation — the RDAP service timed out (after all retries).
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
