using System.Text.RegularExpressions;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Services
{
    /// <summary>
    /// Service for validating domain information.
    /// </summary>
    public class DomainValidationService : IDomainValidationService
    {
        private readonly ILogger<DomainValidationService> _logger;

        // Regex patterns for domain validation
        private static readonly Regex DomainNameRegex = new Regex(
            @"^[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?$", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TopLevelDomainRegex = new Regex(
            @"^[a-zA-Z]{2,6}$", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Common reserved domain names that should not be allowed
        private static readonly HashSet<string> ReservedDomainNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "www", "ftp", "mail", "email", "smtp", "pop", "imap", "dns", "ns1", "ns2", "localhost", "admin", "root", "test"
        };

        // Valid TLDs (this could be expanded or loaded from a configuration source)
        private static readonly HashSet<string> ValidTopLevelDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "com", "org", "net", "edu", "gov", "mil", "int", "co", "io", "app", "dev", "tech", "info", "biz", "name", "me"
        };

        public DomainValidationService(ILogger<DomainValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates domain information comprehensively.
        /// </summary>
        /// <param name="domain">The domain to validate.</param>
        /// <returns>A validation result with details about any issues found.</returns>
        public ValidationResult ValidateDomain(Domain domain)
        {
            if (domain == null)
            {
                _logger.LogWarning("Domain validation failed: domain is null");
                return ValidationResult.Failure("Domain information is required");
            }

            var errors = new List<string>();

            // Validate second-level domain
            if (string.IsNullOrWhiteSpace(domain.SecondLevelDomain))
            {
                errors.Add("Second level domain is required");
            }
            else if (!IsValidDomainName(domain.SecondLevelDomain))
            {
                errors.Add("Invalid second level domain name format");
            }
            else if (ReservedDomainNames.Contains(domain.SecondLevelDomain))
            {
                errors.Add("Second level domain name is reserved and cannot be used");
            }
            else if (domain.SecondLevelDomain.Length < 2)
            {
                errors.Add("Second level domain must be at least 2 characters long");
            }
            else if (domain.SecondLevelDomain.Length > 63)
            {
                errors.Add("Second level domain cannot exceed 63 characters");
            }

            // Validate top-level domain
            if (string.IsNullOrWhiteSpace(domain.TopLevelDomain))
            {
                errors.Add("Top level domain is required");
            }
            else if (!IsValidTopLevelDomain(domain.TopLevelDomain))
            {
                errors.Add("Invalid top level domain format");
            }
            else if (!ValidTopLevelDomains.Contains(domain.TopLevelDomain))
            {
                errors.Add($"Top level domain '{domain.TopLevelDomain}' is not supported");
            }

            // Validate full domain length
            if (!string.IsNullOrWhiteSpace(domain.SecondLevelDomain) && 
                !string.IsNullOrWhiteSpace(domain.TopLevelDomain))
            {
                var fullDomain = domain.FullDomainName;
                if (fullDomain.Length > 253)
                {
                    errors.Add("Full domain name cannot exceed 253 characters");
                }
            }

            if (errors.Any())
            {
                _logger.LogWarning("Domain validation failed for {Domain}: {Errors}", 
                    domain.FullDomainName, string.Join(", ", errors));
                return ValidationResult.Failure(errors.ToArray());
            }

            _logger.LogDebug("Domain validation successful for {Domain}", domain.FullDomainName);
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates a domain name string using regex pattern.
        /// </summary>
        /// <param name="domainName">The domain name to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValidDomainName(string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
                return false;

            // Check basic format: alphanumeric and hyphens, not starting/ending with hyphen
            return DomainNameRegex.IsMatch(domainName);
        }

        /// <summary>
        /// Validates a top-level domain string using regex pattern.
        /// </summary>
        /// <param name="topLevelDomain">The TLD to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValidTopLevelDomain(string topLevelDomain)
        {
            if (string.IsNullOrWhiteSpace(topLevelDomain))
                return false;

            // Check basic format: letters only, 2-6 characters
            return TopLevelDomainRegex.IsMatch(topLevelDomain);
        }
    }
}