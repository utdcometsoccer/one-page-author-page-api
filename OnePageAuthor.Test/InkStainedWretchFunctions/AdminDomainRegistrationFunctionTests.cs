using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorAPI.Functions;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OnePageAuthor.Test.InkStainedWretchFunctions
{
    using DomainRegistrationEntity = InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration;
    using DomainEntity = InkStainedWretch.OnePageAuthorAPI.Entities.Domain;
    using ContactInformationEntity = InkStainedWretch.OnePageAuthorAPI.Entities.ContactInformation;

    public class AdminDomainRegistrationFunctionTests
    {
        private readonly Mock<ILogger<AdminDomainRegistrationFunction>> _mockLogger;
        private readonly Mock<IJwtValidationService> _mockJwtValidationService;
        private readonly Mock<IDomainRegistrationRepository> _mockDomainRegistrationRepository;
        private readonly Mock<IFrontDoorService> _mockFrontDoorService;
        private readonly Mock<IWhmcsService> _mockWhmcsService;
        private readonly Mock<IDnsZoneService> _mockDnsZoneService;
        private readonly AdminDomainRegistrationFunction _function;

        public AdminDomainRegistrationFunctionTests()
        {
            _mockLogger = new Mock<ILogger<AdminDomainRegistrationFunction>>();
            _mockJwtValidationService = new Mock<IJwtValidationService>();
            _mockDomainRegistrationRepository = new Mock<IDomainRegistrationRepository>();
            _mockFrontDoorService = new Mock<IFrontDoorService>();
            _mockWhmcsService = new Mock<IWhmcsService>();
            _mockDnsZoneService = new Mock<IDnsZoneService>();

            _function = new AdminDomainRegistrationFunction(
                _mockLogger.Object,
                _mockJwtValidationService.Object,
                _mockDomainRegistrationRepository.Object,
                _mockFrontDoorService.Object,
                _mockWhmcsService.Object,
                _mockDnsZoneService.Object);
        }

        // Creates a real HttpRequest with an Authorization Bearer header
        private static HttpRequest CreateHttpRequestWithAuth(string token = "valid-token")
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = $"Bearer {token}";
            return context.Request;
        }

        // Creates a real HttpRequest with no Authorization header
        private static HttpRequest CreateHttpRequestWithoutAuth()
        {
            return new DefaultHttpContext().Request;
        }

        private static ClaimsPrincipal CreateAdminUser(string upn = "admin@example.com")
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("upn", upn),
                new Claim("oid", "admin-oid-123"),
                new Claim("roles", "Admin")
            }));
        }

        private static ClaimsPrincipal CreateNonAdminUser(string upn = "user@example.com")
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("upn", upn),
                new Claim("oid", "user-oid-456")
            }));
        }

        private static DomainRegistrationEntity CreateTestDomainRegistration(string id = "test-reg-123",
            DomainRegistrationStatus status = DomainRegistrationStatus.Pending)
        {
            return new DomainRegistrationEntity
            {
                id = id,
                Upn = "author@example.com",
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "mysite"
                },
                ContactInformation = new ContactInformationEntity
                {
                    FirstName = "John",
                    LastName = "Doe",
                    EmailAddress = "john@example.com",
                    Address = "123 Main St",
                    City = "Anytown",
                    State = "CA",
                    Country = "USA",
                    ZipCode = "12345",
                    TelephoneNumber = "+1-555-123-4567"
                },
                Status = status,
                CreatedAt = DateTime.UtcNow
            };
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AdminDomainRegistrationFunction(
                null!,
                _mockJwtValidationService.Object,
                _mockDomainRegistrationRepository.Object,
                _mockFrontDoorService.Object,
                _mockWhmcsService.Object,
                _mockDnsZoneService.Object));
        }

        [Fact]
        public void Constructor_WithNullJwtValidationService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AdminDomainRegistrationFunction(
                _mockLogger.Object,
                null!,
                _mockDomainRegistrationRepository.Object,
                _mockFrontDoorService.Object,
                _mockWhmcsService.Object,
                _mockDnsZoneService.Object));
        }

        [Fact]
        public void Constructor_WithNullDomainRegistrationRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AdminDomainRegistrationFunction(
                _mockLogger.Object,
                _mockJwtValidationService.Object,
                null!,
                _mockFrontDoorService.Object,
                _mockWhmcsService.Object,
                _mockDnsZoneService.Object));
        }

        [Fact]
        public void Constructor_WithNullFrontDoorService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AdminDomainRegistrationFunction(
                _mockLogger.Object,
                _mockJwtValidationService.Object,
                _mockDomainRegistrationRepository.Object,
                null!,
                _mockWhmcsService.Object,
                _mockDnsZoneService.Object));
        }

        [Fact]
        public void Constructor_WithNullWhmcsService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AdminDomainRegistrationFunction(
                _mockLogger.Object,
                _mockJwtValidationService.Object,
                _mockDomainRegistrationRepository.Object,
                _mockFrontDoorService.Object,
                null!,
                _mockDnsZoneService.Object));
        }

        [Fact]
        public void Constructor_WithNullDnsZoneService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AdminDomainRegistrationFunction(
                _mockLogger.Object,
                _mockJwtValidationService.Object,
                _mockDomainRegistrationRepository.Object,
                _mockFrontDoorService.Object,
                _mockWhmcsService.Object,
                null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            var function = new AdminDomainRegistrationFunction(
                _mockLogger.Object,
                _mockJwtValidationService.Object,
                _mockDomainRegistrationRepository.Object,
                _mockFrontDoorService.Object,
                _mockWhmcsService.Object,
                _mockDnsZoneService.Object);

            Assert.NotNull(function);
        }

        #endregion

        #region AdminCompleteDomainRegistration Tests

        [Fact]
        public async Task AdminCompleteDomainRegistration_WithNoAuthHeader_Returns401()
        {
            // Arrange - request has no Authorization header
            var req = CreateHttpRequestWithoutAuth();

            // Act
            var result = await _function.AdminCompleteDomainRegistration(req, "test-reg-123");

            // Assert - JwtAuthenticationHelper returns 401 when Authorization header is absent
            var objectResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, objectResult.StatusCode);
        }

        [Fact]
        public async Task AdminCompleteDomainRegistration_NonAdminUser_Returns403()
        {
            // Arrange - valid JWT but no Admin role
            var nonAdminUser = CreateNonAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(nonAdminUser);

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminCompleteDomainRegistration(req, "test-reg-123");

            // Assert
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);

            // No provisioning calls should have been made
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
        }

        [Fact]
        public async Task AdminCompleteDomainRegistration_RegistrationNotFound_Returns404()
        {
            // Arrange
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            _mockDomainRegistrationRepository.Setup(x => x.GetByIdCrossPartitionAsync("non-existent-id"))
                                             .ReturnsAsync((DomainRegistrationEntity?)null);

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminCompleteDomainRegistration(req, "non-existent-id");

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        [Fact]
        public async Task AdminCompleteDomainRegistration_AlreadyCompleted_Returns409()
        {
            // Arrange
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            var completedRegistration = CreateTestDomainRegistration("test-reg-123", DomainRegistrationStatus.Completed);
            _mockDomainRegistrationRepository.Setup(x => x.GetByIdCrossPartitionAsync("test-reg-123"))
                                             .ReturnsAsync(completedRegistration);

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminCompleteDomainRegistration(req, "test-reg-123");

            // Assert - no re-provisioning should occur
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
        }

        [Fact]
        public async Task AdminCompleteDomainRegistration_AlreadyCancelled_Returns409()
        {
            // Arrange
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            var cancelledRegistration = CreateTestDomainRegistration("test-reg-123", DomainRegistrationStatus.Cancelled);
            _mockDomainRegistrationRepository.Setup(x => x.GetByIdCrossPartitionAsync("test-reg-123"))
                                             .ReturnsAsync(cancelledRegistration);

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminCompleteDomainRegistration(req, "test-reg-123");

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task AdminCompleteDomainRegistration_AllStepsSucceed_Returns200WithCompletedStatus()
        {
            // Arrange
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            var registration = CreateTestDomainRegistration("test-reg-123");
            _mockDomainRegistrationRepository.Setup(x => x.GetByIdCrossPartitionAsync("test-reg-123"))
                                             .ReturnsAsync(registration);

            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(registration)).ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(registration)).ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.GetNameServersAsync("mysite.com"))
                               .ReturnsAsync(new[] { "ns1.azure.com", "ns2.azure.com" });
            _mockWhmcsService.Setup(x => x.UpdateNameServersAsync("mysite.com", It.IsAny<string[]>()))
                             .ReturnsAsync(true);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(registration)).ReturnsAsync(true);

            var completedRegistration = CreateTestDomainRegistration("test-reg-123", DomainRegistrationStatus.Completed);
            _mockDomainRegistrationRepository.Setup(x => x.UpdateAsync(It.IsAny<DomainRegistrationEntity>()))
                                             .ReturnsAsync(completedRegistration);

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminCompleteDomainRegistration(req, "test-reg-123");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            var response = Assert.IsType<DomainRegistrationResponse>(ok.Value);
            Assert.Equal(DomainRegistrationStatus.Completed, response.Status);

            // Verify all provisioning calls were made
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(registration), Times.Once);
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(registration), Times.Once);
            _mockDnsZoneService.Verify(x => x.GetNameServersAsync("mysite.com"), Times.Once);
            _mockWhmcsService.Verify(x => x.UpdateNameServersAsync("mysite.com", It.IsAny<string[]>()), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(registration), Times.Once);
            _mockDomainRegistrationRepository.Verify(x => x.UpdateAsync(It.Is<DomainRegistrationEntity>(
                r => r.Status == DomainRegistrationStatus.Completed)), Times.Once);
        }

        [Fact]
        public async Task AdminCompleteDomainRegistration_WhmcsFails_Returns200WithInProgressStatus()
        {
            // Arrange
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            var registration = CreateTestDomainRegistration("test-reg-123");
            _mockDomainRegistrationRepository.Setup(x => x.GetByIdCrossPartitionAsync("test-reg-123"))
                                             .ReturnsAsync(registration);

            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(registration)).ReturnsAsync(false);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(registration)).ReturnsAsync(true);

            var inProgressRegistration = CreateTestDomainRegistration("test-reg-123", DomainRegistrationStatus.InProgress);
            _mockDomainRegistrationRepository.Setup(x => x.UpdateAsync(It.IsAny<DomainRegistrationEntity>()))
                                             .ReturnsAsync(inProgressRegistration);

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminCompleteDomainRegistration(req, "test-reg-123");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            var response = Assert.IsType<DomainRegistrationResponse>(ok.Value);
            Assert.Equal(DomainRegistrationStatus.InProgress, response.Status);

            // DNS steps should be skipped when WHMCS fails
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);

            // Status saved as InProgress
            _mockDomainRegistrationRepository.Verify(x => x.UpdateAsync(It.Is<DomainRegistrationEntity>(
                r => r.Status == DomainRegistrationStatus.InProgress)), Times.Once);
        }

        [Fact]
        public async Task AdminCompleteDomainRegistration_FrontDoorFails_Returns200WithInProgressStatus()
        {
            // Arrange
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            var registration = CreateTestDomainRegistration("test-reg-123");
            _mockDomainRegistrationRepository.Setup(x => x.GetByIdCrossPartitionAsync("test-reg-123"))
                                             .ReturnsAsync(registration);

            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(registration)).ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(registration)).ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.GetNameServersAsync("mysite.com"))
                               .ReturnsAsync(new[] { "ns1.azure.com", "ns2.azure.com" });
            _mockWhmcsService.Setup(x => x.UpdateNameServersAsync("mysite.com", It.IsAny<string[]>()))
                             .ReturnsAsync(true);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(registration)).ReturnsAsync(false);

            var inProgressRegistration = CreateTestDomainRegistration("test-reg-123", DomainRegistrationStatus.InProgress);
            _mockDomainRegistrationRepository.Setup(x => x.UpdateAsync(It.IsAny<DomainRegistrationEntity>()))
                                             .ReturnsAsync(inProgressRegistration);

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminCompleteDomainRegistration(req, "test-reg-123");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            var response = Assert.IsType<DomainRegistrationResponse>(ok.Value);
            Assert.Equal(DomainRegistrationStatus.InProgress, response.Status);

            // Status saved as InProgress
            _mockDomainRegistrationRepository.Verify(x => x.UpdateAsync(It.Is<DomainRegistrationEntity>(
                r => r.Status == DomainRegistrationStatus.InProgress)), Times.Once);
        }

        [Fact]
        public async Task AdminCompleteDomainRegistration_DnsFails_Returns200WithInProgressStatus()
        {
            // Arrange
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            var registration = CreateTestDomainRegistration("test-reg-123");
            _mockDomainRegistrationRepository.Setup(x => x.GetByIdCrossPartitionAsync("test-reg-123"))
                                             .ReturnsAsync(registration);

            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(registration)).ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(registration)).ReturnsAsync(false); // DNS zone fails
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(registration)).ReturnsAsync(true);

            var inProgressRegistration = CreateTestDomainRegistration("test-reg-123", DomainRegistrationStatus.InProgress);
            _mockDomainRegistrationRepository.Setup(x => x.UpdateAsync(It.IsAny<DomainRegistrationEntity>()))
                                             .ReturnsAsync(inProgressRegistration);

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminCompleteDomainRegistration(req, "test-reg-123");

            // Assert - DNS failure means status is InProgress even if FrontDoor succeeds
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            var response = Assert.IsType<DomainRegistrationResponse>(ok.Value);
            Assert.Equal(DomainRegistrationStatus.InProgress, response.Status);

            _mockDomainRegistrationRepository.Verify(x => x.UpdateAsync(It.Is<DomainRegistrationEntity>(
                r => r.Status == DomainRegistrationStatus.InProgress)), Times.Once);
        }

        [Fact]
        public async Task AdminCompleteDomainRegistration_WithEmptyRegistrationId_Returns400()
        {
            // Arrange - admin user authenticated successfully
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminCompleteDomainRegistration(req, "");

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        #endregion

        #region Admin Role Check Tests

        [Fact]
        public void AdminRoleCheck_UserWithAdminRoleClaim_IsAdmin()
        {
            // Arrange
            var adminUser = CreateAdminUser();

            // Act - check admin role as the function would
            var isAdmin = adminUser.FindAll("roles").Any(c => c.Value == "Admin")
                       || adminUser.IsInRole("Admin");

            // Assert
            Assert.True(isAdmin, "User with 'Admin' role claim should be recognized as admin");
        }

        [Fact]
        public void AdminRoleCheck_UserWithoutAdminRoleClaim_IsNotAdmin()
        {
            // Arrange
            var nonAdminUser = CreateNonAdminUser();

            // Act
            var isAdmin = nonAdminUser.FindAll("roles").Any(c => c.Value == "Admin")
                       || nonAdminUser.IsInRole("Admin");

            // Assert
            Assert.False(isAdmin, "User without 'Admin' role claim should not be recognized as admin");
        }

        [Fact]
        public void AdminRoleCheck_UserWithOtherRoleClaim_IsNotAdmin()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("upn", "user@example.com"),
                new Claim("roles", "Editor")
            }));

            // Act
            var isAdmin = user.FindAll("roles").Any(c => c.Value == "Admin")
                       || user.IsInRole("Admin");

            // Assert
            Assert.False(isAdmin, "User with 'Editor' role but not 'Admin' should not be recognized as admin");
        }

        #endregion
    }
}
