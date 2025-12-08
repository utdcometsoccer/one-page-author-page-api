using System.ComponentModel.DataAnnotations;

namespace InkStainedWretch.OnePageAuthorAPI.Entities.DTOs
{
    /// <summary>
    /// Data transfer object for creating a domain registration request.
    /// </summary>
    public class CreateDomainRegistrationRequest
    {
        /// <summary>
        /// Domain information for the registration request.
        /// </summary>
        [Required]
        public DomainDto Domain { get; set; } = new();

        /// <summary>
        /// Contact information for the domain registration.
        /// </summary>
        [Required]
        public ContactInformationDto ContactInformation { get; set; } = new();
    }

    /// <summary>
    /// Data transfer object for updating a domain registration request.
    /// </summary>
    public class UpdateDomainRegistrationRequest
    {
        /// <summary>
        /// Domain information for the registration request (optional - only updated if provided).
        /// </summary>
        public DomainDto? Domain { get; set; }

        /// <summary>
        /// Contact information for the domain registration (optional - only updated if provided).
        /// </summary>
        public ContactInformationDto? ContactInformation { get; set; }

        /// <summary>
        /// Status of the domain registration (optional - only updated if provided).
        /// </summary>
        public DomainRegistrationStatus? Status { get; set; }
    }

    /// <summary>
    /// Data transfer object for domain information.
    /// </summary>
    public class DomainDto
    {
        /// <summary>
        /// Top-level domain (e.g., "com", "org", "net").
        /// </summary>
        [Required]
        [StringLength(10, MinimumLength = 2)]
        public string TopLevelDomain { get; set; } = string.Empty;

        /// <summary>
        /// Second-level domain name (e.g., "example" in "example.com").
        /// </summary>
        [Required]
        [StringLength(63, MinimumLength = 1)]
        public string SecondLevelDomain { get; set; } = string.Empty;

        /// <summary>
        /// Converts to Domain entity.
        /// </summary>
        public Domain ToEntity()
        {
            return new Domain
            {
                TopLevelDomain = TopLevelDomain,
                SecondLevelDomain = SecondLevelDomain
            };
        }
    }

    /// <summary>
    /// Data transfer object for contact information.
    /// </summary>
    public class ContactInformationDto
    {
        /// <summary>
        /// First name of the contact.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Last name of the contact.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Primary address line.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Secondary address line (optional).
        /// </summary>
        [StringLength(100)]
        public string? Address2 { get; set; }

        /// <summary>
        /// City name.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// State or province.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Country name.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// ZIP or postal code.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string ZipCode { get; set; } = string.Empty;

        /// <summary>
        /// Email address.
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string EmailAddress { get; set; } = string.Empty;

        /// <summary>
        /// Telephone number.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string TelephoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Converts to ContactInformation entity.
        /// </summary>
        public ContactInformation ToEntity()
        {
            return new ContactInformation
            {
                FirstName = FirstName,
                LastName = LastName,
                Address = Address,
                Address2 = Address2,
                City = City,
                State = State,
                Country = Country,
                ZipCode = ZipCode,
                EmailAddress = EmailAddress,
                TelephoneNumber = TelephoneNumber
            };
        }
    }

    /// <summary>
    /// Data transfer object for domain registration response.
    /// </summary>
    public class DomainRegistrationResponse
    {
        /// <summary>
        /// The registration ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Domain information for the registration.
        /// </summary>
        public DomainDto Domain { get; set; } = new();

        /// <summary>
        /// Contact information for the registration.
        /// </summary>
        public ContactInformationDto ContactInformation { get; set; } = new();

        /// <summary>
        /// Timestamp when the registration was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Status of the domain registration.
        /// </summary>
        public DomainRegistrationStatus Status { get; set; }

        /// <summary>
        /// Creates a response DTO from a domain registration entity.
        /// </summary>
        public static DomainRegistrationResponse FromEntity(DomainRegistration entity)
        {
            return new DomainRegistrationResponse
            {
                Id = entity.id ?? string.Empty,
                Domain = new DomainDto
                {
                    TopLevelDomain = entity.Domain.TopLevelDomain,
                    SecondLevelDomain = entity.Domain.SecondLevelDomain
                },
                ContactInformation = new ContactInformationDto
                {
                    FirstName = entity.ContactInformation.FirstName,
                    LastName = entity.ContactInformation.LastName,
                    Address = entity.ContactInformation.Address,
                    Address2 = entity.ContactInformation.Address2,
                    City = entity.ContactInformation.City,
                    State = entity.ContactInformation.State,
                    Country = entity.ContactInformation.Country,
                    ZipCode = entity.ContactInformation.ZipCode,
                    EmailAddress = entity.ContactInformation.EmailAddress,
                    TelephoneNumber = entity.ContactInformation.TelephoneNumber
                },
                CreatedAt = entity.CreatedAt,
                Status = entity.Status
            };
        }
    }
}