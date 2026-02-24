using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using InkStainedWretch.OnePageAuthorAPI.Functions;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private readonly Mock<IRoleChecker> _mockRoleChecker;
        private readonly Mock<IDomainRegistrationRepository> _mockDomainRegistrationRepository;
        private readonly Mock<IFrontDoorService> _mockFrontDoorService;
        private readonly Mock<IWhmcsService> _mockWhmcsService;
        private readonly Mock<IDnsZoneService> _mockDnsZoneService;
        private readonly AdminDomainRegistrationFunction _function;

        public AdminDomainRegistrationFunctionTests()
        {
            _mockLogger = new Mock<ILogger<AdminDomainRegistrationFunction>>();
            _mockJwtValidationService = new Mock<IJwtValidationService>();
            _mockRoleChecker = new Mock<IRoleChecker>();
            _mockDomainRegistrationRepository = new Mock<IDomainRegistrationRepository>();
            _mockFrontDoorService = new Mock<IFrontDoorService>();
            _mockWhmcsService = new Mock<IWhmcsService>();
            _mockDnsZoneService = new Mock<IDnsZoneService>();

            // Default: delegate to the real JwtAuthenticationHelper so existing claim-based
            // user stubs (CreateAdminUser / CreateNonAdminUser) still drive the outcome.
            _mockRoleChecker.Setup(x => x.HasRole(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
                            .Returns((ClaimsPrincipal user, string role) => JwtAuthenticationHelper.HasRole(user, role));

            _function = new AdminDomainRegistrationFunction(
                _mockLogger.Object,
                _mockJwtValidationService.Object,
                _mockRoleChecker.Object,
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
                _mockRoleChecker.Object,
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
                _mockRoleChecker.Object,
                _mockDomainRegistrationRepository.Object,
                _mockFrontDoorService.Object,
                _mockWhmcsService.Object,
                _mockDnsZoneService.Object));
        }

        [Fact]
        public void Constructor_WithNullRoleChecker_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AdminDomainRegistrationFunction(
                _mockLogger.Object,
                _mockJwtValidationService.Object,
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
                _mockRoleChecker.Object,
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
                _mockRoleChecker.Object,
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
                _mockRoleChecker.Object,
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
                _mockRoleChecker.Object,
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
                _mockRoleChecker.Object,
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

        #region AdminGetIncompleteDomainRegistrations Tests

        [Fact]
        public async Task AdminGetIncompleteDomainRegistrations_WithNoAuthHeader_Returns401()
        {
            // Arrange
            var req = CreateHttpRequestWithoutAuth();

            // Act
            var result = await _function.AdminGetIncompleteDomainRegistrations(req);

            // Assert
            var objectResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, objectResult.StatusCode);
        }

        [Fact]
        public async Task AdminGetIncompleteDomainRegistrations_NonAdminUser_Returns403()
        {
            // Arrange
            var nonAdminUser = CreateNonAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(nonAdminUser);

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminGetIncompleteDomainRegistrations(req);

            // Assert
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);

            _mockDomainRegistrationRepository.Verify(x => x.GetAllIncompleteAsync(It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task AdminGetIncompleteDomainRegistrations_AdminUser_Returns200WithList()
        {
            // Arrange
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            var registrations = new List<DomainRegistrationEntity>
            {
                CreateTestDomainRegistration("reg-1", DomainRegistrationStatus.Pending),
                CreateTestDomainRegistration("reg-2", DomainRegistrationStatus.InProgress),
                CreateTestDomainRegistration("reg-3", DomainRegistrationStatus.Failed)
            };

            _mockDomainRegistrationRepository.Setup(x => x.GetAllIncompleteAsync(It.IsAny<int?>()))
                                             .ReturnsAsync(registrations);

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminGetIncompleteDomainRegistrations(req);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            var response = Assert.IsAssignableFrom<IEnumerable<DomainRegistrationResponse>>(ok.Value);
            Assert.Equal(3, response.Count());

            _mockDomainRegistrationRepository.Verify(x => x.GetAllIncompleteAsync(It.IsAny<int?>()), Times.Once);
        }

        [Fact]
        public async Task AdminGetIncompleteDomainRegistrations_ResponseHasNullContactInformation()
        {
            // Arrange - contact information must be redacted in admin cross-user listing
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            var registrations = new List<DomainRegistrationEntity>
            {
                CreateTestDomainRegistration("reg-1", DomainRegistrationStatus.Pending)
            };

            _mockDomainRegistrationRepository.Setup(x => x.GetAllIncompleteAsync(It.IsAny<int?>()))
                                             .ReturnsAsync(registrations);

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminGetIncompleteDomainRegistrations(req);

            // Assert - ContactInformation must be null (redacted) to avoid PII exposure
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsAssignableFrom<IEnumerable<DomainRegistrationResponse>>(ok.Value).ToList();
            Assert.Single(response);
            Assert.Null(response[0].ContactInformation);
        }

        [Fact]
        public async Task AdminGetIncompleteDomainRegistrations_SkipsRegistrationsWithNullDomain()
        {
            // Arrange
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            var validReg = CreateTestDomainRegistration("reg-valid");
            var invalidReg = new DomainRegistrationEntity
            {
                id = "reg-no-domain",
                Upn = "user@example.com",
                Domain = null!,
                ContactInformation = new ContactInformationEntity { FirstName = "Test" },
                Status = DomainRegistrationStatus.Pending
            };

            _mockDomainRegistrationRepository.Setup(x => x.GetAllIncompleteAsync(It.IsAny<int?>()))
                                             .ReturnsAsync(new List<DomainRegistrationEntity> { validReg, invalidReg });

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminGetIncompleteDomainRegistrations(req);

            // Assert - only the valid registration is returned
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsAssignableFrom<IEnumerable<DomainRegistrationResponse>>(ok.Value).ToList();
            Assert.Single(response);
            Assert.Equal("reg-valid", response[0].Id);
        }

        [Fact]
        public async Task AdminGetIncompleteDomainRegistrations_WithMaxResultsQueryParam_PassesValueToRepository()
        {
            // Arrange
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            _mockDomainRegistrationRepository.Setup(x => x.GetAllIncompleteAsync(It.IsAny<int?>()))
                                             .ReturnsAsync(Enumerable.Empty<DomainRegistrationEntity>());

            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer valid-token";
            context.Request.QueryString = new QueryString("?maxResults=50");
            var req = context.Request;

            // Act
            await _function.AdminGetIncompleteDomainRegistrations(req);

            // Assert - repository was called with the parsed maxResults value
            _mockDomainRegistrationRepository.Verify(x => x.GetAllIncompleteAsync(50), Times.Once);
        }

        [Fact]
        public async Task AdminGetIncompleteDomainRegistrations_AdminUser_Returns200WithEmptyList_WhenNoneExist()
        {
            // Arrange
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            _mockDomainRegistrationRepository.Setup(x => x.GetAllIncompleteAsync(It.IsAny<int?>()))
                                             .ReturnsAsync(Enumerable.Empty<DomainRegistrationEntity>());

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminGetIncompleteDomainRegistrations(req);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            var response = Assert.IsAssignableFrom<IEnumerable<DomainRegistrationResponse>>(ok.Value);
            Assert.Empty(response);
        }

        [Fact]
        public async Task AdminGetIncompleteDomainRegistrations_RepositoryThrows_Returns500()
        {
            // Arrange
            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            _mockDomainRegistrationRepository.Setup(x => x.GetAllIncompleteAsync(It.IsAny<int?>()))
                                             .ThrowsAsync(new Exception("Database error"));

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await _function.AdminGetIncompleteDomainRegistrations(req);

            // Assert
            var statusResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion

        #region Admin Role Check Tests

        [Fact]
        public void AdminRoleCheck_UserWithAdminRoleClaim_IsAdmin()
        {
            // Arrange
            var adminUser = CreateAdminUser();

            // Act - delegate to the extracted helper as the function does
            var isAdmin = JwtAuthenticationHelper.HasRole(adminUser, "Admin");

            // Assert
            Assert.True(isAdmin, "User with 'Admin' role claim should be recognized as admin");
        }

        [Fact]
        public void AdminRoleCheck_UserWithoutAdminRoleClaim_IsNotAdmin()
        {
            // Arrange
            var nonAdminUser = CreateNonAdminUser();

            // Act
            var isAdmin = JwtAuthenticationHelper.HasRole(nonAdminUser, "Admin");

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
            var isAdmin = JwtAuthenticationHelper.HasRole(user, "Admin");

            // Assert
            Assert.False(isAdmin, "User with 'Editor' role but not 'Admin' should not be recognized as admin");
        }

        /// <summary>
        /// Regression test: JwtSecurityTokenHandler maps the JWT "roles" claim to
        /// <see cref="ClaimTypes.Role"/> (the long-form URI) when processing Entra ID tokens.
        /// HasRole must handle this mapped claim type so that admin users are not incorrectly
        /// denied access with "Admin role required".
        /// </summary>
        [Fact]
        public void HasRole_UserWithMappedClaimTypesRole_IsAdmin()
        {
            // Arrange – simulate a ClaimsPrincipal produced by JwtSecurityTokenHandler where
            // the "roles" JWT claim has been mapped to ClaimTypes.Role (full URI form)
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("upn", "admin@example.com"),
                new Claim(ClaimTypes.Role, "Admin")
            }));

            // Act
            var isAdmin = JwtAuthenticationHelper.HasRole(user, "Admin");

            // Assert
            Assert.True(isAdmin, "User with mapped ClaimTypes.Role 'Admin' claim should be recognized as admin");
        }

        [Fact]
        public void HasRole_UserWithMappedClaimTypesRole_NonAdmin_IsNotAdmin()
        {
            // Arrange – user with a different mapped role
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("upn", "editor@example.com"),
                new Claim(ClaimTypes.Role, "Editor")
            }));

            // Act
            var isAdmin = JwtAuthenticationHelper.HasRole(user, "Admin");

            // Assert
            Assert.False(isAdmin, "User with mapped ClaimTypes.Role 'Editor' claim should not be recognized as admin");
        }

        [Fact]
        public void HasRole_UserWithIsInRole_IsAdmin()
        {
            // Arrange – ClaimsIdentity constructed so that IsInRole works correctly
            var identity = new ClaimsIdentity(
                new[] { new Claim("upn", "admin@example.com") },
                authenticationType: "Test",
                nameType: "upn",
                roleType: "roles");
            identity.AddClaim(new Claim("roles", "Admin"));
            var user = new ClaimsPrincipal(identity);

            // Act
            var isAdmin = JwtAuthenticationHelper.HasRole(user, "Admin");

            // Assert
            Assert.True(isAdmin, "User recognised via IsInRole should be recognized as admin");
        }

        /// <summary>
        /// Verifies that <see cref="AdminDomainRegistrationFunction"/> delegates the role check
        /// to the injected <see cref="IRoleChecker"/>, so a custom implementation can be swapped in.
        /// </summary>
        [Fact]
        public async Task AdminGetIncompleteDomainRegistrations_UsesInjectedRoleChecker()
        {
            // Arrange – override the default mock to always deny (custom policy)
            var customRoleChecker = new Mock<IRoleChecker>();
            customRoleChecker.Setup(x => x.HasRole(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
                             .Returns(false);

            var adminUser = CreateAdminUser();
            _mockJwtValidationService.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                                     .ReturnsAsync(adminUser);

            var function = new AdminDomainRegistrationFunction(
                _mockLogger.Object,
                _mockJwtValidationService.Object,
                customRoleChecker.Object,
                _mockDomainRegistrationRepository.Object,
                _mockFrontDoorService.Object,
                _mockWhmcsService.Object,
                _mockDnsZoneService.Object);

            var req = CreateHttpRequestWithAuth();

            // Act
            var result = await function.AdminGetIncompleteDomainRegistrations(req);

            // Assert – even though the JWT principal has "Admin" claim, the custom checker denied it
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            customRoleChecker.Verify(x => x.HasRole(adminUser, "Admin"), Times.Once);
        }

        #endregion

        #region Function Metadata Tests

        [Fact]
        public void AdminGetIncompleteDomainRegistrations_HasCorrectFunctionName()
        {
            // Verify the [Function] attribute carries the name "AdminGetIncompleteDomainRegistrations".
            // This is a regression test for issue #312 where the metadata in Azure showed
            // "CreateAuthorInvitation" and "ListAuthorInvitations" instead of the expected name.
            var method = typeof(AdminDomainRegistrationFunction)
                .GetMethod("AdminGetIncompleteDomainRegistrations");

            Assert.NotNull(method);

            var functionAttr = method.GetCustomAttribute<FunctionAttribute>();
            Assert.NotNull(functionAttr);
            Assert.Equal("AdminGetIncompleteDomainRegistrations", functionAttr.Name);
        }

        [Fact]
        public void AdminGetIncompleteDomainRegistrations_HasCorrectHttpRoute()
        {
            // Verify the HttpTrigger is bound to GET "management/domain-registrations".
            // This ensures the function responds to /api/management/domain-registrations as documented.
            // Note: the "admin/" prefix is reserved by Azure Functions built-in routes; "management/" is used instead.
            var method = typeof(AdminDomainRegistrationFunction)
                .GetMethod("AdminGetIncompleteDomainRegistrations");

            Assert.NotNull(method);

            var triggerParam = method.GetParameters()
                .FirstOrDefault(p => p.GetCustomAttribute<HttpTriggerAttribute>() != null);

            Assert.NotNull(triggerParam);

            var triggerAttr = triggerParam.GetCustomAttribute<HttpTriggerAttribute>();
            Assert.NotNull(triggerAttr);
            Assert.Equal("management/domain-registrations", triggerAttr.Route);
            Assert.Contains("get", triggerAttr.Methods, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void AdminCompleteDomainRegistration_HasCorrectFunctionName()
        {
            // Verify the [Function] attribute carries the name "AdminCompleteDomainRegistration".
            var method = typeof(AdminDomainRegistrationFunction)
                .GetMethod("AdminCompleteDomainRegistration");

            Assert.NotNull(method);

            var functionAttr = method.GetCustomAttribute<FunctionAttribute>();
            Assert.NotNull(functionAttr);
            Assert.Equal("AdminCompleteDomainRegistration", functionAttr.Name);
        }

        [Fact]
        public void AdminCompleteDomainRegistration_HasCorrectHttpRoute()
        {
            // Verify the HttpTrigger is bound to POST "management/domain-registrations/{registrationId}/complete".
            var method = typeof(AdminDomainRegistrationFunction)
                .GetMethod("AdminCompleteDomainRegistration");

            Assert.NotNull(method);

            var triggerParam = method.GetParameters()
                .FirstOrDefault(p => p.GetCustomAttribute<HttpTriggerAttribute>() != null);

            Assert.NotNull(triggerParam);

            var triggerAttr = triggerParam.GetCustomAttribute<HttpTriggerAttribute>();
            Assert.NotNull(triggerAttr);
            Assert.Equal("management/domain-registrations/{registrationId}/complete", triggerAttr.Route);
            Assert.Contains("post", triggerAttr.Methods, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void AdminDomainRegistrationFunction_DoesNotConflictWithAuthorInvitationFunctionNames()
        {
            // Verify that AdminDomainRegistrationFunction does not use function names belonging
            // to AuthorInvitationFunction. Duplicate [Function] names cause routes to be
            // unregistered and return 404.
            var adminMethods = typeof(AdminDomainRegistrationFunction)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(m => m.GetCustomAttribute<FunctionAttribute>())
                .Where(a => a != null)
                .Select(a => a!.Name)
                .ToList();

            Assert.DoesNotContain("CreateAuthorInvitation", adminMethods);
            Assert.DoesNotContain("ListAuthorInvitations", adminMethods);
        }

        #endregion
    }
}
