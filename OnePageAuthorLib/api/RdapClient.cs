using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorLib.Models;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.API;

/// <summary>
/// Implements domain availability checks by querying an RDAP provider.
/// </summary>
/// <remarks>
/// The RDAP (Registration Data Access Protocol) service acts as a universal bootstrap proxy.
/// An HTTP 200 response means the domain is registered; an HTTP 404 means the domain is available.
/// Any other status code is treated as a lookup failure.
/// The <see cref="HttpClient.BaseAddress"/> is configured at registration time (via
/// <c>AddHttpClient</c> in <c>ServiceFactory</c>); this class uses relative paths only.
/// <para>
/// When the RDAP service times out or returns a transient error on the first attempt, the lookup
/// is automatically retried once after a <see cref="RetryDelayMs"/>-millisecond delay.  If the
/// retry also fails the exception is propagated to the caller.
/// </para>
/// </remarks>
public class RdapClient : IRdapClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RdapClient> _logger;

    // Delay between the first failed attempt and the single retry.
    internal const int RetryDelayMs = 1000;

    /// <summary>
    /// Initializes a new instance of <see cref="RdapClient"/>.
    /// </summary>
    /// <param name="httpClient">Typed <see cref="HttpClient"/> injected by <see cref="IHttpClientFactory"/>.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public RdapClient(HttpClient httpClient, ILogger<RdapClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<DomainAvailabilityResponse> CheckAvailabilityAsync(
        string domain,
        CancellationToken cancellationToken = default)
    {
        // Trim whitespace and trailing dot (FQDN notation) so that inputs like "example.com."
        // are sent to RDAP as "example.com", consistent with DomainValidator normalization.
        var normalizedDomain = domain.ToLowerInvariant().Trim().TrimEnd('.');
        // Use a relative path; BaseAddress is configured via DI (defaults to https://rdap.org/).
        var requestUrl = $"domain/{Uri.EscapeDataString(normalizedDomain)}";

        // Derive the source label from the configured base address rather than hardcoding it.
        var rdapSource = _httpClient.BaseAddress?.Host ?? "rdap.org";

        _logger.LogInformation("Querying RDAP for domain {Domain} at {Url}", normalizedDomain, requestUrl);

        try
        {
            return await QueryRdapAsync(normalizedDomain, requestUrl, rdapSource, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Caller cancelled the operation — do not retry.
            throw;
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException)
        {
            // Timeout or transient network/HTTP error on the first attempt — retry once.
            _logger.LogWarning(ex,
                "RDAP lookup for domain {Domain} failed on first attempt; retrying in {DelayMs} ms.",
                normalizedDomain, RetryDelayMs);

            await Task.Delay(RetryDelayMs, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Retrying RDAP query for domain {Domain} at {Url}", normalizedDomain, requestUrl);
            return await QueryRdapAsync(normalizedDomain, requestUrl, rdapSource, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes a single HTTP request to the RDAP service and maps the response to a
    /// <see cref="DomainAvailabilityResponse"/>.
    /// </summary>
    private async Task<DomainAvailabilityResponse> QueryRdapAsync(
        string normalizedDomain,
        string requestUrl,
        string rdapSource,
        CancellationToken cancellationToken)
    {
        // ResponseHeadersRead avoids buffering the RDAP JSON body; only the status code is needed.
        using var response = await _httpClient.GetAsync(
            requestUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        var statusCode = (int)response.StatusCode;
        _logger.LogInformation("RDAP returned HTTP {StatusCode} for domain {Domain}", statusCode, normalizedDomain);

        return statusCode switch
        {
            200 => new DomainAvailabilityResponse
            {
                Domain = normalizedDomain,
                Available = false,
                CheckedAt = DateTime.UtcNow,
                RdapStatus = statusCode,
                RdapSource = rdapSource
            },
            404 => new DomainAvailabilityResponse
            {
                Domain = normalizedDomain,
                Available = true,
                CheckedAt = DateTime.UtcNow,
                RdapStatus = statusCode,
                RdapSource = rdapSource
            },
            _ => throw new HttpRequestException(
                $"RDAP service returned an unexpected HTTP {statusCode} status for domain '{normalizedDomain}'.",
                null,
                response.StatusCode)
        };
    }
}
