using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.API.ImageServices;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;

namespace OnePageAuthor.Test.ImageAPI.Services
{
    public class ImageStorageTierServiceTests
    {
        private readonly Mock<ILogger<ImageStorageTierService>> _loggerMock;
        private readonly Mock<IImageStorageTierRepository> _tierRepositoryMock;
        private readonly Mock<IImageStorageTierMembershipRepository> _membershipRepositoryMock;
        private readonly ImageStorageTierService _tierService;

        public ImageStorageTierServiceTests()
        {
            _loggerMock = new Mock<ILogger<ImageStorageTierService>>();
            _tierRepositoryMock = new Mock<IImageStorageTierRepository>();
            _membershipRepositoryMock = new Mock<IImageStorageTierMembershipRepository>();
            _tierService = new ImageStorageTierService(_loggerMock.Object, _tierRepositoryMock.Object, _membershipRepositoryMock.Object);
        }

        [Fact]
        public async Task GetUserTierAsync_WithStarterRole_ReturnsStarterTier()
        {
            // Arrange
            var userId = "user-123";
            var user = CreateUserWithRoles(userId, "ImageStorageTier.Starter");
            
            var starterTier = new ImageStorageTier
            {
                id = "tier-1",
                Name = "Starter",
                CostInDollars = 0m,
                StorageInGB = 5m,
                BandwidthInGB = 25m
            };

            var allTiers = new List<ImageStorageTier> { starterTier };
            
            _tierRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(allTiers);
            _tierRepositoryMock.Setup(x => x.GetByNameAsync("Starter"))
                .ReturnsAsync(starterTier);

            // Act
            var result = await _tierService.GetUserTierAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Starter", result.Name);
            Assert.Equal(0m, result.CostInDollars);
        }

        [Fact]
        public async Task GetUserTierAsync_WithProRole_ReturnsProTier()
        {
            // Arrange
            var userId = "user-123";
            var user = CreateUserWithRoles(userId, "ImageStorageTier.Pro");
            
            var proTier = new ImageStorageTier
            {
                id = "tier-2",
                Name = "Pro",
                CostInDollars = 9.99m,
                StorageInGB = 250m,
                BandwidthInGB = 1024m
            };

            var allTiers = new List<ImageStorageTier> { proTier };
            
            _tierRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(allTiers);
            _tierRepositoryMock.Setup(x => x.GetByNameAsync("Pro"))
                .ReturnsAsync(proTier);

            // Act
            var result = await _tierService.GetUserTierAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Pro", result.Name);
            Assert.Equal(9.99m, result.CostInDollars);
        }

        [Fact]
        public async Task GetUserTierAsync_WithNoRole_ReturnsStarterTier()
        {
            // Arrange
            var userId = "user-123";
            var user = CreateUserWithRoles(userId); // No roles
            
            var starterTier = new ImageStorageTier
            {
                id = "tier-1",
                Name = "Starter",
                CostInDollars = 0m,
                StorageInGB = 5m,
                BandwidthInGB = 25m
            };

            var allTiers = new List<ImageStorageTier> { starterTier };

            _tierRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(allTiers);
            _tierRepositoryMock.Setup(x => x.GetByNameAsync("Starter"))
                .ReturnsAsync(starterTier);

            // Act
            var result = await _tierService.GetUserTierAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Starter", result.Name);
            Assert.Equal(0m, result.CostInDollars);
        }

        [Fact]
        public async Task GetUserTierAsync_WithNoRoleAndNoStarterTier_ReturnsLowestCostTier()
        {
            // Arrange
            var userId = "user-123";
            var user = CreateUserWithRoles(userId); // No roles
            
            var basicTier = new ImageStorageTier
            {
                id = "tier-1",
                Name = "Basic",
                CostInDollars = 0m,
                StorageInGB = 5m,
                BandwidthInGB = 25m
            };

            var proTier = new ImageStorageTier
            {
                id = "tier-2",
                Name = "Pro",
                CostInDollars = 9.99m,
                StorageInGB = 250m,
                BandwidthInGB = 1024m
            };

            var allTiers = new List<ImageStorageTier> { proTier, basicTier };

            _tierRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(allTiers);
            _tierRepositoryMock.Setup(x => x.GetByNameAsync("Starter"))
                .ReturnsAsync((ImageStorageTier?)null); // Starter doesn't exist

            // Act
            var result = await _tierService.GetUserTierAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Basic", result.Name); // Should return the lowest cost tier
            Assert.Equal(0m, result.CostInDollars);
        }

        [Fact]
        public async Task GetUserTierAsync_WithMultipleRoles_ReturnsFirstMatchingTier()
        {
            // Arrange
            var userId = "user-123";
            var user = CreateUserWithRoles(userId, "Admin", "ImageStorageTier.Pro", "ImageStorageTier.Elite");
            
            var proTier = new ImageStorageTier
            {
                id = "tier-2",
                Name = "Pro",
                CostInDollars = 9.99m,
                StorageInGB = 250m,
                BandwidthInGB = 1024m
            };

            var allTiers = new List<ImageStorageTier> { proTier };
            
            _tierRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(allTiers);
            _tierRepositoryMock.Setup(x => x.GetByNameAsync("Pro"))
                .ReturnsAsync(proTier);

            // Act
            var result = await _tierService.GetUserTierAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Pro", result.Name); // Should return first matching tier role
        }

        [Fact]
        public async Task GetUserTierByRolesAsync_WithValidRole_ReturnsTier()
        {
            // Arrange
            var userId = "user-123";
            var roles = new[] { "ImageStorageTier.Elite" };
            
            var eliteTier = new ImageStorageTier
            {
                id = "tier-3",
                Name = "Elite",
                CostInDollars = 24.99m,
                StorageInGB = 2048m,
                BandwidthInGB = 10240m
            };

            var allTiers = new List<ImageStorageTier> { eliteTier };
            
            _tierRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(allTiers);
            _tierRepositoryMock.Setup(x => x.GetByNameAsync("Elite"))
                .ReturnsAsync(eliteTier);

            // Act
            var result = await _tierService.GetUserTierByRolesAsync(userId, roles);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Elite", result.Name);
            Assert.Equal(24.99m, result.CostInDollars);
        }

        private ClaimsPrincipal CreateUserWithRoles(string userId, params string[] roles)
        {
            var claims = new List<Claim>
            {
                new Claim("oid", userId)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim("roles", role));
            }

            var identity = new ClaimsIdentity(claims, "test");
            return new ClaimsPrincipal(identity);
        }
    }
}
