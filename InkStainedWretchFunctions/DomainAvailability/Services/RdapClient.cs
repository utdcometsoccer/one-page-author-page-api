using InkStainedWretch.OnePageAuthorAPI.Functions.DomainAvailability.Models;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Functions.DomainAvailability.Services;

/// <summary>
/// Implements domain availability checks by querying <c>rdap.org</c>.
/// </summary>
/// <remarks>
/// The RDAP (Registration Data Access Protocol) service at <c>rdap.org</c> acts as a universal
/// bootstrap proxy. An HTTP 200 response means the domain is registered; an HTTP 404 means the
/// domain is available. Any other status code is treated as a lookup failure.
/// </remarks>
public class RdapClient : IRdapClient
{
    private const string RdapSource = "rdap.org";
    private const string RdapBaseUrl = "https://rdap.org/domain/";

    private readonly HttpClient _httpClient;
    private readonly ILogger<RdapClient> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RdapClient"/>.
    /// </summary>
    /// <param name="httpClient">Named <see cref="HttpClient"/> injected by <see cref="IHttpClientFactory"/>.</param>
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
        var normalizedDomain = domain.ToLowerInvariant().Trim();
        var requestUrl = $"{RdapBaseUrl}{Uri.EscapeDataString(normalizedDomain)}";

        _logger.LogInformation("Querying RDAP for domain {Domain} at {Url}", normalizedDomain, requestUrl);

        using var response = await _httpClient.GetAsync(requestUrl, cancellationToken).ConfigureAwait(false);

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
                RdapSource = RdapSource
            },
            404 => new DomainAvailabilityResponse
            {
                Domain = normalizedDomain,
                Available = true,
                CheckedAt = DateTime.UtcNow,
                RdapStatus = statusCode,
                RdapSource = RdapSource
            },
            _ => throw new HttpRequestException(
                $"RDAP service returned an unexpected HTTP {statusCode} status for domain '{normalizedDomain}'.",
                null,
                response.StatusCode)
        };
    }
}
