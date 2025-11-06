using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace OnePageAuthor.Test.Services
{
    /// <summary>
    /// Unit tests for DomainValidationService.
    /// </summary>
    public class DomainValidationServiceTests
    {
        private readonly Mock<ILogger<DomainValidationService>> _mockLogger;
        private readonly DomainValidationService _service;

        public DomainValidationServiceTests()
        {
            _mockLogger = new Mock<ILogger<DomainValidationService>>();
            _service = new DomainValidationService(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DomainValidationService(null!));
        }

        [Fact]
        public void ValidateDomain_WithNullDomain_ReturnsInvalidResult()
        {
            // Act
            var result = _service.ValidateDomain(null!);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Contains("Domain information is required", result.Errors);
        }

        [Fact]
        public void ValidateDomain_WithValidDomain_ReturnsValidResult()
        {
            // Arrange
            var domain = new Domain
            {
                SecondLevelDomain = "example",
                TopLevelDomain = "com"
            };

            // Act
            var result = _service.ValidateDomain(domain);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData("", "Second level domain is required")]
        [InlineData("   ", "Second level domain is required")]
        [InlineData(null, "Second level domain is required")]
        public void ValidateDomain_WithInvalidSecondLevelDomain_ReturnsInvalidResult(string secondLevelDomain, string expectedError)
        {
            // Arrange
            var domain = new Domain
            {
                SecondLevelDomain = secondLevelDomain,
                TopLevelDomain = "com"
            };

            // Act
            var result = _service.ValidateDomain(domain);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("a", "Second level domain must be at least 2 characters long")]
        [InlineData("this-is-a-very-long-domain-name-that-exceeds-the-maximum-allowed-length-for-a-domain", "Second level domain cannot exceed 63 characters")]
        public void ValidateDomain_WithInvalidSecondLevelDomainLength_ReturnsInvalidResult(string secondLevelDomain, string expectedError)
        {
            // Arrange
            var domain = new Domain
            {
                SecondLevelDomain = secondLevelDomain,
                TopLevelDomain = "com"
            };

            // Act
            var result = _service.ValidateDomain(domain);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("-invalid", "Invalid second level domain name format")]
        [InlineData("invalid-", "Invalid second level domain name format")]
        [InlineData("inv@lid", "Invalid second level domain name format")]
        [InlineData("inv.alid", "Invalid second level domain name format")]
        public void ValidateDomain_WithInvalidSecondLevelDomainFormat_ReturnsInvalidResult(string secondLevelDomain, string expectedError)
        {
            // Arrange
            var domain = new Domain
            {
                SecondLevelDomain = secondLevelDomain,
                TopLevelDomain = "com"
            };

            // Act
            var result = _service.ValidateDomain(domain);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("www", "Second level domain name is reserved and cannot be used")]
        [InlineData("ftp", "Second level domain name is reserved and cannot be used")]
        [InlineData("mail", "Second level domain name is reserved and cannot be used")]
        [InlineData("admin", "Second level domain name is reserved and cannot be used")]
        public void ValidateDomain_WithReservedSecondLevelDomain_ReturnsInvalidResult(string secondLevelDomain, string expectedError)
        {
            // Arrange
            var domain = new Domain
            {
                SecondLevelDomain = secondLevelDomain,
                TopLevelDomain = "com"
            };

            // Act
            var result = _service.ValidateDomain(domain);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("", "Top level domain is required")]
        [InlineData("   ", "Top level domain is required")]
        [InlineData(null, "Top level domain is required")]
        public void ValidateDomain_WithInvalidTopLevelDomain_ReturnsInvalidResult(string topLevelDomain, string expectedError)
        {
            // Arrange
            var domain = new Domain
            {
                SecondLevelDomain = "example",
                TopLevelDomain = topLevelDomain
            };

            // Act
            var result = _service.ValidateDomain(domain);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("c", "Invalid top level domain format")]
        [InlineData("toolong", "Invalid top level domain format")]
        [InlineData("co1", "Invalid top level domain format")]
        [InlineData("c@m", "Invalid top level domain format")]
        public void ValidateDomain_WithInvalidTopLevelDomainFormat_ReturnsInvalidResult(string topLevelDomain, string expectedError)
        {
            // Arrange
            var domain = new Domain
            {
                SecondLevelDomain = "example",
                TopLevelDomain = topLevelDomain
            };

            // Act
            var result = _service.ValidateDomain(domain);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("xyz", "Top level domain 'xyz' is not supported")]
        [InlineData("test", "Top level domain 'test' is not supported")]
        public void ValidateDomain_WithUnsupportedTopLevelDomain_ReturnsInvalidResult(string topLevelDomain, string expectedError)
        {
            // Arrange
            var domain = new Domain
            {
                SecondLevelDomain = "example",
                TopLevelDomain = topLevelDomain
            };

            // Act
            var result = _service.ValidateDomain(domain);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(expectedError, result.Errors);
        }

        [Theory]
        [InlineData("valid", true)]
        [InlineData("test123", true)]
        [InlineData("my-domain", true)]
        [InlineData("123valid", true)]
        [InlineData("-invalid", false)]
        [InlineData("invalid-", false)]
        [InlineData("", false)]
        [InlineData("inv@lid", false)]
        public void IsValidDomainName_WithVariousInputs_ReturnsExpectedResult(string domainName, bool expected)
        {
            // Act
            var result = _service.IsValidDomainName(domainName);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("com", true)]
        [InlineData("org", true)]
        [InlineData("co", true)]
        [InlineData("info", true)]
        [InlineData("c", false)]
        [InlineData("toolong", false)]
        [InlineData("co1", false)]
        [InlineData("", false)]
        public void IsValidTopLevelDomain_WithVariousInputs_ReturnsExpectedResult(string tld, bool expected)
        {
            // Act
            var result = _service.IsValidTopLevelDomain(tld);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ValidateDomain_WithMultipleErrors_ReturnsAllErrors()
        {
            // Arrange
            var domain = new Domain
            {
                SecondLevelDomain = "",
                TopLevelDomain = ""
            };

            // Act
            var result = _service.ValidateDomain(domain);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Second level domain is required", result.Errors);
            Assert.Contains("Top level domain is required", result.Errors);
            Assert.True(result.Errors.Count >= 2);
        }

        [Theory]
        [InlineData("com")]
        [InlineData("org")]
        [InlineData("net")]
        [InlineData("edu")]
        [InlineData("io")]
        [InlineData("app")]
        public void ValidateDomain_WithSupportedTlds_ReturnsValidResult(string tld)
        {
            // Arrange
            var domain = new Domain
            {
                SecondLevelDomain = "example",
                TopLevelDomain = tld
            };

            // Act
            var result = _service.ValidateDomain(domain);

            // Assert
            Assert.True(result.IsValid);
        }
    }
}