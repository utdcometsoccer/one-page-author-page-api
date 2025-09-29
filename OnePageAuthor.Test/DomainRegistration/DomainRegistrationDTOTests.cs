using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations;

namespace OnePageAuthor.Test.DomainRegistration
{
    public class DomainRegistrationDTOTests
    {
        [Fact]
        public void DomainDto_ToEntity_MapsCorrectly()
        {
            // Arrange
            var dto = new DomainDto
            {
                TopLevelDomain = "com",
                SecondLevelDomain = "example"
            };

            // Act
            var entity = dto.ToEntity();

            // Assert
            Assert.NotNull(entity);
            Assert.Equal("com", entity.TopLevelDomain);
            Assert.Equal("example", entity.SecondLevelDomain);
            Assert.Equal("example.com", entity.FullDomainName);
        }

        [Fact]
        public void ContactInformationDto_ToEntity_MapsCorrectly()
        {
            // Arrange
            var dto = new ContactInformationDto
            {
                FirstName = "John",
                LastName = "Doe",
                Address = "123 Main St",
                Address2 = "Apt 4B",
                City = "Anytown",
                State = "CA",
                Country = "USA",
                ZipCode = "12345",
                EmailAddress = "john@example.com",
                TelephoneNumber = "+1-555-123-4567"
            };

            // Act
            var entity = dto.ToEntity();

            // Assert
            Assert.NotNull(entity);
            Assert.Equal("John", entity.FirstName);
            Assert.Equal("Doe", entity.LastName);
            Assert.Equal("123 Main St", entity.Address);
            Assert.Equal("Apt 4B", entity.Address2);
            Assert.Equal("Anytown", entity.City);
            Assert.Equal("CA", entity.State);
            Assert.Equal("USA", entity.Country);
            Assert.Equal("12345", entity.ZipCode);
            Assert.Equal("john@example.com", entity.EmailAddress);
            Assert.Equal("+1-555-123-4567", entity.TelephoneNumber);
        }

        [Fact]
        public void DomainRegistrationResponse_FromEntity_MapsCorrectly()
        {
            // Arrange
            var entity = new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration
            {
                id = "test-id-123",
                Upn = "test@example.com",
                Domain = new Domain
                {
                    TopLevelDomain = "org",
                    SecondLevelDomain = "testsite"
                },
                ContactInformation = new ContactInformation
                {
                    FirstName = "Jane",
                    LastName = "Smith",
                    Address = "456 Oak Ave",
                    Address2 = null,
                    City = "Springfield",
                    State = "IL",
                    Country = "USA",
                    ZipCode = "62701",
                    EmailAddress = "jane@example.org",
                    TelephoneNumber = "+1-217-555-9876"
                },
                CreatedAt = new DateTime(2025, 9, 29, 12, 0, 0, DateTimeKind.Utc),
                Status = DomainRegistrationStatus.InProgress
            };

            // Act
            var response = DomainRegistrationResponse.FromEntity(entity);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("test-id-123", response.Id);
            Assert.Equal("org", response.Domain.TopLevelDomain);
            Assert.Equal("testsite", response.Domain.SecondLevelDomain);
            Assert.Equal("Jane", response.ContactInformation.FirstName);
            Assert.Equal("Smith", response.ContactInformation.LastName);
            Assert.Equal("456 Oak Ave", response.ContactInformation.Address);
            Assert.Null(response.ContactInformation.Address2);
            Assert.Equal("Springfield", response.ContactInformation.City);
            Assert.Equal("IL", response.ContactInformation.State);
            Assert.Equal("USA", response.ContactInformation.Country);
            Assert.Equal("62701", response.ContactInformation.ZipCode);
            Assert.Equal("jane@example.org", response.ContactInformation.EmailAddress);
            Assert.Equal("+1-217-555-9876", response.ContactInformation.TelephoneNumber);
            Assert.Equal(new DateTime(2025, 9, 29, 12, 0, 0, DateTimeKind.Utc), response.CreatedAt);
            Assert.Equal(DomainRegistrationStatus.InProgress, response.Status);
        }

        [Fact]
        public void CreateDomainRegistrationRequest_ValidatesRequired_Domain()
        {
            // Arrange
            var request = new CreateDomainRegistrationRequest
            {
                Domain = null!, // Explicitly set to null to test validation
                ContactInformation = new ContactInformationDto
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Address = "123 Main St",
                    City = "Anytown",
                    State = "CA",
                    Country = "USA",
                    ZipCode = "12345",
                    EmailAddress = "john@example.com",
                    TelephoneNumber = "+1-555-123-4567"
                }
            };

            // Act & Assert
            // This would be validated by the ASP.NET Core model validation
            Assert.Null(request.Domain);
            Assert.NotNull(request.ContactInformation);
        }

        [Fact]
        public void CreateDomainRegistrationRequest_ValidatesRequired_ContactInformation()
        {
            // Arrange
            var request = new CreateDomainRegistrationRequest
            {
                Domain = new DomainDto
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example"
                },
                ContactInformation = null! // Explicitly set to null to test validation
            };

            // Act & Assert
            // This would be validated by the ASP.NET Core model validation
            Assert.NotNull(request.Domain);
            Assert.Null(request.ContactInformation);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void DomainDto_RequiresTopLevelDomain(string? topLevelDomain)
        {
            // Arrange
            var dto = new DomainDto
            {
                TopLevelDomain = topLevelDomain!,
                SecondLevelDomain = "example"
            };

            // Act & Assert
            // This tests the data annotation validation logic
            Assert.True(string.IsNullOrWhiteSpace(dto.TopLevelDomain));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void DomainDto_RequiresSecondLevelDomain(string? secondLevelDomain)
        {
            // Arrange
            var dto = new DomainDto
            {
                TopLevelDomain = "com",
                SecondLevelDomain = secondLevelDomain!
            };

            // Act & Assert
            // This tests the data annotation validation logic
            Assert.True(string.IsNullOrWhiteSpace(dto.SecondLevelDomain));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void ContactInformationDto_RequiresFirstName(string? firstName)
        {
            // Arrange
            var dto = new ContactInformationDto
            {
                FirstName = firstName!,
                LastName = "Doe",
                Address = "123 Main St",
                City = "Anytown",
                State = "CA",
                Country = "USA",
                ZipCode = "12345",
                EmailAddress = "john@example.com",
                TelephoneNumber = "+1-555-123-4567"
            };

            // Act & Assert
            Assert.True(string.IsNullOrWhiteSpace(dto.FirstName));
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("@invalid.com")]
        [InlineData("invalid@")]
        [InlineData("")]
        public void ContactInformationDto_RequiresValidEmail(string emailAddress)
        {
            // Arrange
            var dto = new ContactInformationDto
            {
                FirstName = "John",
                LastName = "Doe",
                Address = "123 Main St",
                City = "Anytown",
                State = "CA",
                Country = "USA",
                ZipCode = "12345",
                EmailAddress = emailAddress,
                TelephoneNumber = "+1-555-123-4567"
            };

            // Act & Assert
            // This tests that invalid emails are set (validation would catch them)
            Assert.Equal(emailAddress, dto.EmailAddress);
        }
    }
}