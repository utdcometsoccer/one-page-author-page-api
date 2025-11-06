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

    public class GoogleDomainRegistrationFunctionTests
    {
        private readonly Mock<ILogger<GoogleDomainRegistrationFunction>> _mockLogger;
        private readonly Mock<IGoogleDomainsService> _mockGoogleDomainsService;
        private readonly GoogleDomainRegistrationFunction _function;

        public GoogleDomainRegistrationFunctionTests()
        {
            _mockLogger = new Mock<ILogger<GoogleDomainRegistrationFunction>>();
            _mockGoogleDomainsService = new Mock<IGoogleDomainsService>();
            _function = new GoogleDomainRegistrationFunction(_mockLogger.Object, _mockGoogleDomainsService.Object);
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
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GoogleDomainRegistrationFunction received no documents")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            
            _mockGoogleDomainsService.Verify(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
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
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GoogleDomainRegistrationFunction received no documents")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            
            _mockGoogleDomainsService.Verify(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
        }

        [Fact]
        public async Task Run_WithPendingDomainRegistration_RegistersDomainSuccessfully()
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

            _mockGoogleDomainsService.Setup(x => x.RegisterDomainAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockGoogleDomainsService.Verify(x => x.RegisterDomainAsync(domainRegistration), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing domain registration test-id-123 for domain example.com")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully initiated domain registration for example.com via Google Domains API")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(DomainStatus.InProgress)]
        [InlineData(DomainStatus.Completed)]
        [InlineData(DomainStatus.Failed)]
        [InlineData(DomainStatus.Cancelled)]
        public async Task Run_WithNonPendingStatus_SkipsDomainRegistration(DomainStatus status)
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
            _mockGoogleDomainsService.Verify(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Skipping domain skip-example.com - status is {status}, expected Pending")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WhenDomainRegistrationFails_LogsError()
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

            _mockGoogleDomainsService.Setup(x => x.RegisterDomainAsync(domainRegistration))
                .ReturnsAsync(false);

            // Act
            await _function.Run(input);

            // Assert
            _mockGoogleDomainsService.Verify(x => x.RegisterDomainAsync(domainRegistration), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to register domain fail-example.com via Google Domains API")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WhenGoogleDomainsServiceThrowsException_LogsErrorAndContinues()
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

            _mockGoogleDomainsService.Setup(x => x.RegisterDomainAsync(domainRegistration1))
                .ThrowsAsync(new InvalidOperationException("Google Domains API error"));
            _mockGoogleDomainsService.Setup(x => x.RegisterDomainAsync(domainRegistration2))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockGoogleDomainsService.Verify(x => x.RegisterDomainAsync(domainRegistration1), Times.Once);
            _mockGoogleDomainsService.Verify(x => x.RegisterDomainAsync(domainRegistration2), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing domain registration test-id-exception")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully initiated domain registration for success-example.com via Google Domains API")),
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
            _mockGoogleDomainsService.Verify(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Skipping registration (null) - missing domain information")),
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
            _mockGoogleDomainsService.Verify(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Skipping registration test-id-null-domain - missing domain information")),
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
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "org",
                    SecondLevelDomain = "second-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration1, domainRegistration2 };

            _mockGoogleDomainsService.Setup(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockGoogleDomainsService.Verify(x => x.RegisterDomainAsync(domainRegistration1), Times.Once);
            _mockGoogleDomainsService.Verify(x => x.RegisterDomainAsync(domainRegistration2), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GoogleDomainRegistrationFunction processing 2 domain registration(s)")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GoogleDomainRegistrationFunction completed processing 2 registration(s)")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WithMixedStatuses_ProcessesOnlyPendingRegistrations()
        {
            // Arrange
            var pendingRegistration = new DomainRegistrationEntity
            {
                id = "test-id-pending",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "pending-example"
                }
            };
            var completedRegistration = new DomainRegistrationEntity
            {
                id = "test-id-completed",
                Status = DomainStatus.Completed,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "completed-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { pendingRegistration, completedRegistration };

            _mockGoogleDomainsService.Setup(x => x.RegisterDomainAsync(pendingRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockGoogleDomainsService.Verify(x => x.RegisterDomainAsync(pendingRegistration), Times.Once);
            _mockGoogleDomainsService.Verify(x => x.RegisterDomainAsync(completedRegistration), Times.Never);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully initiated domain registration for pending-example.com via Google Domains API")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Skipping domain completed-example.com - status is Completed, expected Pending")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}