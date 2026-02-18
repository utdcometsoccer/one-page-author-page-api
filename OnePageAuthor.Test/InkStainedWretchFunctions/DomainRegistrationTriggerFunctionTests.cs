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
        private readonly Mock<IWhmcsService> _mockWhmcsService;
        private readonly Mock<IDnsZoneService> _mockDnsZoneService;
        private readonly DomainRegistrationTriggerFunction _function;

        public DomainRegistrationTriggerFunctionTests()
        {
            _mockLogger = new Mock<ILogger<DomainRegistrationTriggerFunction>>();
            _mockFrontDoorService = new Mock<IFrontDoorService>();
            _mockWhmcsService = new Mock<IWhmcsService>();
            _mockDnsZoneService = new Mock<IDnsZoneService>();
            _function = new DomainRegistrationTriggerFunction(
                _mockLogger.Object, 
                _mockFrontDoorService.Object, 
                _mockWhmcsService.Object,
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
                _mockWhmcsService.Object,
                _mockDnsZoneService.Object));
        }

        [Fact]
        public void Constructor_WithNullFrontDoorService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainRegistrationTriggerFunction(
                _mockLogger.Object,
                null!,
                _mockWhmcsService.Object,
                _mockDnsZoneService.Object));
        }

        [Fact]
        public void Constructor_WithNullWhmcsService_ThrowsArgumentNullException()
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
                _mockWhmcsService.Object,
                null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var function = new DomainRegistrationTriggerFunction(
                _mockLogger.Object,
                _mockFrontDoorService.Object,
                _mockWhmcsService.Object,
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
            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration))
                .ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.GetNameServersAsync("example.com"))
                .ReturnsAsync(new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net", "ns3-04.azure-dns.org", "ns4-04.azure-dns.info" });
            _mockWhmcsService.Setup(x => x.UpdateNameServersAsync("example.com", It.IsAny<string[]>()))
                .ReturnsAsync(true);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(domainRegistration), Times.Once);
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration), Times.Once);
            _mockDnsZoneService.Verify(x => x.GetNameServersAsync("example.com"), Times.Once);
            _mockWhmcsService.Verify(x => x.UpdateNameServersAsync("example.com", 
                It.Is<string[]>(ns => ns.Length == 4 && ns[0] == "ns1-04.azure-dns.com")), Times.Once);
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

        #region Name Server Update Tests

        [Fact]
        public async Task Run_AfterSuccessfulWhmcsRegistration_UpdatesNameServers()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-nameserver",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "nameserver-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };
            var expectedNameServers = new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net", "ns3-04.azure-dns.org", "ns4-04.azure-dns.info" };

            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(domainRegistration))
                .ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration))
                .ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.GetNameServersAsync("nameserver-example.com"))
                .ReturnsAsync(expectedNameServers);
            _mockWhmcsService.Setup(x => x.UpdateNameServersAsync("nameserver-example.com", expectedNameServers))
                .ReturnsAsync(true);
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert - verify full flow executed
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(domainRegistration), Times.Once);
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration), Times.Once);
            _mockDnsZoneService.Verify(x => x.GetNameServersAsync("nameserver-example.com"), Times.Once);
            _mockWhmcsService.Verify(x => x.UpdateNameServersAsync("nameserver-example.com", expectedNameServers), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration), Times.Once);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully updated name servers for domain nameserver-example.com in WHMCS")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WhenDnsZoneCreationFails_SkipsNameServerUpdate()
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

            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(domainRegistration))
                .ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration))
                .ReturnsAsync(false); // DNS zone creation fails
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(domainRegistration), Times.Once);
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration), Times.Once);
            _mockDnsZoneService.Verify(x => x.GetNameServersAsync(It.IsAny<string>()), Times.Never);
            _mockWhmcsService.Verify(x => x.UpdateNameServersAsync(It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
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
        public async Task Run_WhenNoNameServersRetrieved_SkipsNameServerUpdate()
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

            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(domainRegistration))
                .ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration))
                .ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.GetNameServersAsync("no-ns-example.com"))
                .ReturnsAsync((string[]?)null); // No name servers returned
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(domainRegistration), Times.Once);
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(domainRegistration), Times.Once);
            _mockDnsZoneService.Verify(x => x.GetNameServersAsync("no-ns-example.com"), Times.Once);
            _mockWhmcsService.Verify(x => x.UpdateNameServersAsync(It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration), Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No name servers retrieved for domain no-ns-example.com")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WhenNameServerUpdateFails_ContinuesToFrontDoor()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-ns-update-fail",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "ns-update-fail-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };
            var nameServers = new[] { "ns1-04.azure-dns.com", "ns2-04.azure-dns.net" };

            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(domainRegistration))
                .ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.EnsureDnsZoneExistsAsync(domainRegistration))
                .ReturnsAsync(true);
            _mockDnsZoneService.Setup(x => x.GetNameServersAsync("ns-update-fail-example.com"))
                .ReturnsAsync(nameServers);
            _mockWhmcsService.Setup(x => x.UpdateNameServersAsync("ns-update-fail-example.com", nameServers))
                .ReturnsAsync(false); // Name server update fails
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockWhmcsService.Verify(x => x.UpdateNameServersAsync("ns-update-fail-example.com", nameServers), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration), Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to update name servers for domain ns-update-fail-example.com in WHMCS")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WhenWhmcsRegistrationFails_SkipsNameServerUpdate()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-reg-fail",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "reg-fail-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };

            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(domainRegistration))
                .ReturnsAsync(false); // WHMCS registration fails
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(domainRegistration), Times.Once);
            _mockDnsZoneService.Verify(x => x.EnsureDnsZoneExistsAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
            _mockDnsZoneService.Verify(x => x.GetNameServersAsync(It.IsAny<string>()), Times.Never);
            _mockWhmcsService.Verify(x => x.UpdateNameServersAsync(It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration), Times.Once);
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

        [Fact]
        public async Task Run_WhenWhmcsRegistrationFails_ContinuesToFrontDoorIntegration()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-whmcs-fail",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "whmcs-fail-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };

            // WHMCS registration fails
            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(domainRegistration))
                .ReturnsAsync(false);
            
            // Front Door should still be called and succeed
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert - verify both services were called
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(domainRegistration), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration), Times.Once);
            
            // Verify WHMCS failure was logged as warning (not error)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to register domain whmcs-fail-example.com via WHMCS API")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Verify Front Door success was still logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully processed domain whmcs-fail-example.com for Front Door")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_WhenWhmcsRegistrationThrows_ContinuesToFrontDoorIntegration()
        {
            // Arrange
            var domainRegistration = new DomainRegistrationEntity
            {
                id = "test-id-whmcs-exception",
                Status = DomainStatus.Pending,
                Domain = new DomainEntity
                {
                    TopLevelDomain = "com",
                    SecondLevelDomain = "whmcs-exception-example"
                }
            };
            var input = new List<DomainRegistrationEntity> { domainRegistration };

            // WHMCS registration throws exception
            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(domainRegistration))
                .ThrowsAsync(new HttpRequestException("WHMCS API unavailable"));
            
            // Front Door should still be called and succeed
            _mockFrontDoorService.Setup(x => x.AddDomainToFrontDoorAsync(domainRegistration))
                .ReturnsAsync(true);

            // Act
            await _function.Run(input);

            // Assert - verify WHMCS was called and Front Door was still called despite exception
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(domainRegistration), Times.Once);
            _mockFrontDoorService.Verify(x => x.AddDomainToFrontDoorAsync(domainRegistration), Times.Once);
            
            // Verify WHMCS exception was caught and logged as error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception while registering domain whmcs-exception-example.com via WHMCS API")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
                
            // Verify warning was also logged about the failure
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to register domain whmcs-exception-example.com via WHMCS API")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}
