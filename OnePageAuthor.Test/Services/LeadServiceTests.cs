using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.Services;
using System.ComponentModel.DataAnnotations;

namespace OnePageAuthor.Test.Services
{
    public class LeadServiceTests
    {
        private readonly Mock<ILeadRepository> _mockLeadRepository;
        private readonly Mock<ILogger<LeadService>> _mockLogger;
        private readonly LeadService _leadService;

        public LeadServiceTests()
        {
            _mockLeadRepository = new Mock<ILeadRepository>();
            _mockLogger = new Mock<ILogger<LeadService>>();
            _leadService = new LeadService(_mockLeadRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateLeadAsync_WithValidRequest_CreatesNewLead()
        {
            // Arrange
            var request = new CreateLeadRequest
            {
                Email = "test@example.com",
                FirstName = "John",
                Source = "landing_page",
                Locale = "en-US",
                ConsentGiven = true
            };

            _mockLeadRepository
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Lead?)null);

            var createdLead = new Lead("test@example.com", "landing_page", "en-US")
            {
                id = "lead-123",
                FirstName = "John",
                ConsentGiven = true
            };

            _mockLeadRepository
                .Setup(r => r.AddAsync(It.IsAny<Lead>()))
                .ReturnsAsync(createdLead);

            // Act
            var result = await _leadService.CreateLeadAsync(request, "192.168.1.1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("lead-123", result.Id);
            Assert.Equal(LeadCreationStatus.Created, result.Status);
            Assert.Equal("Lead successfully created", result.Message);

            _mockLeadRepository.Verify(r => r.GetByEmailAsync("test@example.com", "example.com"), Times.Once);
            _mockLeadRepository.Verify(r => r.AddAsync(It.IsAny<Lead>()), Times.Once);
        }

        [Fact]
        public async Task CreateLeadAsync_WithExistingEmail_ReturnsExisting()
        {
            // Arrange
            var request = new CreateLeadRequest
            {
                Email = "existing@example.com",
                Source = "blog",
                Locale = "en-US"
            };

            var existingLead = new Lead("existing@example.com", "blog", "en-US")
            {
                id = "existing-lead-123"
            };

            _mockLeadRepository
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingLead);

            // Act
            var result = await _leadService.CreateLeadAsync(request, "192.168.1.1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("existing-lead-123", result.Id);
            Assert.Equal(LeadCreationStatus.Existing, result.Status);
            Assert.Equal("Email already registered", result.Message);

            _mockLeadRepository.Verify(r => r.GetByEmailAsync("existing@example.com", "example.com"), Times.Once);
            _mockLeadRepository.Verify(r => r.AddAsync(It.IsAny<Lead>()), Times.Never);
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("@example.com")]
        [InlineData("test@")]
        [InlineData("test")]
        [InlineData("")]
        public async Task CreateLeadAsync_WithInvalidEmail_ThrowsValidationException(string invalidEmail)
        {
            // Arrange
            var request = new CreateLeadRequest
            {
                Email = invalidEmail,
                Source = "landing_page",
                Locale = "en-US"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => 
                _leadService.CreateLeadAsync(request, "192.168.1.1"));
        }

        [Theory]
        [InlineData("invalid_source")]
        [InlineData("")]
        [InlineData("LANDING_PAGE")]
        public async Task CreateLeadAsync_WithInvalidSource_ThrowsValidationException(string invalidSource)
        {
            // Arrange
            var request = new CreateLeadRequest
            {
                Email = "test@example.com",
                Source = invalidSource,
                Locale = "en-US"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => 
                _leadService.CreateLeadAsync(request, "192.168.1.1"));
        }

        [Theory]
        [InlineData("test@example.com", true)]
        [InlineData("user.name@domain.co.uk", true)]
        [InlineData("user+tag@example.com", true)]
        [InlineData("invalid-email", false)]
        [InlineData("@example.com", false)]
        [InlineData("test@", false)]
        [InlineData("", false)]
        public void IsValidEmail_ValidatesEmailCorrectly(string email, bool expectedValid)
        {
            // Act
            var result = _leadService.IsValidEmail(email);

            // Assert
            Assert.Equal(expectedValid, result);
        }

        [Fact]
        public async Task CreateLeadAsync_NormalizesEmailToLowercase()
        {
            // Arrange
            var request = new CreateLeadRequest
            {
                Email = "Test@EXAMPLE.COM",
                Source = "landing_page",
                Locale = "en-US"
            };

            Lead? capturedLead = null;

            _mockLeadRepository
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Lead?)null);

