using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Functions;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnePageAuthor.Test.InkStainedWretchFunctions
{
    using DomainRegistrationEntity = InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration;
    using DomainEntity = InkStainedWretch.OnePageAuthorAPI.Entities.Domain;
    using DomainStatus = InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrationStatus;

    public class CreateDnsZoneFunctionTests
    {
        private readonly Mock<ILogger<CreateDnsZoneFunction>> _mockLogger;
        private readonly Mock<IDnsZoneService> _mockDnsZoneService;
        private readonly Mock<IDomainRegistrationService> _mockDomainRegistrationService;
        private readonly CreateDnsZoneFunction _function;

        public CreateDnsZoneFunctionTests()
        {
            _mockLogger = new Mock<ILogger<CreateDnsZoneFunction>>();
            _mockDnsZoneService = new Mock<IDnsZoneService>();
            _mockDomainRegistrationService = new Mock<IDomainRegistrationService>();
            _function = new CreateDnsZoneFunction(_mockLogger.Object, _mockDnsZoneService.Object, _mockDomainRegistrationService.Object);
        }

        [Fact]
        public async Task Run_WithNullInput_LogsInformationAndReturns()
        {
            // Arrange
            IReadOnlyList<DomainRegistrationEntity>? input = null;

            // Act
            await _function.Run(input);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No domain registrations to process")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
        }

        [Fact]
        public async Task Run_WithEmptyInput_LogsInformationAndReturns()
        {
            // Arrange
            var input = new List<DomainRegistrationEntity>();

            // Act
            await _function.Run(input);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No domain registrations to process")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
        }

        [Fact]
        public async Task Run_WithPendingDomainRegistration_CreatesDnsZoneSuccessfully()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-123",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };

            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DNS zone successfully created/verified for domain: example.com")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DNS zone provisioning completed for domain: example.com")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WithInProgressDomainRegistration_CreatesDnsZone()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-456",
                Status = DomainStatus.InProgress,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "test-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };

            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DNS zone successfully created/verified for domain: test-example.com")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(DomainStatus.Completed)]
        [InlineData(DomainStatus.Failed)]
        [InlineData(DomainStatus.Cancelled)]
        public async Task Run_WithInvalidStatus_SkipsDnsZoneCreation(DomainStatus status)
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-skip",
                Status = status,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "skip-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };

            // Act
            await _function.Run(input);

            // Assert
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Domain registration test-id-skip status is {status}, skipping DNS zone creation")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WhenDnsZoneCreationFails_LogsError()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-fail",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "fail-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };

            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration))
                .ReturnsAsync(false);

            // Act
            await _function.Run(input);

            // Assert
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create DNS zone for domain: fail-example.com")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WhenDnsZoneServiceThrowsException_LogsErrorAndContinues()
        {
            // Arrange
            var domainRegistration1 = new DomainRegistrationEntity
            {
                id = "test-id-exception",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "exception-example"
                }
            };
            var domainRegistration2 = new DomainRegistrationEntity
            {
                id = "test-id-success",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "success-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration1, domainRegistration2 };

            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration1))
                .ThrowsAsync(new InvalidOperationException("DNS zone creation failed"));
            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration2))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration1), Times.Once);
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration2), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing domain registration with ID: test-id-exception")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DNS zone successfully created/verified for domain: success-example.com")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WithNullDomainRegistration_SkipsProcessing()
        {
            // Arrange
            var input = new List<DomainRegistrationEntity> { null! };

            // Act
            await _function.Run(input);

            // Assert
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Domain registration or domain is null, skipping")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WithNullDomain_SkipsProcessing()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-null-domain",
                Status = DomainStatus.Pending,
                Domain = null!
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };

            // Act
            await _function.Run(input);

            // Assert
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Domain registration or domain is null, skipping")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WithMultipleDomainRegistrations_ProcessesAll()
        {
            // Arrange
            var domainRegistration1 = new DomainRegistrationEntity
            {
                id = "test-id-1",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "first-example"
                }
            };
            var domainRegistration2 = new DomainRegistrationEntity
            {
                id = "test-id-2",
                Status = DomainStatus.InProgress,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "second-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration1, domainRegistration2 };

            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(It.IsAny<DomainRegistrationEntity>()))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration1), Times.Once);
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration2), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing 2 domain registration(s)")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Completed processing domain registrations")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}