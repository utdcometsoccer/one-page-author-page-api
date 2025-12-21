using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.Services;

namespace OnePageAuthor.Test.Services
{
    /// <summary>
    /// Unit tests for ExperimentService
    /// </summary>
    public class ExperimentServiceTests
    {
        private readonly Mock<IExperimentRepository> _mockRepository;
        private readonly Mock<ILogger<ExperimentService>> _mockLogger;
        private readonly ExperimentService _service;

        public ExperimentServiceTests()
        {
            _mockRepository = new Mock<IExperimentRepository>();
            _mockLogger = new Mock<ILogger<ExperimentService>>();
            _service = new ExperimentService(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ExperimentService(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ExperimentService(_mockRepository.Object, null!));
        }

        [Fact]
        public async Task GetExperimentsAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _service.GetExperimentsAsync(null!));
        }

        [Fact]
        public async Task GetExperimentsAsync_WithEmptyPage_ThrowsArgumentException()
        {
            // Arrange
            var request = new GetExperimentsRequest { Page = "" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.GetExperimentsAsync(request));
        }

        [Fact]
        public async Task GetExperimentsAsync_WithValidRequest_ReturnsAssignedExperiments()
        {
            // Arrange
            var page = "landing";
            var experiments = new List<Experiment>
            {
                new Experiment
                {
                    id = "exp1",
                    Name = "Test Experiment",
                    Page = page,
                    IsActive = true,
                    Variants = new List<ExperimentVariant>
                    {
                        new ExperimentVariant
                        {
                            Id = "control",
                            Name = "Control",
                            TrafficPercentage = 50,
                            Config = new Dictionary<string, object> { { "color", "blue" } }
                        },
                        new ExperimentVariant
                        {
                            Id = "variant_a",
                            Name = "Variant A",
                            TrafficPercentage = 50,
                            Config = new Dictionary<string, object> { { "color", "red" } }
                        }
                    }
                }
            };

            _mockRepository.Setup(r => r.GetActiveExperimentsByPageAsync(page))
                .ReturnsAsync(experiments);

            var request = new GetExperimentsRequest
            {
                Page = page,
                UserId = "test-user-123"
            };

            // Act
            var response = await _service.GetExperimentsAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("test-user-123", response.SessionId);
            Assert.Single(response.Experiments);
            Assert.Equal("exp1", response.Experiments[0].Id);
            Assert.Equal("Test Experiment", response.Experiments[0].Name);
            Assert.NotEmpty(response.Experiments[0].Variant);
        }

        [Fact]
        public async Task GetExperimentsAsync_WithoutUserId_GeneratesSessionId()
        {
            // Arrange
            var page = "landing";
            _mockRepository.Setup(r => r.GetActiveExperimentsByPageAsync(page))
                .ReturnsAsync(new List<Experiment>());

            var request = new GetExperimentsRequest { Page = page };

            // Act
            var response = await _service.GetExperimentsAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotEmpty(response.SessionId);
            Assert.NotEqual("", response.SessionId);
        }

