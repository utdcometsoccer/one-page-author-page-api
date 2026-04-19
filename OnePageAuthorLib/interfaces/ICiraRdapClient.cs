namespace InkStainedWretch.OnePageAuthorAPI.Interfaces;

/// <summary>
/// Specialised RDAP client that queries the CIRA (Canadian Internet Registration Authority) RDAP service
/// directly for <c>.CA</c> domain availability lookups.
/// </summary>
/// <remarks>
/// CIRA operates its own authoritative RDAP endpoint at <c>https://rdap.cira.ca/</c>.
/// Using this client for <c>.CA</c> domains avoids the extra network hop through the generic
/// <c>rdap.org</c> bootstrap proxy and provides more reliable results for the Canadian TLD.
/// </remarks>
public interface ICiraRdapClient : IRdapClient
{
}
