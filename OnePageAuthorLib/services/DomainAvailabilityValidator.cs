using System.Text.RegularExpressions;

namespace InkStainedWretch.OnePageAuthorAPI.Services;

/// <summary>
/// Validates that a domain string is a well-formed, registerable root domain.
/// </summary>
/// <remarks>
/// <para>
/// This validator enforces structural rules (label length, allowed characters, valid TLD format)
/// and requires exactly two dot-separated labels (e.g., <c>example.com</c>). It does
/// <strong>not</strong> consult the Public Suffix List (PSL), so domains under multi-part
/// public suffixes such as <c>example.co.uk</c> or <c>example.com.au</c> will be rejected
/// as apparent subdomains. For general-purpose registrability checks across all TLDs,
/// integrate a PSL library (e.g., <c>Nager.PublicSuffix</c>).
/// </para>
/// <para>
/// <strong>.MX exception:</strong> Mexico's ccTLD uses registered second-level domains
/// (e.g., <c>example.com.mx</c>, <c>example.gob.mx</c>). Three-label domains whose
/// rightmost label is <c>mx</c> and whose middle label is one of the recognized .MX
/// second-level domains (<c>com</c>, <c>net</c>, <c>org</c>, <c>edu</c>, <c>gob</c>)
/// are accepted as valid root domains.
/// </para>
/// </remarks>
public static partial class DomainAvailabilityValidator
{
    // Labels (parts separated by dots) may contain letters, digits, and hyphens.
    // They must not start or end with a hyphen and must be 1–63 characters long.
    private static readonly Regex LabelRegex = LabelPattern();

    // A TLD must consist of at least two letters (ASCII alpha only for classic TLDs).
    // Internationalised TLDs encoded in punycode (xn--...) are also accepted.
    private static readonly Regex TldRegex = TldPattern();

    // Recognized second-level domains under Mexico's .MX ccTLD.
    // A three-label domain ending in .mx is only valid when the middle label is one of these.
    // This list reflects NIC Mexico's published SLD structure and is intentionally .MX-specific;
    // extend with a dictionary keyed by ccTLD if similar support is needed for other countries.
    private static readonly HashSet<string> MxSecondLevelDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "com", "net", "org", "edu", "gob"
    };

    // Pre-computed for use in validation error messages to avoid repeated LINQ allocations.
    private static readonly string MxSupportedSldList =
        string.Join(", ", MxSecondLevelDomains.OrderBy(x => x).Select(x => $"{x}.mx"));

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

        // Specialized .MX validation: Mexico's ccTLD uses registered second-level domains
        // (e.g., example.com.mx), producing three-label domain strings that are still
        // registerable root domains — not subdomains.
        if (labels.Length == 3 && labels[2] == "mx")
        {
            return IsValidMxDomain(labels, out errorMessage);
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

    /// <summary>
    /// Validates a three-label domain whose last label is <c>mx</c>, applying the specialized
    /// rules for Mexico's ccTLD (e.g., <c>example.com.mx</c>).
    /// </summary>
    /// <param name="labels">Normalized, split labels — must contain exactly three elements.</param>
    /// <param name="errorMessage">Human-readable error if invalid; otherwise <c>null</c>.</param>
    private static bool IsValidMxDomain(string[] labels, out string? errorMessage)
    {
        // labels[0] = registrable name (e.g., "example")
        // labels[1] = .MX second-level domain (e.g., "com")
        // labels[2] = "mx"
        var sld = labels[1];

        if (!MxSecondLevelDomains.Contains(sld))
        {
            errorMessage = $"'{sld}.mx' is not a recognized .MX second-level domain. Supported: {MxSupportedSldList}.";
            return false;
        }

        // Validate every label for length and character rules.
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

        errorMessage = null;
        return true;
    }
}
