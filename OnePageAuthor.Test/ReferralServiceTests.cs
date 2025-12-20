using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Services;

namespace OnePageAuthor.Test
{
    public class ReferralServiceTests
    {
        private readonly Mock<IReferralRepository> _mockRepository;
        private readonly Mock<ILogger<ReferralService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly ReferralService _service;

        public ReferralServiceTests()
        {
            _mockRepository = new Mock<IReferralRepository>();
            _mockLogger = new Mock<ILogger<ReferralService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup default configuration
            _mockConfiguration.Setup(c => c["REFERRAL_BASE_URL"])
                .Returns("https://inkstainedwretches.com");

            _service = new ReferralService(
                _mockRepository.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);
        }

        [Fact]
        public void Constructor_ThrowsOnNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new ReferralService(
                null!,
                _mockLogger.Object,
                _mockConfiguration.Object));

            Assert.Throws<ArgumentNullException>(() => new ReferralService(
                _mockRepository.Object,
                null!,
                _mockConfiguration.Object));

            Assert.Throws<ArgumentNullException>(() => new ReferralService(
                _mockRepository.Object,
                _mockLogger.Object,
                null!));
        }

        [Fact]
        public async Task CreateReferralAsync_ThrowsOnNullRequest()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _service.CreateReferralAsync(null!));
        }

        [Fact]
        public async Task CreateReferralAsync_ThrowsOnEmptyReferrerId()
        {
            var request = new CreateReferralRequest
            {
                ReferrerId = "",
                ReferredEmail = "test@example.com"
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateReferralAsync(request));
        }

        [Fact]
        public async Task CreateReferralAsync_ThrowsOnEmptyReferredEmail()
        {
            var request = new CreateReferralRequest
            {
                ReferrerId = "user-123",
                ReferredEmail = ""
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateReferralAsync(request));
        }

        [Fact]
        public async Task CreateReferralAsync_ThrowsOnInvalidEmail()
        {
            var request = new CreateReferralRequest
            {
                ReferrerId = "user-123",
                ReferredEmail = "invalid-email"
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateReferralAsync(request));
        }

        [Fact]
        public async Task CreateReferralAsync_ThrowsWhenEmailAlreadyReferred()
        {
            var request = new CreateReferralRequest
            {
                ReferrerId = "user-123",
                ReferredEmail = "test@example.com"
            };

            _mockRepository.Setup(r => r.ExistsByReferrerAndEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateReferralAsync(request));
        }

        [Fact]
        public async Task CreateReferralAsync_CreatesReferralSuccessfully()
        {
            var request = new CreateReferralRequest
            {
                ReferrerId = "user-123",
                ReferredEmail = "test@example.com"
            };

            _mockRepository.Setup(r => r.ExistsByReferrerAndEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockRepository.Setup(r => r.GetByReferralCodeAsync(It.IsAny<string>()))
                .ReturnsAsync((Referral?)null);

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Referral>()))
                .ReturnsAsync((Referral r) => r);

            var result = await _service.CreateReferralAsync(request);

            Assert.NotNull(result);
            Assert.NotEmpty(result.ReferralCode);
            Assert.Contains(result.ReferralCode, result.ReferralUrl);
            Assert.Contains("https://inkstainedwretches.com/signup?ref=", result.ReferralUrl);

            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Referral>()), Times.Once);
        }

        [Fact]
        public async Task CreateReferralAsync_GeneratesUniqueCodeWhenCollision()
        {
            var request = new CreateReferralRequest
            {
                ReferrerId = "user-123",
                ReferredEmail = "test@example.com"
            };

            _mockRepository.Setup(r => r.ExistsByReferrerAndEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(false);

            // First call returns existing code, second returns null (unique)
            _mockRepository.SetupSequence(r => r.GetByReferralCodeAsync(It.IsAny<string>()))
                .ReturnsAsync(new Referral("other-user", "other@example.com", "EXISTING"))
                .ReturnsAsync((Referral?)null);

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Referral>()))
                .ReturnsAsync((Referral r) => r);

            var result = await _service.CreateReferralAsync(request);

            Assert.NotNull(result);
            Assert.NotEmpty(result.ReferralCode);
            
            // Should have checked for code collision twice
            _mockRepository.Verify(r => r.GetByReferralCodeAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetReferralStatsAsync_ThrowsOnEmptyUserId()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetReferralStatsAsync(""));
        }

        [Fact]
        public async Task GetReferralStatsAsync_ReturnsStatsSuccessfully()
        {
            var userId = "user-123";
            var referrals = new List<Referral>
            {
                new Referral(userId, "test1@example.com", "CODE1") { Status = "Pending" },
                new Referral(userId, "test2@example.com", "CODE2") { Status = "Converted" },
                new Referral(userId, "test3@example.com", "CODE3") { Status = "Converted" },
                new Referral(userId, "test4@example.com", "CODE4") { Status = "Expired" }
            };

            _mockRepository.Setup(r => r.GetByReferrerIdAsync(userId))
                .ReturnsAsync(referrals);

            var result = await _service.GetReferralStatsAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(4, result.TotalReferrals);
            Assert.Equal(2, result.SuccessfulReferrals);
            Assert.Equal(2, result.PendingCredits);
            Assert.Equal(0, result.RedeemedCredits);
        }

        [Fact]
        public async Task GetReferralStatsAsync_ReturnsZeroStats_WhenNoReferrals()
        {
            var userId = "user-123";

            _mockRepository.Setup(r => r.GetByReferrerIdAsync(userId))
                .ReturnsAsync(new List<Referral>());

            var result = await _service.GetReferralStatsAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(0, result.TotalReferrals);
            Assert.Equal(0, result.SuccessfulReferrals);
            Assert.Equal(0, result.PendingCredits);
            Assert.Equal(0, result.RedeemedCredits);
        }

        [Fact]
        public void GenerateReferralCode_ReturnsValidCode()
        {
            var code = _service.GenerateReferralCode();

            Assert.NotNull(code);
            Assert.Equal(8, code.Length);
            Assert.All(code, c => Assert.True(char.IsLetterOrDigit(c)));
            Assert.All(code, c => Assert.True(char.IsUpper(c) || char.IsDigit(c)));
        }

        [Fact]
        public void GenerateReferralUrl_ReturnsValidUrl()
        {
            var code = "ABC12345";
            var url = _service.GenerateReferralUrl(code);

            Assert.NotNull(url);
            Assert.Contains("https://inkstainedwretches.com/signup?ref=ABC12345", url);
        }

        [Fact]
        public void GenerateReferralUrl_UsesDefaultBaseUrl_WhenNotConfigured()
        {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["REFERRAL_BASE_URL"]).Returns((string?)null);

            var service = new ReferralService(
                _mockRepository.Object,
                _mockLogger.Object,
                mockConfig.Object);

            var code = "ABC12345";
            var url = service.GenerateReferralUrl(code);

            Assert.NotNull(url);
            Assert.Contains("https://inkstainedwretches.com/signup?ref=ABC12345", url);
        }
    }
}
