using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace OnePageAuthor.Test.Services
{
    /// <summary>
    /// Unit tests for ContactInformationValidationService.
    /// </summary>
    public class ContactInformationValidationServiceTests
    {
        private readonly Mock<ILogger<ContactInformationValidationService>> _mockLogger;
        private readonly ContactInformationValidationService _service;

        public ContactInformationValidationServiceTests()
        {
            _mockLogger = new Mock<ILogger<ContactInformationValidationService>>();
            _service = new ContactInformationValidationService(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ContactInformationValidationService(null!));
        }

        [Fact]
        public void ValidateContactInformation_WithNullContactInfo_ReturnsInvalidResult()
        {
            // Act
            var result = _service.ValidateContactInformation(null!);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Contains("Contact information is required", result.Errors);
        }

        [Fact]
        public void ValidateContactInformation_WithValidContactInfo_ReturnsValidResult()
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData("", "First name is required")]
        [InlineData("   ", "First name is required")]
        [InlineData(null, "First name is required")]
        [InlineData("A", "First name must be at least 2 characters long")]
        public void ValidateContactInformation_WithInvalidFirstName_ReturnsInvalidResult(string? firstName, string expectedError)
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.FirstName = firstName!;

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Fact]
        public void ValidateContactInformation_WithTooLongFirstName_ReturnsInvalidResult()
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.FirstName = new string('A', 51); // 51 characters

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("First name cannot exceed 50 characters", result.Errors);
        }

        [Theory]
        [InlineData("John123", "First name contains invalid characters")]
        [InlineData("John@", "First name contains invalid characters")]
        [InlineData("John#Smith", "First name contains invalid characters")]
        public void ValidateContactInformation_WithInvalidFirstNameCharacters_ReturnsInvalidResult(string firstName, string expectedError)
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.FirstName = firstName;

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("", "Last name is required")]
        [InlineData("   ", "Last name is required")]
        [InlineData(null, "Last name is required")]
        [InlineData("S", "Last name must be at least 2 characters long")]
        public void ValidateContactInformation_WithInvalidLastName_ReturnsInvalidResult(string? lastName, string expectedError)
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.LastName = lastName!;

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("", "Email address is required")]
        [InlineData("   ", "Email address is required")]
        [InlineData(null, "Email address is required")]
        [InlineData("invalid-email", "Email address format is invalid")]
        [InlineData("invalid@", "Email address format is invalid")]
        [InlineData("@invalid.com", "Email address format is invalid")]
        [InlineData("invalid..email@test.com", "Email address format is invalid")]
        public void ValidateContactInformation_WithInvalidEmail_ReturnsInvalidResult(string? email, string expectedError)
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.EmailAddress = email!;

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("", "Address is required")]
        [InlineData("   ", "Address is required")]
        [InlineData(null, "Address is required")]
        [InlineData("123", "Address must be at least 5 characters long")]
        public void ValidateContactInformation_WithInvalidAddress_ReturnsInvalidResult(string? address, string expectedError)
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.Address = address!;

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Fact]
        public void ValidateContactInformation_WithTooLongAddress_ReturnsInvalidResult()
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.Address = new string('A', 101); // 101 characters

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Address cannot exceed 100 characters", result.Errors);
        }

        [Theory]
        [InlineData("", "City is required")]
        [InlineData("   ", "City is required")]
        [InlineData(null, "City is required")]
        [InlineData("A", "City must be at least 2 characters long")]
        public void ValidateContactInformation_WithInvalidCity_ReturnsInvalidResult(string? city, string expectedError)
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.City = city!;

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("", "State is required")]
        [InlineData("   ", "State is required")]
        [InlineData(null, "State is required")]
        [InlineData("A", "State must be at least 2 characters long")]
        public void ValidateContactInformation_WithInvalidState_ReturnsInvalidResult(string? state, string expectedError)
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.State = state!;

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("", "Country is required")]
        [InlineData("   ", "Country is required")]
        [InlineData(null, "Country is required")]
        [InlineData("InvalidCountry", "Country 'InvalidCountry' is not supported")]
        public void ValidateContactInformation_WithInvalidCountry_ReturnsInvalidResult(string? country, string expectedError)
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.Country = country!;

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("", "ZIP code is required")]
        [InlineData("   ", "ZIP code is required")]
        [InlineData(null, "ZIP code is required")]
        [InlineData("123", "ZIP code format is invalid for the specified country")]
        [InlineData("abcde", "ZIP code format is invalid for the specified country")]
        public void ValidateContactInformation_WithInvalidZipCode_ReturnsInvalidResult(string? zipCode, string expectedError)
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.ZipCode = zipCode!;

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("", "Telephone number is required")]
        [InlineData("   ", "Telephone number is required")]
        [InlineData(null, "Telephone number is required")]
        [InlineData("abc", "Telephone number format is invalid")]
        [InlineData("123", "Telephone number format is invalid")]
        public void ValidateContactInformation_WithInvalidPhoneNumber_ReturnsInvalidResult(string? phoneNumber, string expectedError)
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.TelephoneNumber = phoneNumber!;

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("john@example.com", true)]
        [InlineData("john.doe@example.com", true)]
        [InlineData("john+test@example.com", true)]
        [InlineData("john@sub.example.com", true)]
        [InlineData("invalid-email", false)]
        [InlineData("invalid@", false)]
        [InlineData("@invalid.com", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidEmail_WithVariousInputs_ReturnsExpectedResult(string? email, bool expected)
        {
            // Act
            var result = _service.IsValidEmail(email!);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("1234567890", true)]
        [InlineData("+11234567890", true)]
        [InlineData("123-456-7890", true)]
        [InlineData("(123) 456-7890", true)]
        [InlineData("123.456.7890", true)]
        [InlineData("abc", false)]
        [InlineData("123", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidPhoneNumber_WithVariousInputs_ReturnsExpectedResult(string? phoneNumber, bool expected)
        {
            // Act
            var result = _service.IsValidPhoneNumber(phoneNumber!);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("US", "12345", true)]
        [InlineData("US", "12345-6789", true)]
        [InlineData("US", "1234", false)]
        [InlineData("US", "abcde", false)]
        [InlineData("CA", "K1A 0A6", true)]
        [InlineData("CA", "K1A0A6", true)]
        [InlineData("UK", "SW1A 1AA", true)]
        [InlineData("DE", "12345", true)]
        public void ValidateContactInformation_WithCountrySpecificZipCodes_ReturnsExpectedResult(string country, string zipCode, bool shouldBeValid)
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.Country = country;
            contactInfo.ZipCode = zipCode;

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.Equal(shouldBeValid, result.IsValid);
            if (!shouldBeValid)
            {
                Assert.Contains("ZIP code format is invalid for the specified country", result.Errors);
            }
        }

        [Fact]
        public void ValidateContactInformation_WithMultipleErrors_ReturnsAllErrors()
        {
            // Arrange
            var contactInfo = new ContactInformation
            {
                FirstName = "",
                LastName = "",
                EmailAddress = "invalid-email",
                Address = "",
                City = "",
                State = "",
                Country = "InvalidCountry",
                ZipCode = "",
                TelephoneNumber = ""
            };

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.False(result.IsValid);
            Assert.True(result.Errors.Count >= 8); // Should have multiple validation errors
            Assert.Contains("First name is required", result.Errors);
            Assert.Contains("Last name is required", result.Errors);
            Assert.Contains("Email address format is invalid", result.Errors);
            Assert.Contains("Address is required", result.Errors);
        }

        [Theory]
        [InlineData("John", true)]
        [InlineData("Jean-Paul", true)]
        [InlineData("Mary-Jane", true)]
        [InlineData("O'Connor", true)]
        [InlineData("José", true)]
        [InlineData("François", true)]
        [InlineData("John123", false)]
        [InlineData("John@", false)]
        public void ValidateContactInformation_WithVariousNameFormats_ReturnsExpectedResult(string name, bool shouldBeValid)
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.FirstName = name;

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.Equal(shouldBeValid, result.IsValid);
            if (!shouldBeValid)
            {
                Assert.Contains("First name contains invalid characters", result.Errors);
            }
        }

        [Theory]
        [InlineData("New York", true)]
        [InlineData("San Francisco", true)]
        [InlineData("Saint-Jean", true)]
        [InlineData("St. Louis", true)]
        [InlineData("Las Vegas", true)]
        [InlineData("City123", true)] // Cities can have numbers
        [InlineData("C@ty", false)]
        public void ValidateContactInformation_WithVariousCityFormats_ReturnsExpectedResult(string city, bool shouldBeValid)
        {
            // Arrange
            var contactInfo = CreateValidContactInformation();
            contactInfo.City = city;

            // Act
            var result = _service.ValidateContactInformation(contactInfo);

            // Assert
            Assert.Equal(shouldBeValid, result.IsValid);
            if (!shouldBeValid)
            {
                Assert.Contains("City name contains invalid characters", result.Errors);
            }
        }

        private static ContactInformation CreateValidContactInformation()
        {
            return new ContactInformation
            {
                FirstName = "John",
                LastName = "Doe",
                EmailAddress = "john.doe@example.com",
                Address = "123 Main Street",
                City = "Anytown",
                State = "CA",
                Country = "US",
                ZipCode = "12345",
                TelephoneNumber = "1234567890"
            };
        }
    }
}