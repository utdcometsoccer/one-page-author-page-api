using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace OnePageAuthor.Test.Country
{
    public class CountryServiceTests
    {
        private readonly Mock<ICountryRepository> _repositoryMock;
        private readonly Mock<ILogger<CountryService>> _loggerMock;
        private readonly CountryService _service;

        public CountryServiceTests()
        {
            _repositoryMock = new Mock<ICountryRepository>();
            _loggerMock = new Mock<ILogger<CountryService>>();
            _service = new CountryService(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new CountryService(null!, _loggerMock.Object));
            Assert.Equal("repository", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new CountryService(_repositoryMock.Object, null!));
            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public async Task GetCountriesByLanguageAsync_WithValidLanguage_ReturnsCountries()
        {
            // Arrange
            var language = "en";
            var expectedCountries = new List<InkStainedWretch.OnePageAuthorAPI.Entities.Country>
            {
                new() { Code = "US", Name = "United States", Language = "en" },
                new() { Code = "CA", Name = "Canada", Language = "en" }
            };
            _repositoryMock.Setup(x => x.GetByLanguageAsync(language))
                .ReturnsAsync(expectedCountries);

            // Act
            var result = await _service.GetCountriesByLanguageAsync(language);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("US", result[0].Code);
            Assert.Equal("CA", result[1].Code);
        }

        [Fact]
        public async Task GetCountriesByLanguageAsync_WithNullLanguage_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetCountriesByLanguageAsync(null!);

            // Assert
            Assert.Empty(result);
            _repositoryMock.Verify(x => x.GetByLanguageAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetCountryByCodeAndLanguageAsync_WithValidParameters_ReturnsCountry()
        {
            // Arrange
            var country = new InkStainedWretch.OnePageAuthorAPI.Entities.Country 
            { 
                Code = "US", 
                Name = "United States", 
                Language = "en" 
            };
            _repositoryMock.Setup(x => x.GetByCodeAndLanguageAsync("US", "en"))
                .ReturnsAsync(country);

            // Act
            var result = await _service.GetCountryByCodeAndLanguageAsync("US", "en");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("US", result.Code);
            Assert.Equal("United States", result.Name);
        }

        [Fact]
        public async Task GetCountryByCodeAndLanguageAsync_WithInvalidCodeLength_ReturnsNull()
        {
            // Act
            var result = await _service.GetCountryByCodeAndLanguageAsync("USA", "en");

            // Assert
            Assert.Null(result);
            _repositoryMock.Verify(x => x.GetByCodeAndLanguageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateCountryAsync_WithValidCountry_CreatesCountry()
        {
            // Arrange
            var country = new InkStainedWretch.OnePageAuthorAPI.Entities.Country 
            { 
                Code = "US", 
                Name = "United States", 
                Language = "en" 
            };
            _repositoryMock.Setup(x => x.ExistsByCodeAndLanguageAsync("US", "en"))
                .ReturnsAsync(false);
            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.Country>()))
                .ReturnsAsync(country);

            // Act
            var result = await _service.CreateCountryAsync(country);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("US", result.Code);
            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<InkStainedWretch.OnePageAuthorAPI.Entities.Country>()), Times.Once);
        }

        [Fact]
        public async Task CreateCountryAsync_WithExistingCountry_ThrowsInvalidOperationException()
        {
            // Arrange
            var country = new InkStainedWretch.OnePageAuthorAPI.Entities.Country 
            { 
                Code = "US", 
                Name = "United States", 
                Language = "en" 
            };
            _repositoryMock.Setup(x => x.ExistsByCodeAndLanguageAsync("US", "en"))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.CreateCountryAsync(country));
        }

        [Fact]
        public async Task CreateCountryAsync_WithNullCountry_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.CreateCountryAsync(null!));
        }

        [Fact]
        public async Task CreateCountryAsync_WithInvalidCode_ThrowsArgumentException()
        {
            // Arrange
            var country = new InkStainedWretch.OnePageAuthorAPI.Entities.Country 
            { 
                Code = "USA", // Invalid - should be 2 characters
                Name = "United States", 
                Language = "en" 
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateCountryAsync(country));
        }

        [Fact]
        public async Task DeleteCountryAsync_WithValidParameters_DeletesCountry()
        {
            // Arrange
            _repositoryMock.Setup(x => x.DeleteAsync("123", "en"))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteCountryAsync("123", "en");

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(x => x.DeleteAsync("123", "en"), Times.Once);
        }

        [Fact]
        public async Task GetAllCountriesAsync_ReturnsAllCountries()
        {
            // Arrange
            var countries = new List<InkStainedWretch.OnePageAuthorAPI.Entities.Country>
            {
                new() { Code = "US", Name = "United States", Language = "en" },
                new() { Code = "ES", Name = "España", Language = "es" }
            };
            _repositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(countries);

            // Act
            var result = await _service.GetAllCountriesAsync();

            // Assert
            Assert.Equal(2, result.Count);
        }
    }
}
