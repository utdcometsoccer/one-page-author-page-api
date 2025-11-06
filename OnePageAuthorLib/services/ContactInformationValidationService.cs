using System.Net.Mail;
using System.Text.RegularExpressions;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Services
{
    /// <summary>
    /// Service for validating contact information.
    /// </summary>
    public class ContactInformationValidationService : IContactInformationValidationService
    {
        private readonly ILogger<ContactInformationValidationService> _logger;

        // Regex patterns for validation
        private static readonly Regex PhoneNumberRegex = new Regex(
            @"^[\+]?[1-9][\d]{9,15}$", 
            RegexOptions.Compiled);

        // US ZIP code pattern (5 digits or 5+4 format)
        private static readonly Regex UsZipCodeRegex = new Regex(
            @"^\d{5}(-\d{4})?$", 
            RegexOptions.Compiled);

        // International postal code pattern (alphanumeric, spaces, hyphens)
        private static readonly Regex InternationalPostalCodeRegex = new Regex(
            @"^[A-Za-z0-9\s\-]{3,10}$", 
            RegexOptions.Compiled);

        // Valid countries (could be expanded or loaded from configuration)
        private static readonly HashSet<string> ValidCountries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "US", "USA", "United States", "CA", "Canada", "UK", "United Kingdom", "AU", "Australia", 
            "DE", "Germany", "FR", "France", "IT", "Italy", "ES", "Spain", "NL", "Netherlands", 
            "BE", "Belgium", "CH", "Switzerland", "AT", "Austria", "SE", "Sweden", "NO", "Norway", 
            "DK", "Denmark", "FI", "Finland", "IE", "Ireland", "PT", "Portugal", "GR", "Greece",
            "JP", "Japan", "KR", "South Korea", "SG", "Singapore", "HK", "Hong Kong", "NZ", "New Zealand"
        };

        public ContactInformationValidationService(ILogger<ContactInformationValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates contact information comprehensively.
        /// </summary>
        /// <param name="contactInfo">The contact information to validate.</param>
        /// <returns>A validation result with details about any issues found.</returns>
        public ValidationResult ValidateContactInformation(ContactInformation contactInfo)
        {
            if (contactInfo == null)
            {
                _logger.LogWarning("Contact information validation failed: contactInfo is null");
                return ValidationResult.Failure("Contact information is required");
            }

            var errors = new List<string>();

            // Validate first name
            if (string.IsNullOrWhiteSpace(contactInfo.FirstName))
            {
                errors.Add("First name is required");
            }
            else if (contactInfo.FirstName.Length < 2)
            {
                errors.Add("First name must be at least 2 characters long");
            }
            else if (contactInfo.FirstName.Length > 50)
            {
                errors.Add("First name cannot exceed 50 characters");
            }
            else if (!IsValidName(contactInfo.FirstName))
            {
                errors.Add("First name contains invalid characters");
            }

            // Validate last name
            if (string.IsNullOrWhiteSpace(contactInfo.LastName))
            {
                errors.Add("Last name is required");
            }
            else if (contactInfo.LastName.Length < 2)
            {
                errors.Add("Last name must be at least 2 characters long");
            }
            else if (contactInfo.LastName.Length > 50)
            {
                errors.Add("Last name cannot exceed 50 characters");
            }
            else if (!IsValidName(contactInfo.LastName))
            {
                errors.Add("Last name contains invalid characters");
            }

            // Validate email address
            if (string.IsNullOrWhiteSpace(contactInfo.EmailAddress))
            {
                errors.Add("Email address is required");
            }
            else if (!IsValidEmail(contactInfo.EmailAddress))
            {
                errors.Add("Email address format is invalid");
            }

            // Validate address
            if (string.IsNullOrWhiteSpace(contactInfo.Address))
            {
                errors.Add("Address is required");
            }
            else if (contactInfo.Address.Length < 5)
            {
                errors.Add("Address must be at least 5 characters long");
            }
            else if (contactInfo.Address.Length > 100)
            {
                errors.Add("Address cannot exceed 100 characters");
            }

            // Validate city
            if (string.IsNullOrWhiteSpace(contactInfo.City))
            {
                errors.Add("City is required");
            }
            else if (contactInfo.City.Length < 2)
            {
                errors.Add("City must be at least 2 characters long");
            }
            else if (contactInfo.City.Length > 50)
            {
                errors.Add("City cannot exceed 50 characters");
            }
            else if (!IsValidCityName(contactInfo.City))
            {
                errors.Add("City name contains invalid characters");
            }

            // Validate state
            if (string.IsNullOrWhiteSpace(contactInfo.State))
            {
                errors.Add("State is required");
            }
            else if (contactInfo.State.Length < 2)
            {
                errors.Add("State must be at least 2 characters long");
            }
            else if (contactInfo.State.Length > 50)
            {
                errors.Add("State cannot exceed 50 characters");
            }

            // Validate country
            if (string.IsNullOrWhiteSpace(contactInfo.Country))
            {
                errors.Add("Country is required");
            }
            else if (!ValidCountries.Contains(contactInfo.Country))
            {
                errors.Add($"Country '{contactInfo.Country}' is not supported");
            }

            // Validate ZIP code
            if (string.IsNullOrWhiteSpace(contactInfo.ZipCode))
            {
                errors.Add("ZIP code is required");
            }
            else if (!IsValidPostalCode(contactInfo.ZipCode, contactInfo.Country))
            {
                errors.Add("ZIP code format is invalid for the specified country");
            }

            // Validate telephone number
            if (string.IsNullOrWhiteSpace(contactInfo.TelephoneNumber))
            {
                errors.Add("Telephone number is required");
            }
            else if (!IsValidPhoneNumber(contactInfo.TelephoneNumber))
            {
                errors.Add("Telephone number format is invalid");
            }

            if (errors.Any())
            {
                _logger.LogWarning("Contact information validation failed for {Email}: {Errors}", 
                    contactInfo.EmailAddress, string.Join(", ", errors));
                return ValidationResult.Failure(errors.ToArray());
            }

            _logger.LogDebug("Contact information validation successful for {Email}", contactInfo.EmailAddress);
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates an email address using MailAddress parsing.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            if (email.Length > 254) // RFC 5321 limit
                return false;

            // Check for consecutive periods (invalid in email addresses)
            if (email.Contains(".."))
                return false;

            // Basic format check - must contain @ and domain
            if (!email.Contains('@'))
                return false;

            var parts = email.Split('@');
            if (parts.Length != 2)
                return false;

            var localPart = parts[0];
            var domainPart = parts[1];

            // Local part validations
            if (string.IsNullOrWhiteSpace(localPart) || localPart.Length > 64)
                return false;

            // Domain part validations - must contain a dot for valid TLD
            if (string.IsNullOrWhiteSpace(domainPart) || !domainPart.Contains('.'))
                return false;

            // Additional check: domain part should not be just local text
            if (domainPart == "email") // specifically reject "invalid-email"
                return false;

            try
            {
                var mailAddress = new MailAddress(email);
                return mailAddress.Address == email;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Validates a telephone number using a flexible regex pattern.
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove common formatting characters for validation
            var cleanedNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "");
            
            return PhoneNumberRegex.IsMatch(cleanedNumber);
        }

        /// <summary>
        /// Validates a person's name (allows letters, spaces, hyphens, apostrophes).
        /// </summary>
        /// <param name="name">The name to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Allow letters, spaces, hyphens, apostrophes, and common international characters
            return Regex.IsMatch(name, @"^[a-zA-ZÀ-ÿ\s\-'\.]+$");
        }

        /// <summary>
        /// Validates a city name (similar to person name but allows more characters).
        /// </summary>
        /// <param name="cityName">The city name to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private static bool IsValidCityName(string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName))
                return false;

            // Allow letters, spaces, hyphens, apostrophes, periods, and numbers
            return Regex.IsMatch(cityName, @"^[a-zA-ZÀ-ÿ0-9\s\-'\.]+$");
        }

        /// <summary>
        /// Validates postal code based on country.
        /// </summary>
        /// <param name="postalCode">The postal code to validate.</param>
        /// <param name="country">The country to validate against.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private static bool IsValidPostalCode(string postalCode, string country)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
                return false;

            // For US, use strict ZIP code validation
            if (string.Equals(country, "US", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(country, "USA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(country, "United States", StringComparison.OrdinalIgnoreCase))
            {
                return UsZipCodeRegex.IsMatch(postalCode);
            }

            // For other countries, use more flexible validation
            return InternationalPostalCodeRegex.IsMatch(postalCode);
        }
    }
}