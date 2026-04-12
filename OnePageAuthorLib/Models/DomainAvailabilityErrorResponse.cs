namespace InkStainedWretch.OnePageAuthorLib.Models;

/// <summary>
/// Represents a structured error response returned by the domain availability endpoint.
/// </summary>
public class DomainAvailabilityErrorResponse
{
    /// <summary>
    /// A short machine-readable error code (e.g., "InvalidDomain", "RdapLookupFailed").
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// A human-readable description of what went wrong.
    /// </summary>
    public required string Message { get; init; }
}
