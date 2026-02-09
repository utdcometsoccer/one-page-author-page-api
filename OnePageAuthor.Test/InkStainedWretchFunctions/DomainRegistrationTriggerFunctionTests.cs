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

    /// <summary>
    /// Unit tests for DomainRegistrationTriggerFunction (CosmosDB triggered function for WHMCS and Front Door integration).
    /// </summary>
    public class DomainRegistrationTriggerFunctionTests
    {
        private readonly Mock<ILogger<DomainRegistrationTriggerFunction>> _mockLogger;
        private readonly Mock<IFrontDoorService> _mockFrontDoorService;
        private readonly Mock<IDomainRegistrationService> _mockDomainRegistrationService;
        private readonly Mock<IWhmcsService> _mockWhmcsService;
        private readonly DomainRegistrationTriggerFunction _function;

        public DomainRegistrationTriggerFunctionTests()
        {
            _mockLogger = new Mock<ILogger<DomainRegistrationTriggerFunction>>();
            _mockFrontDoorService = new Mock<IFrontDoorService>();
            _mockDomainRegistrationService = new Mock<IDomainRegistrationService>();
            _mockWhmcsService = new Mock<IWhmcsService>();
            _function = new DomainRegistrationTriggerFunction(
                _mockLogger.Object, 
                _mockFrontDoorService.Object, 
                _mockDomainRegistrationService.Object,
                _mockWhmcsService.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationTriggerFunction(
                null!,
                _mockFrontDoorService.Object,
                _mockDomainRegistrationService.Object,
                _mockWhmcsService.Object));
        }

        [Fact]
        public void Constructor_WithNullFrontDoorService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationTriggerFunction(
                _mockLogger.Object,
                null!,
                _mockDomainRegistrationService.Object,
                _mockWhmcsService.Object));
        }

        [Fact]
        public void Constructor_WithNullDomainRegistrationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationTriggerFunction(
                _mockLogger.Object,
                _mockFrontDoorService.Object,
                null!,
                _mockWhmcsService.Object));
        }

        [Fact]
        public void Constructor_WithNullWhmcsService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationTriggerFunction(
                _mockLogger.Object,
                _mockFrontDoorService.Object,
                _mockDomainRegistrationService.Object,
                null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var function = new DomainRegistrationTriggerFunction(
                _mockLogger.Object,
                _mockFrontDoorService.Object,
                _mockDomainRegistrationService.Object,
                _mockWhmcsService.Object);

            // Assert
            Assert.NotNull(function);
        }

        #endregion

        #region Null/Empty Input Tests

        [Fact]
        public async Task Run_WithNullInput_LogsInformationAndReturns()
        {
            // Arrange
            IReadOnlyList<DomainRegistrationEntity>? input = null;

            // Act
            await _function.Run(input!);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DomainRegistrationTrigger received no documents")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
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
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DomainRegistrationTrigger received no documents")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
        }

        #endregion

        #region Successful Processing Tests

        [Fact]
        public async Task Run_WithPendingDomainRegistration_RegistersDomainViaWhmcsAndAddsToDoorSuccessfully()
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

            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(domainRegistration))
                .ReturnsAsync(true);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(domainRegistration), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration), Times.Once);
            
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
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully registered domain example.com via WHMCS API")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully processed domain example.com for Front Door")),
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

            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()))
                .ReturnsAsync(true);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(It.IsAny<DomainRegistrationEntity>()))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(domainRegistration1), Times.Once);
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(domainRegistration2), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration1), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration2), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DomainRegistrationTrigger processing 2 domain registration(s)")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DomainRegistrationTrigger completed processing 2 registration(s)")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Status Filtering Tests

        [Theory]
        [InlineData(DomainStatus.InProgress)]
        [InlineData(DomainStatus.Completed)]
        [InlineData(DomainStatus.Failed)]
        [InlineData(DomainStatus.Cancelled)]
        public async Task Run_WithNonPendingStatus_SkipsFrontDoorAddition(DomainStatus status)
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
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
            
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

            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(pendingRegistration))
                .ReturnsAsync(true);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(pendingRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(pendingRegistration), Times.Once);
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(completedRegistration), Times.Never);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(pendingRegistration), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(completedRegistration), Times.Never);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully processed domain pending-example.com for Front Door")),
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

        #endregion

        #region Null Domain Tests

        [Fact]
        public async Task Run_WithNullDomainRegistration_SkipsProcessing()
        {
            // Arrange
            var input = new List<DomainRegistrationEntity> { null! };

            // Act
            await _function.Run(input);

            // Assert
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
            
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
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Skipping registration test-id-null-domain - missing domain information")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Run_WhenFrontDoorAdditionFails_LogsError()
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

            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(false);

            // Act
            await _function.Run(input);

            // Assert
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to add domain fail-example.com to Front Door")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WhenFrontDoorServiceThrowsException_LogsErrorAndContinues()
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

            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration1))
                .ThrowsAsync(new InvalidOperationException("Front Door API error"));
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration2))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration1), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration2), Times.Once);
            
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
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully processed domain success-example.com for Front Door")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}
