using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using ImageAPI.Models;

namespace OnePageAuthor.Test.ImageAPI.Models
{
    public class ImageEntityTests
    {
        [Fact]
        public void Image_DefaultValues_AreSetCorrectly()
        {
            // Act
            var image = new Image();

            // Assert
            Assert.Equal(string.Empty, image.id);
            Assert.Equal(string.Empty, image.UserProfileId);
            Assert.Equal(string.Empty, image.Name);
            Assert.Equal(string.Empty, image.Url);
            Assert.Equal(0L, image.Size);
            Assert.Equal(string.Empty, image.ContentType);
            Assert.Equal(string.Empty, image.ContainerName);
            Assert.Equal(string.Empty, image.BlobName);
            Assert.True((DateTime.UtcNow - image.UploadedAt).TotalSeconds < 1);
        }

        [Fact]
        public void Image_PropertyAssignment_WorksCorrectly()
        {
            // Arrange
            var imageId = Guid.NewGuid().ToString();
            var userProfileId = "user-123";
            var name = "test.jpg";
            var url = "https://storage.blob.core.windows.net/images/test.jpg";
            var size = 1024L;
            var contentType = "image/jpeg";
            var containerName = "images";
            var blobName = "user-123/test.jpg";
            var uploadedAt = DateTime.UtcNow.AddHours(-1);

            // Act
            var image = new Image
            {
                id = imageId,
                UserProfileId = userProfileId,
                Name = name,
                Url = url,
                Size = size,
                ContentType = contentType,
                ContainerName = containerName,
                BlobName = blobName,
                UploadedAt = uploadedAt
            };

            // Assert
            Assert.Equal(imageId, image.id);
            Assert.Equal(userProfileId, image.UserProfileId);
            Assert.Equal(name, image.Name);
            Assert.Equal(url, image.Url);
            Assert.Equal(size, image.Size);
            Assert.Equal(contentType, image.ContentType);
            Assert.Equal(containerName, image.ContainerName);
            Assert.Equal(blobName, image.BlobName);
            Assert.Equal(uploadedAt, image.UploadedAt);
        }
    }

    public class ImageStorageTierTests
    {
        [Fact]
        public void ImageStorageTier_DefaultValues_AreSetCorrectly()
        {
            // Act
            var tier = new ImageStorageTier { Name = "Test" };

            // Assert
            Assert.Equal(string.Empty, tier.id);
            Assert.Equal("Test", tier.Name);
            Assert.Equal(0m, tier.CostInDollars);
            Assert.Equal(0m, tier.StorageInGB);
            Assert.Equal(0m, tier.BandwidthInGB);
        }

        [Fact]
        public void ImageStorageTier_PropertyAssignment_WorksCorrectly()
        {
            // Arrange
            var tierId = Guid.NewGuid().ToString();
            var name = "Pro";
            var cost = 9.99m;
            var storage = 250m;
            var bandwidth = 1000m;

            // Act
            var tier = new ImageStorageTier
            {
                id = tierId,
                Name = name,
                CostInDollars = cost,
                StorageInGB = storage,
                BandwidthInGB = bandwidth
            };

            // Assert
            Assert.Equal(tierId, tier.id);
            Assert.Equal(name, tier.Name);
            Assert.Equal(cost, tier.CostInDollars);
            Assert.Equal(storage, tier.StorageInGB);
            Assert.Equal(bandwidth, tier.BandwidthInGB);
        }
    }

    public class ImageStorageTierMembershipTests
    {
        [Fact]
        public void ImageStorageTierMembership_DefaultValues_AreSetCorrectly()
        {
            // Act
            var membership = new ImageStorageTierMembership();

            // Assert
            Assert.Equal(string.Empty, membership.id);
            Assert.Equal(string.Empty, membership.TierId);
            Assert.Equal(string.Empty, membership.UserProfileId);
            Assert.Equal(0L, membership.StorageUsedInBytes);
            Assert.Equal(0L, membership.BandwidthUsedInBytes);
        }

        [Fact]
        public void ImageStorageTierMembership_PropertyAssignment_WorksCorrectly()
        {
            // Arrange
            var membershipId = Guid.NewGuid().ToString();
            var tierId = "tier-123";
            var userProfileId = "user-456";
            var storageUsed = 1024L * 1024 * 500; // 500MB
            var bandwidthUsed = 1024L * 1024 * 1024 * 2; // 2GB

            // Act
            var membership = new ImageStorageTierMembership
            {
                id = membershipId,
                TierId = tierId,
                UserProfileId = userProfileId,
                StorageUsedInBytes = storageUsed,
                BandwidthUsedInBytes = bandwidthUsed
            };

            // Assert
            Assert.Equal(membershipId, membership.id);
            Assert.Equal(tierId, membership.TierId);
            Assert.Equal(userProfileId, membership.UserProfileId);
            Assert.Equal(storageUsed, membership.StorageUsedInBytes);
            Assert.Equal(bandwidthUsed, membership.BandwidthUsedInBytes);
        }
    }

    public class ResponseModelTests
    {
        [Fact]
        public void UploadImageResponse_DefaultValues_AreSetCorrectly()
        {
            // Act
            var response = new UploadImageResponse();

            // Assert
            Assert.Equal(string.Empty, response.Id);
            Assert.Equal(string.Empty, response.Url);
            Assert.Equal(string.Empty, response.Name);
            Assert.Equal(0L, response.Size);
        }

        [Fact]
        public void UserImageResponse_DefaultValues_AreSetCorrectly()
        {
            // Act
            var response = new UserImageResponse();

            // Assert
            Assert.Equal(string.Empty, response.Id);
            Assert.Equal(string.Empty, response.Url);
            Assert.Equal(string.Empty, response.Name);
            Assert.Equal(0L, response.Size);
            Assert.Equal(default(DateTime), response.UploadedAt);
        }

        [Fact]
        public void ErrorResponse_DefaultValues_AreSetCorrectly()
        {
            // Act
            var response = new ErrorResponse();

            // Assert
            Assert.Equal(string.Empty, response.Error);
        }

        [Fact]
        public void ResponseModels_PropertyAssignment_WorksCorrectly()
        {
            // Arrange
            var id = "image-123";
            var url = "https://storage.blob.core.windows.net/images/test.jpg";
            var name = "test.jpg";
            var size = 1024L;
            var uploadedAt = DateTime.UtcNow;
            var errorMessage = "Test error";

            // Act
            var uploadResponse = new UploadImageResponse
            {
                Id = id,
                Url = url,
                Name = name,
                Size = size
            };

            var userResponse = new UserImageResponse
            {
                Id = id,
                Url = url,
                Name = name,
                Size = size,
                UploadedAt = uploadedAt
            };

            var errorResponse = new ErrorResponse
            {
                Error = errorMessage
            };

            // Assert
            Assert.Equal(id, uploadResponse.Id);
            Assert.Equal(url, uploadResponse.Url);
            Assert.Equal(name, uploadResponse.Name);
            Assert.Equal(size, uploadResponse.Size);

            Assert.Equal(id, userResponse.Id);
            Assert.Equal(url, userResponse.Url);
            Assert.Equal(name, userResponse.Name);
            Assert.Equal(size, userResponse.Size);
            Assert.Equal(uploadedAt, userResponse.UploadedAt);

            Assert.Equal(errorMessage, errorResponse.Error);
        }
    }
}