            _mockLeadRepository
                .Setup(r => r.AddAsync(It.IsAny<Lead>()))
                .Callback<Lead>(lead => capturedLead = lead)
                .ReturnsAsync((Lead lead) => 
                {
                    lead.id = "lead-123";
                    return lead;
                });

            // Act
            await _leadService.CreateLeadAsync(request, "192.168.1.1");

            // Assert
            Assert.NotNull(capturedLead);
            Assert.Equal("test@example.com", capturedLead!.Email);
            Assert.Equal("example.com", capturedLead.EmailDomain);

            _mockLeadRepository.Verify(r => r.GetByEmailAsync("test@example.com", "example.com"), Times.Once);
        }

        [Fact]
        public async Task CreateLeadAsync_WithUTMParameters_StoresAllFields()
        {
            // Arrange
            var request = new CreateLeadRequest
            {
                Email = "test@example.com",
                FirstName = "Jane",
                Source = "newsletter",
                LeadMagnet = "author-success-kit",
                UtmSource = "google",
                UtmMedium = "cpc",
                UtmCampaign = "spring-2024",
                Referrer = "https://google.com",
                Locale = "fr-CA",
                ConsentGiven = true
            };

            Lead? capturedLead = null;

            _mockLeadRepository
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Lead?)null);

            _mockLeadRepository
                .Setup(r => r.AddAsync(It.IsAny<Lead>()))
                .Callback<Lead>(lead => capturedLead = lead)
                .ReturnsAsync((Lead lead) => 
                {
                    lead.id = "lead-456";
                    return lead;
                });

            // Act
            await _leadService.CreateLeadAsync(request, "10.0.0.1");

            // Assert
            Assert.NotNull(capturedLead);
            Assert.Equal("Jane", capturedLead!.FirstName);
            Assert.Equal("newsletter", capturedLead.Source);
            Assert.Equal("author-success-kit", capturedLead.LeadMagnet);
            Assert.Equal("google", capturedLead.UtmSource);
            Assert.Equal("cpc", capturedLead.UtmMedium);
            Assert.Equal("spring-2024", capturedLead.UtmCampaign);
            Assert.Equal("https://google.com", capturedLead.Referrer);
            Assert.Equal("fr-CA", capturedLead.Locale);
            Assert.Equal("10.0.0.1", capturedLead.IpAddress);
            Assert.True(capturedLead.ConsentGiven);
            Assert.Equal("pending", capturedLead.EmailServiceStatus);
        }

        [Fact]
        public async Task GetLeadsBySourceAsync_WithValidSource_ReturnsLeads()
        {
            // Arrange
            var source = "landing_page";
            var leads = new List<Lead>
            {
                new Lead("test1@example.com", source, "en-US") { id = "lead-1" },
                new Lead("test2@example.com", source, "en-US") { id = "lead-2" }
            };

            _mockLeadRepository
                .Setup(r => r.GetBySourceAsync(source, null, null))
                .ReturnsAsync(leads);

            // Act
            var result = await _leadService.GetLeadsBySourceAsync(source);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockLeadRepository.Verify(r => r.GetBySourceAsync(source, null, null), Times.Once);
        }

        [Fact]
        public async Task GetLeadsBySourceAsync_WithInvalidSource_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _leadService.GetLeadsBySourceAsync("invalid_source"));
        }
    }
}
