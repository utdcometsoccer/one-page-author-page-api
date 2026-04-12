namespace InkStainedWretch.OnePageAuthorLib.Models;

/// <summary>
/// Represents the result of a domain availability check via RDAP.
/// </summary>
public class DomainAvailabilityResponse
{
    /// <summary>
    /// The domain name that was checked (normalized to lowercase).
    /// </summary>
    public required string Domain { get; init; }

    /// <summary>
    /// <c>true</c> if the domain is available for registration; <c>false</c> if it is already registered.
    /// </summary>
    public bool Available { get; init; }

    /// <summary>
    /// UTC timestamp of when the check was performed.
    /// </summary>
    public DateTime CheckedAt { get; init; }

    /// <summary>
    /// The HTTP status code returned by the RDAP service (200 = registered, 404 = available).
    /// </summary>
    public int RdapStatus { get; init; }

    /// <summary>
    /// The RDAP source host used to perform the lookup.
    /// </summary>
    public required string RdapSource { get; init; }
}
