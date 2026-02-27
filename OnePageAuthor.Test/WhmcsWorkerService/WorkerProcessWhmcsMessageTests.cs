using System.Text.Json;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace OnePageAuthor.Test.WhmcsWorkerService
{
    using DomainRegistrationEntity = InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistration;
    using DomainEntity = InkStainedWretch.OnePageAuthorAPI.Entities.Domain;
    using MessageEntity = InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations.WhmcsDomainRegistrationMessage;
    using WorkerClass = global::WhmcsWorkerService.Worker;
    using Outcome = global::WhmcsWorkerService.MessageProcessingOutcome;

    /// <summary>
    /// Unit tests for the Worker's core message-processing logic
    /// (dead-letter / abandon / complete semantics and name server validation).
    /// </summary>
    public class WorkerProcessWhmcsMessageTests
    {
        private readonly Mock<ILogger<WorkerClass>> _mockLogger;
        private readonly Mock<IWhmcsService> _mockWhmcsService;
        private readonly WorkerClass _worker;

        private static string MakeMessageJson(
            string? secondLevelDomain = "example",
            string? topLevelDomain = "com",
            string[]? nameServers = null,
            bool nullRegistration = false,
            bool nullDomain = false)
        {
            var msg = new MessageEntity
            {
                MessageId = Guid.NewGuid().ToString(),
                DomainRegistration = nullRegistration ? null! : new DomainRegistrationEntity
                {
                    id = "reg-1",
                    Domain = nullDomain ? null! : new DomainEntity
                    {
                        SecondLevelDomain = secondLevelDomain!,
                        TopLevelDomain = topLevelDomain!,
                    }
                },
                NameServers = nameServers ?? [],
            };
            return JsonSerializer.Serialize(msg);
        }

        public WorkerProcessWhmcsMessageTests()
        {
            _mockLogger = new Mock<ILogger<WorkerClass>>();
            _mockWhmcsService = new Mock<IWhmcsService>();

            var configData = new Dictionary<string, string?>
            {
                ["SERVICE_BUS_CONNECTION_STRING"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==",
                ["SERVICE_BUS_WHMCS_QUEUE_NAME"] = "test-queue",
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            _worker = new WorkerClass(_mockLogger.Object, _mockWhmcsService.Object, configuration);
        }

        #region Dead-letter: Invalid JSON

        [Fact]
        public async Task ProcessWhmcsMessageAsync_WithInvalidJson_ReturnsDeadLetterInvalidJson()
        {
            // Act
            var outcome = await _worker.ProcessWhmcsMessageAsync("not-valid-json", "msg-1");

            // Assert
            Assert.Equal(Outcome.DeadLetterInvalidJson, outcome);
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
        }

        #endregion

        #region Dead-letter: Missing domain data

        [Fact]
        public async Task ProcessWhmcsMessageAsync_WithNullRegistration_ReturnsDeadLetterMissingData()
        {
            var json = MakeMessageJson(nullRegistration: true);

            // Act
            var outcome = await _worker.ProcessWhmcsMessageAsync(json, "msg-2");

            // Assert
            Assert.Equal(Outcome.DeadLetterMissingData, outcome);
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
        }

        [Fact]
        public async Task ProcessWhmcsMessageAsync_WithNullDomain_ReturnsDeadLetterMissingData()
        {
            var json = MakeMessageJson(nullDomain: true);

            // Act
            var outcome = await _worker.ProcessWhmcsMessageAsync(json, "msg-3");

            // Assert
            Assert.Equal(Outcome.DeadLetterMissingData, outcome);
            _mockWhmcsService.Verify(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()), Times.Never);
        }

        #endregion

        #region Abandon: WHMCS registration failure

        [Fact]
        public async Task ProcessWhmcsMessageAsync_WhenRegistrationReturnsFalse_ReturnsAbandon()
        {
            var json = MakeMessageJson();
            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()))
                .ReturnsAsync(false);

            // Act
            var outcome = await _worker.ProcessWhmcsMessageAsync(json, "msg-4");

            // Assert
            Assert.Equal(Outcome.Abandon, outcome);
            _mockWhmcsService.Verify(x => x.UpdateNameServersAsync(It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task ProcessWhmcsMessageAsync_WhenRegistrationThrows_ReturnsAbandon()
        {
            var json = MakeMessageJson();
            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()))
                .ThrowsAsync(new HttpRequestException("WHMCS unavailable"));

            // Act
            var outcome = await _worker.ProcessWhmcsMessageAsync(json, "msg-5");

            // Assert
            Assert.Equal(Outcome.Abandon, outcome);
        }

        #endregion

        #region Complete: Successful registration

        [Fact]
        public async Task ProcessWhmcsMessageAsync_SuccessfulRegistrationWithNoNameServers_ReturnsComplete()
        {
            var json = MakeMessageJson(nameServers: []);
            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()))
                .ReturnsAsync(true);

            // Act
            var outcome = await _worker.ProcessWhmcsMessageAsync(json, "msg-6");

            // Assert
            Assert.Equal(Outcome.Complete, outcome);
            _mockWhmcsService.Verify(x => x.UpdateNameServersAsync(It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task ProcessWhmcsMessageAsync_SuccessfulRegistrationWithNullNameServers_ReturnsComplete()
        {
            // JSON where NameServers is explicitly null
            var raw = new { MessageId = "msg-7", DomainRegistration = new { id = "reg-1", Domain = new { SecondLevelDomain = "example", TopLevelDomain = "com" } }, NameServers = (string[]?)null, EnqueuedAt = DateTime.UtcNow };
            var json = JsonSerializer.Serialize(raw);

            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()))
                .ReturnsAsync(true);

            // Act — should not throw NullReferenceException
            var outcome = await _worker.ProcessWhmcsMessageAsync(json, "msg-7");

            // Assert
            Assert.Equal(Outcome.Complete, outcome);
        }

        [Fact]
        public async Task ProcessWhmcsMessageAsync_SuccessfulRegistrationWithValidNameServers_UpdatesAndReturnsComplete()
        {
            var ns = new[] { "ns1.azure.com", "ns2.azure.net", "ns3.azure.org", "ns4.azure.info" };
            var json = MakeMessageJson(nameServers: ns);
            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()))
                .ReturnsAsync(true);
            _mockWhmcsService.Setup(x => x.UpdateNameServersAsync("example.com", ns))
                .ReturnsAsync(true);

            // Act
            var outcome = await _worker.ProcessWhmcsMessageAsync(json, "msg-8");

            // Assert
            Assert.Equal(Outcome.Complete, outcome);
            _mockWhmcsService.Verify(x => x.UpdateNameServersAsync("example.com", ns), Times.Once);
        }

        [Fact]
        public async Task ProcessWhmcsMessageAsync_WhenNameServerUpdateFails_StillReturnsComplete()
        {
            var ns = new[] { "ns1.azure.com", "ns2.azure.net" };
            var json = MakeMessageJson(nameServers: ns);
            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()))
                .ReturnsAsync(true);
            _mockWhmcsService.Setup(x => x.UpdateNameServersAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ReturnsAsync(false);

            // Act
            var outcome = await _worker.ProcessWhmcsMessageAsync(json, "msg-9");

            // Assert – registration succeeded; NS update failure should not abandon
            Assert.Equal(Outcome.Complete, outcome);
        }

        [Fact]
        public async Task ProcessWhmcsMessageAsync_WhenNameServerUpdateThrows_StillReturnsComplete()
        {
            var ns = new[] { "ns1.azure.com", "ns2.azure.net" };
            var json = MakeMessageJson(nameServers: ns);
            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()))
                .ReturnsAsync(true);
            _mockWhmcsService.Setup(x => x.UpdateNameServersAsync(It.IsAny<string>(), It.IsAny<string[]>()))
                .ThrowsAsync(new HttpRequestException("WHMCS NS error"));

            // Act
            var outcome = await _worker.ProcessWhmcsMessageAsync(json, "msg-10");

            // Assert – registration succeeded; NS update exception should not abandon
            Assert.Equal(Outcome.Complete, outcome);
        }

        #endregion

        #region Name server count validation

        [Theory]
        [InlineData(1)]   // below minimum
        [InlineData(6)]   // above maximum
        public async Task ProcessWhmcsMessageAsync_WithInvalidNameServerCount_SkipsNsUpdateAndReturnsComplete(int count)
        {
            var ns = Enumerable.Range(1, count).Select(i => $"ns{i}.azure.com").ToArray();
            var json = MakeMessageJson(nameServers: ns);
            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()))
                .ReturnsAsync(true);

            // Act
            var outcome = await _worker.ProcessWhmcsMessageAsync(json, $"msg-ns-{count}");

            // Assert
            Assert.Equal(Outcome.Complete, outcome);
            _mockWhmcsService.Verify(x => x.UpdateNameServersAsync(It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
        }

        [Theory]
        [InlineData(2)]   // minimum valid
        [InlineData(5)]   // maximum valid
        public async Task ProcessWhmcsMessageAsync_WithValidNameServerCount_CallsUpdateNameServers(int count)
        {
            var ns = Enumerable.Range(1, count).Select(i => $"ns{i}.azure.com").ToArray();
            var json = MakeMessageJson(nameServers: ns);
            _mockWhmcsService.Setup(x => x.RegisterDomainAsync(It.IsAny<DomainRegistrationEntity>()))
                .ReturnsAsync(true);
            _mockWhmcsService.Setup(x => x.UpdateNameServersAsync(It.IsAny<string>(), ns))
                .ReturnsAsync(true);

            // Act
            var outcome = await _worker.ProcessWhmcsMessageAsync(json, $"msg-ns-valid-{count}");

            // Assert
            Assert.Equal(Outcome.Complete, outcome);
            _mockWhmcsService.Verify(x => x.UpdateNameServersAsync("example.com", ns), Times.Once);
        }

        [Fact]
        public void Worker_Constants_AreCorrect()
        {
            Assert.Equal(2, WorkerClass.MinNameServers);
            Assert.Equal(5, WorkerClass.MaxNameServers);
        }

        #endregion
    }
}
