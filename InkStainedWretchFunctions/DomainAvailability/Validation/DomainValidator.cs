using System.Text.RegularExpressions;

namespace InkStainedWretch.OnePageAuthorAPI.Functions.DomainAvailability.Validation;

/// <summary>
/// Validates that a domain string is a well-formed, registerable root domain.
/// </summary>
public static partial class DomainValidator
{
    // Labels (parts separated by dots) may contain letters, digits, and hyphens.
    // They must not start or end with a hyphen and must be 1–63 characters long.
    private static readonly Regex LabelRegex = LabelPattern();

    // A TLD must consist of at least two letters (ASCII alpha only for classic TLDs).
    // Internationalised TLDs encoded in punycode (xn--...) are also accepted.
    private static readonly Regex TldRegex = TldPattern();

    /// <summary>
    /// Returns <c>true</c> when <paramref name="domain"/> is a valid root domain; otherwise <c>false</c>.
    /// </summary>
    /// <param name="domain">The domain string to validate (may include trailing dot).</param>
    /// <param name="errorMessage">
    /// When the method returns <c>false</c>, contains a human-readable reason; otherwise <c>null</c>.
    /// </param>
    public static bool IsValid(string? domain, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(domain))
        {
            errorMessage = "Domain name must not be empty.";
            return false;
        }

        // Strip optional trailing dot (FQDN notation).
        var normalized = domain.Trim().TrimEnd('.').ToLowerInvariant();

        // Overall length: a domain including dots must not exceed 253 characters.
        if (normalized.Length > 253)
        {
            errorMessage = "Domain name must not exceed 253 characters.";
            return false;
        }

        var labels = normalized.Split('.');

        // A root domain has exactly two labels: <name>.<tld>.
        // More than two labels means a subdomain was supplied.
        if (labels.Length < 2)
        {
            errorMessage = "Domain name must include a valid TLD (e.g., example.com).";
            return false;
        }

        if (labels.Length > 2)
        {
            errorMessage = "Subdomains are not allowed. Please supply a root domain (e.g., example.com).";
            return false;
        }

        // Validate each label.
        foreach (var label in labels)
        {
            if (string.IsNullOrEmpty(label))
            {
                errorMessage = "Domain name contains an empty label.";
                return false;
            }

            if (label.Length > 63)
            {
                errorMessage = $"Each domain label must not exceed 63 characters (offending label: '{label}').";
                return false;
            }

            if (!LabelRegex.IsMatch(label))
            {
                errorMessage = $"Domain label '{label}' contains invalid characters or starts/ends with a hyphen.";
                return false;
            }
        }

        // Validate TLD specifically.
        var tld = labels[^1];
        if (!TldRegex.IsMatch(tld))
        {
            errorMessage = $"The TLD '{tld}' is not valid. TLDs must contain only letters or be a valid punycode label.";
            return false;
        }

        return true;
    }

    [GeneratedRegex(@"^[a-z0-9]([a-z0-9\-]{0,61}[a-z0-9])?$", RegexOptions.Compiled)]
    private static partial Regex LabelPattern();

    [GeneratedRegex(@"^([a-z]{2,}|xn--[a-z0-9\-]+)$", RegexOptions.Compiled)]
    private static partial Regex TldPattern();
}
