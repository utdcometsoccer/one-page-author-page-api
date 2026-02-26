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
    /// Unit tests for DomainRegistrationTriggerFunction.
    /// The trigger now enqueues WHMCS operations to a Service Bus queue (via IWhmcsQueueService)
    /// rather than calling the WHMCS REST API directly.
    /// Azure DNS zone creation and Front Door operations still happen within the function.
    /// </summary>
    public class DomainRegistrationTriggerFunctionTests
    {
        private readonly Mock<ILogger<DomainRegistrationTriggerFunction>> _mockLogger;
        private readonly Mock<IFrontDoorService> _mockFrontDoorService;
        private readonly Mock<IWhmcsQueueService> _mockWhmcsQueueService;
        private readonly Mock<IDnsZoneService> _mockDnsZoneService;
        private readonly DomainRegistrationTriggerFunction _function;

        public DomainRegistrationTriggerFunctionTests()
        {
            _mockLogger = new Mock<ILogger<DomainRegistrationTriggerFunction>>();
            _mockFrontDoorService = new Mock<IFrontDoorService>();
            _mockWhmcsQueueService = new Mock<IWhmcsQueueService>();
            _mockDnsZoneService = new Mock<IDnsZoneService>();
            _function = new DomainRegistrationTriggerFunction(
                _mockLogger.Object,
                _mockFrontDoorService.Object,
                _mockWhmcsQueueService.Object,
                _mockDnsZoneService.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationTriggerFunction(
                null!,
                _mockFrontDoorService.Object,
                _mockWhmcsQueueService.Object,
                _mockDnsZoneService.Object));
        }

        [Fact]
        public void Constructor_WithNullFrontDoorService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationTriggerFunction(
                _mockLogger.Object,
                null!,
                _mockWhmcsQueueService.Object,
                _mockDnsZoneService.Object));
        }

        [Fact]
        public void Constructor_WithNullWhmcsQueueService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationTriggerFunction(
                _mockLogger.Object,
                _mockFrontDoorService.Object,
                null!,
                _mockDnsZoneService.Object));
        }

        [Fact]
        public void Constructor_WithNullDnsZoneService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationTriggerFunction(
                _mockLogger.Object,
                _mockFrontDoorService.Object,
                _mockWhmcsQueueService.Object,
                null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var function = new DomainRegistrationTriggerFunction(
                _mockLogger.Object,
                _mockFrontDoorService.Object,
                _mockWhmcsQueueService.Object,
                _mockDnsZoneService.Object);

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
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(It.IsAny<DomainRegistrationEntity>(), It.IsAny<string[]>()), Times.Never);
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
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(It.IsAny<DomainRegistrationEntity>(), It.IsAny<string[]>()), Times.Never);
        }

        #endregion

        #region Successful Processing Tests

        [Fact]
        public async Task Run_WithPendingDomainRegistration_EnqueuesWhmcsOperationAndAddsToDoor()
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
            var nameServers = new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net", "ns3-04.azure-dns.org", "ns4-04.azure-dns.info" };

            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration))
                .ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.GetNameServersAsync("example.com"))
                .ReturnsAsync(nameServers);
            _mockWhmcsQueueService.Setup(x => x.EnqueueDomainRegistrationAsync(domainRegistration, nameServers))
                .Returns(Task.CompletedTask);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration), Times.Once);
            _mockDnsZoneService.Verify(x => x.GetNameServersAsync("example.com"), Times.Once);
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(domainRegistration, nameServers), Times.Once);
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
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully enqueued WHMCS registration for domain example.com")),
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

            _mockWhmcsQueueService.Setup(x => x.EnqueueDomainRegistrationAsync(It.IsAny<DomainRegistrationEntity>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(It.IsAny<DomainRegistrationEntity>()))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(domainRegistration1, It.IsAny<string[]>()), Times.Once);
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(domainRegistration2, It.IsAny<string[]>()), Times.Once);
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
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(It.IsAny<DomainRegistrationEntity>(), It.IsAny<string[]>()), Times.Never);

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

            _mockWhmcsQueueService.Setup(x => x.EnqueueDomainRegistrationAsync(pendingRegistration, It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(pendingRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(pendingRegistration, It.IsAny<string[]>()), Times.Once);
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(completedRegistration, It.IsAny<string[]>()), Times.Never);
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

        #region DNS Zone and Name Server Tests

        [Fact]
        public async Task Run_WithDnsZoneReady_EnqueuesWithNameServers()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-ns",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "ns-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };
            var expectedNameServers = new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net", "ns3-04.azure-dns.org", "ns4-04.azure-dns.info" };

            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration))
                .ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.GetNameServersAsync("ns-example.com"))
                .ReturnsAsync(expectedNameServers);
            _mockWhmcsQueueService.Setup(x => x.EnqueueDomainRegistrationAsync(domainRegistration, expectedNameServers))
                .Returns(Task.CompletedTask);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration), Times.Once);
            _mockDnsZoneService.Verify(x => x.GetNameServersAsync("ns-example.com"), Times.Once);
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(domainRegistration,
                It.Is<string[]>(ns => ns.Length == 4 && ns[0] == "ns1-04.azure-dns.com")), Times.Once);
        }

        [Fact]
        public async Task Run_WhenDnsZoneCreationFails_EnqueuesWithEmptyNameServersAndContinues()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-dns-fail",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "dns-fail-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };

            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration))
                .ReturnsAsync(false); // DNS zone creation fails
            _mockWhmcsQueueService.Setup(x => x.EnqueueDomainRegistrationAsync(domainRegistration, It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration), Times.Once);
            _mockDnsZoneService.Verify(x => x.GetNameServersAsync(It.IsAny<string>()), Times.Never);
            // Should still enqueue, but with empty name servers
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(domainRegistration,
                It.Is<string[]>(ns => ns.Length == 0)), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration), Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to ensure DNS zone exists for domain dns-fail-example.com")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WhenNoNameServersRetrieved_EnqueuesWithEmptyNameServers()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-no-ns",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "no-ns-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };

            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration))
                .ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.GetNameServersAsync("no-ns-example.com"))
                .ReturnsAsync((string[]?)null); // No name servers returned
            _mockWhmcsQueueService.Setup(x => x.EnqueueDomainRegistrationAsync(domainRegistration, It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockDnsZoneService.Verify(x => x.GetNameServersAsync("no-ns-example.com"), Times.Once);
            // Enqueue is still called, with an empty array
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(domainRegistration,
                It.Is<string[]>(ns => ns.Length == 0)), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration), Times.Once);
        }

        [Fact]
        public async Task Run_WhenInvalidNameServerCount_LogsWarning()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-invalid-count",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "invalid-count-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };
            var tooFewNameServers = new[] { "ns1.azure.com" }; // Only 1 name server (requires 2-5)

            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration))
                .ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.GetNameServersAsync("invalid-count-example.com"))
                .ReturnsAsync(tooFewNameServers);
            _mockWhmcsQueueService.Setup(x => x.EnqueueDomainRegistrationAsync(domainRegistration, It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert – enqueue is still called, but a warning about name server count was logged
            _mockDnsZoneService.Verify(x => x.GetNameServersAsync("invalid-count-example.com"), Times.Once);
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(domainRegistration, It.IsAny<string[]>()), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration), Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("WHMCS requires")),
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
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(It.IsAny<DomainRegistrationEntity>(), It.IsAny<string[]>()), Times.Never);

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
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(It.IsAny<DomainRegistrationEntity>(), It.IsAny<string[]>()), Times.Never);

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

            _mockWhmcsQueueService.Setup(x => x.EnqueueDomainRegistrationAsync(It.IsAny<DomainRegistrationEntity>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);
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

            _mockWhmcsQueueService.Setup(x => x.EnqueueDomainRegistrationAsync(It.IsAny<DomainRegistrationEntity>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);
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

        [Fact]
        public async Task Run_WhenQueueEnqueueFails_LogsErrorAndContinuesToFrontDoor()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-queue-fail",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "queue-fail-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };

            _mockWhmcsQueueService.Setup(x => x.EnqueueDomainRegistrationAsync(domainRegistration, It.IsAny<string[]>()))
                .ThrowsAsync(new InvalidOperationException("Service Bus unavailable"));
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert – Front Door is still called despite queue failure
            _mockWhmcsQueueService.Verify(x => x.EnqueueDomainRegistrationAsync(domainRegistration, It.IsAny<string[]>()), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration), Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to enqueue WHMCS registration for domain queue-fail-example.com")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}
