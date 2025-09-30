using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.Functions;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations;

namespace OnePageAuthor.Test.DomainRegistration
{
    /// <summary>
    /// Unit tests for DomainRegistrationFunction.
    /// Note: These tests focus on constructor validation and basic structure.
    /// Full integration testing is needed for authentication flows due to static helper usage.
    /// </summary>
    public class DomainRegistrationFunctionTests
    {
        private readonly Mock<ILogger<DomainRegistrationFunction>> _loggerMock;
        private readonly Mock<IJwtValidationService> _jwtValidationServiceMock;
        private readonly Mock<IUserProfileService> _userProfileServiceMock;
        private readonly Mock<IDomainRegistrationService> _domainRegistrationServiceMock;
        private readonly DomainRegistrationFunction _function;

        public DomainRegistrationFunctionTests()
        {
            _loggerMock = new Mock<ILogger<DomainRegistrationFunction>>();
            _jwtValidationServiceMock = new Mock<IJwtValidationService>();
            _userProfileServiceMock = new Mock<IUserProfileService>();
            _domainRegistrationServiceMock = new Mock<IDomainRegistrationService>();

            _function = new DomainRegistrationFunction(
                _loggerMock.Object,
                _jwtValidationServiceMock.Object,
                _userProfileServiceMock.Object,
                _domainRegistrationServiceMock.Object);
        }

        private static Mock<HttpRequest> CreateMockRequest()
        {
            var mockRequest = new Mock<HttpRequest>();
            var headers = new HeaderDictionary();
            mockRequest.Setup(r => r.Headers).Returns(headers);
            return mockRequest;
        }

        private static ClaimsPrincipal CreateTestUser(string upn = "test@example.com")
        {
            var claims = new List<Claim>
            {
                new Claim("upn", upn),
                new Claim("oid", "test-oid-123")
            };
            var identity = new ClaimsIdentity(claims, "test");
            return new ClaimsPrincipal(identity);
        }

        private static CreateDomainRegistrationRequest CreateTestRequest()
        {
            return new CreateDomainRegistrationRequest
            {
                Domain = new DomainDto
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example"
                },
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
        }

        private static InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration CreateTestDomainRegistration(string id = "test-id")
        {
            return new InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration
            {
                id = id,
                Upn = "test@example.com",
                Domain = new Domain
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example"
                },
                ContactInformation = new ContactInformation
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
                },
                CreatedAt = DateTime.UtcNow,
                Status = DomainRegistrationStatus.Pending
            };
        }

        [Fact]
        public void Constructor_ThrowsException_NullLogger()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationFunction(
                null!, _jwtValidationServiceMock.Object, _userProfileServiceMock.Object, _domainRegistrationServiceMock.Object));
        }

        [Fact]
        public void Constructor_ThrowsException_NullJwtValidationService()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationFunction(
                _loggerMock.Object, null!, _userProfileServiceMock.Object, _domainRegistrationServiceMock.Object));
        }

        [Fact]
        public void Constructor_ThrowsException_NullUserProfileService()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationFunction(
                _loggerMock.Object, _jwtValidationServiceMock.Object, null!, _domainRegistrationServiceMock.Object));
        }

        [Fact]
        public void Constructor_ThrowsException_NullDomainRegistrationService()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationFunction(
                _loggerMock.Object, _jwtValidationServiceMock.Object, _userProfileServiceMock.Object, null!));
        }

        /// <summary>
        /// Note: Tests for authentication flows require integration testing due to static JwtAuthenticationHelper usage.
        /// The tests below focus on what can be unit tested - constructor validation and service dependencies.
        /// </summary>
        
        [Fact]
        public void DomainRegistrationFunction_ValidatesDependencies()
        {
            // This test ensures the function is properly constructed and dependencies are wired
            // The actual HTTP endpoint testing requires integration tests due to static helper methods
            
            // Assert - Constructor succeeded without exceptions
            Assert.NotNull(_function);
        }

        [Fact] 
        public void CreateDomainRegistrationRequest_CanBeInstantiated()
        {
            // Arrange & Act
            var request = CreateTestRequest();

            // Assert
            Assert.NotNull(request);
            Assert.NotNull(request.Domain);
            Assert.NotNull(request.ContactInformation);
            Assert.Equal("example", request.Domain.SecondLevelDomain);
            Assert.Equal("com", request.Domain.TopLevelDomain);
            Assert.Equal("john@example.com", request.ContactInformation.EmailAddress);
        }

        [Fact]
        public void DomainRegistrationResponse_CanBeCreatedFromEntity()
        {
            // Arrange
            var domainRegistration = CreateTestDomainRegistration();

            // Act
            var response = new DomainRegistrationResponse
            {
                Id = domainRegistration.id!,
                Domain = new DomainDto
                {
                    SecondLevelDomain = domainRegistration.Domain.SecondLevelDomain,
                    TopLevelDomain = domainRegistration.Domain.TopLevelDomain
                },
                ContactInformation = new ContactInformationDto
                {
                    FirstName = domainRegistration.ContactInformation.FirstName,
                    LastName = domainRegistration.ContactInformation.LastName,
                    EmailAddress = domainRegistration.ContactInformation.EmailAddress,
                    Address = domainRegistration.ContactInformation.Address,
                    City = domainRegistration.ContactInformation.City,
                    State = domainRegistration.ContactInformation.State,
                    Country = domainRegistration.ContactInformation.Country,
                    ZipCode = domainRegistration.ContactInformation.ZipCode,
                    TelephoneNumber = domainRegistration.ContactInformation.TelephoneNumber
                },
                CreatedAt = domainRegistration.CreatedAt,
                Status = domainRegistration.Status
            };

            // Assert
            Assert.NotNull(response);
            Assert.Equal(domainRegistration.id, response.Id);
            Assert.Equal(domainRegistration.Domain.SecondLevelDomain, response.Domain.SecondLevelDomain);
            Assert.Equal(domainRegistration.ContactInformation.EmailAddress, response.ContactInformation.EmailAddress);
            Assert.Equal(domainRegistration.Status, response.Status);
        }
    }
}