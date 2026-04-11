using InkStainedWretch.OnePageAuthorAPI.Functions.DomainAvailability.Models;

namespace InkStainedWretch.OnePageAuthorAPI.Functions.DomainAvailability.Services;

/// <summary>
/// Provides domain availability lookups using the RDAP protocol.
/// </summary>
public interface IRdapClient
{
    /// <summary>
    /// Checks whether a domain is available for registration by querying the RDAP service.
    /// </summary>
    /// <param name="domain">The fully-qualified root domain to check (e.g., "example.com").</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// A <see cref="DomainAvailabilityResponse"/> indicating whether the domain is registered or available.
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the RDAP service returns an unexpected HTTP status code other than 200 or 404.
    /// </exception>
    Task<DomainAvailabilityResponse> CheckAvailabilityAsync(string domain, CancellationToken cancellationToken = default);
}
