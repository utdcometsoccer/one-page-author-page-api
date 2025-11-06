using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Service for validating domain information.
    /// </summary>
    public interface IDomainValidationService
    {
        /// <summary>
        /// Validates domain information.
        /// </summary>
        /// <param name="domain">The domain to validate.</param>
        /// <returns>A validation result indicating success or failure with details.</returns>
        ValidationResult ValidateDomain(Domain domain);

        /// <summary>
        /// Validates a domain name string.
        /// </summary>
        /// <param name="domainName">The domain name to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        bool IsValidDomainName(string domainName);

        /// <summary>
        /// Validates a top-level domain string.
        /// </summary>
        /// <param name="topLevelDomain">The TLD to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        bool IsValidTopLevelDomain(string topLevelDomain);
    }

    /// <summary>
    /// Service for validating contact information.
    /// </summary>
    public interface IContactInformationValidationService
    {
        /// <summary>
        /// Validates contact information.
        /// </summary>
        /// <param name="contactInfo">The contact information to validate.</param>
        /// <returns>A validation result indicating success or failure with details.</returns>
        ValidationResult ValidateContactInformation(ContactInformation contactInfo);

        /// <summary>
        /// Validates an email address.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        bool IsValidEmail(string email);

        /// <summary>
        /// Validates a telephone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        bool IsValidPhoneNumber(string phoneNumber);
    }

    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets whether the validation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the validation error messages.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>A valid ValidationResult.</returns>
        public static ValidationResult Success() => new ValidationResult { IsValid = true };

        /// <summary>
        /// Creates a failed validation result with errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        /// <returns>An invalid ValidationResult with error messages.</returns>
        public static ValidationResult Failure(params string[] errors) => 
            new ValidationResult { IsValid = false, Errors = errors.ToList() };

        /// <summary>
        /// Creates a failed validation result with a single error.
        /// </summary>
        /// <param name="error">The validation error.</param>
        /// <returns>An invalid ValidationResult with the error message.</returns>
        public static ValidationResult Failure(string error) => 
            new ValidationResult { IsValid = false, Errors = new List<string> { error } };
    }
}