        [Fact]
        public void AssignVariant_WithNullExperiment_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _service.AssignVariant(null!, "test-key"));
        }

        [Fact]
        public void AssignVariant_WithEmptyBucketingKey_ThrowsArgumentException()
        {
            // Arrange
            var experiment = new Experiment
            {
                id = "exp1",
                Variants = new List<ExperimentVariant>
                {
                    new ExperimentVariant { Id = "control", TrafficPercentage = 100 }
                }
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _service.AssignVariant(experiment, ""));
        }

        [Fact]
        public void AssignVariant_WithNoVariants_ThrowsInvalidOperationException()
        {
            // Arrange
            var experiment = new Experiment
            {
                id = "exp1",
                Variants = new List<ExperimentVariant>()
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _service.AssignVariant(experiment, "test-key"));
        }

        [Fact]
        public void AssignVariant_WithValidExperiment_ReturnsVariant()
        {
            // Arrange
            var experiment = new Experiment
            {
                id = "exp1",
                Variants = new List<ExperimentVariant>
                {
                    new ExperimentVariant
                    {
                        Id = "control",
                        TrafficPercentage = 50,
                        Config = new Dictionary<string, object> { { "color", "blue" } }
                    },
                    new ExperimentVariant
                    {
                        Id = "variant_a",
                        TrafficPercentage = 50,
                        Config = new Dictionary<string, object> { { "color", "red" } }
                    }
                }
            };

            // Act
            var variant = _service.AssignVariant(experiment, "test-user-123");

            // Assert
            Assert.NotNull(variant);
            Assert.Contains(variant.Id, new[] { "control", "variant_a" });
        }

        [Fact]
        public void AssignVariant_WithSameBucketingKey_ReturnsConsistentVariant()
        {
            // Arrange
            var experiment = new Experiment
            {
                id = "exp1",
                Variants = new List<ExperimentVariant>
                {
                    new ExperimentVariant { Id = "control", TrafficPercentage = 50, Config = new() },
                    new ExperimentVariant { Id = "variant_a", TrafficPercentage = 50, Config = new() }
                }
            };

            var bucketingKey = "consistent-user-123";

            // Act - call multiple times with same key
            var variant1 = _service.AssignVariant(experiment, bucketingKey);
            var variant2 = _service.AssignVariant(experiment, bucketingKey);
            var variant3 = _service.AssignVariant(experiment, bucketingKey);

            // Assert - should always get the same variant
            Assert.Equal(variant1.Id, variant2.Id);
            Assert.Equal(variant2.Id, variant3.Id);
        }

        [Fact]
        public void AssignVariant_WithDifferentBucketingKeys_MayReturnDifferentVariants()
        {
            // Arrange
            var experiment = new Experiment
            {
                id = "exp1",
                Variants = new List<ExperimentVariant>
                {
                    new ExperimentVariant { Id = "control", TrafficPercentage = 50, Config = new() },
                    new ExperimentVariant { Id = "variant_a", TrafficPercentage = 50, Config = new() }
                }
            };

            // Act - try with many different keys
            var variants = new HashSet<string>();
            for (int i = 0; i < 100; i++)
            {
                var variant = _service.AssignVariant(experiment, $"user-{i}");
                variants.Add(variant.Id);
            }

            // Assert - should have both variants assigned to at least some users
            // (with 100 users and 50/50 split, extremely unlikely to get only one variant)
            Assert.True(variants.Count >= 2, "Both variants should be assigned to at least some users");
        }

        [Fact]
        public void AssignVariant_With100PercentTraffic_AlwaysReturnsControlVariant()
        {
            // Arrange
            var experiment = new Experiment
            {
                id = "exp1",
                Variants = new List<ExperimentVariant>
                {
                    new ExperimentVariant { Id = "control", TrafficPercentage = 100, Config = new() }
                }
            };

            // Act & Assert - test with multiple keys
            for (int i = 0; i < 10; i++)
            {
                var variant = _service.AssignVariant(experiment, $"user-{i}");
                Assert.Equal("control", variant.Id);
            }
        }

        [Fact]
        public void AssignVariant_WithMultipleVariants_RespectsTrafficAllocation()
        {
            // Arrange
            var experiment = new Experiment
            {
                id = "exp1",
                Variants = new List<ExperimentVariant>
                {
                    new ExperimentVariant { Id = "control", TrafficPercentage = 70, Config = new() },
                    new ExperimentVariant { Id = "variant_a", TrafficPercentage = 20, Config = new() },
                    new ExperimentVariant { Id = "variant_b", TrafficPercentage = 10, Config = new() }
                }
            };

            // Act - assign variants to many users
            var variantCounts = new Dictionary<string, int>
            {
                { "control", 0 },
                { "variant_a", 0 },
                { "variant_b", 0 }
            };

            for (int i = 0; i < 1000; i++)
            {
                var variant = _service.AssignVariant(experiment, $"user-{i}");
                variantCounts[variant.Id]++;
            }

            // Assert - check approximate distribution (with some tolerance)
            // Control should be around 70% (700 out of 1000)
            Assert.InRange(variantCounts["control"], 650, 750);
            // Variant A should be around 20% (200 out of 1000)
            Assert.InRange(variantCounts["variant_a"], 150, 250);
            // Variant B should be around 10% (100 out of 1000)
            Assert.InRange(variantCounts["variant_b"], 50, 150);
        }
    }
}
