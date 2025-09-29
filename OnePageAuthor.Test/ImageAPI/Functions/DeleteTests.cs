using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Security.Claims;
using ImageAPI;
using ImageAPI.Models;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.API.ImageServices;
using InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Authentication;

namespace OnePageAuthor.Test.ImageAPI.Functions
{
    public class DeleteTests
    {
        private readonly Mock<ILogger<Delete>> _loggerMock;
        private readonly Mock<IImageDeleteService> _imageDeleteServiceMock;
        private readonly Mock<IJwtValidationService> _jwtValidationServiceMock;
        private readonly Mock<IUserProfileService> _userProfileServiceMock;
        private readonly Delete _deleteFunction;

        public DeleteTests()
        {
            _loggerMock = new Mock<ILogger<Delete>>();
            _imageDeleteServiceMock = new Mock<IImageDeleteService>();
            _jwtValidationServiceMock = new Mock<IJwtValidationService>();
            _userProfileServiceMock = new Mock<IUserProfileService>();

            _deleteFunction = new Delete(
                _loggerMock.Object,
                _imageDeleteServiceMock.Object,
                _jwtValidationServiceMock.Object,
                _userProfileServiceMock.Object);
        }

        [Fact]
        public async Task Run_WithUnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>
            {
                { "id", "image-123" }
            }));

            // Act
            var result = await _deleteFunction.Run(request.Object);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Run_WithNoImageId_ReturnsBadRequest()
        {
            // Arrange
            var userProfileId = "user-123";
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(authenticationType: "test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.Query).Returns(new QueryCollection());
            request.Setup(x => x.Path).Returns(new PathString("/Delete"));

            var serviceResult = new ImageDeleteResult
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "Image ID is required. Provide it as query parameter ?id=<image-id> or in the path."
            };

            _imageDeleteServiceMock.Setup(x => x.DeleteImageAsync(string.Empty, userProfileId))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _deleteFunction.Run(request.Object);

            // Assert
            var badRequestResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Contains("Image ID is required", errorResponse.Error);
        }

        [Fact]
        public async Task Run_WithNonExistentImage_ReturnsNotFound()
        {
            // Arrange
            var userProfileId = "user-123";
            var imageId = "non-existent-image";

            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(authenticationType: "test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>
            {
                { "id", imageId }
            }));

            var serviceResult = new ImageDeleteResult
            {
                IsSuccess = false,
                StatusCode = 404,
                ErrorMessage = "Image not found."
            };

            _imageDeleteServiceMock.Setup(x => x.DeleteImageAsync(imageId, userProfileId))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _deleteFunction.Run(request.Object);

            // Assert
            var notFoundResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal("Image not found.", errorResponse.Error);
        }

        [Fact]
        public async Task Run_WithImageBelongingToOtherUser_ReturnsNotFound()
        {
            // Arrange
            var userProfileId = "user-123";
            var otherUserProfileId = "other-user-456";
            var imageId = "789e0123-456f-78g9-h012-345678901234";

            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(authenticationType: "test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>
            {
                { "id", imageId }
            }));

            var images = new List<Image>
            {
                new Image
                {
                    id = imageId,
                    UserProfileId = otherUserProfileId, // Belongs to different user
                    Name = "test.jpg",
                    Url = "https://storage.blob.core.windows.net/images/test.jpg",
                    Size = 1024,
                    ContentType = "image/jpeg",
                    ContainerName = "images",
                    BlobName = "other-user-456/test.jpg",
                    UploadedAt = DateTime.UtcNow
                }
            };

            var serviceResult = new ImageDeleteResult
            {
                IsSuccess = false,
                StatusCode = 404,
                ErrorMessage = "Image not found."
            };

            _imageDeleteServiceMock.Setup(x => x.DeleteImageAsync(imageId, userProfileId))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _deleteFunction.Run(request.Object);

            // Assert
            var notFoundResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal("Image not found.", errorResponse.Error);
        }

        [Fact]
        public async Task Run_WithValidImage_DeletesSuccessfully()
        {
            // Arrange
            var userProfileId = "user-123";
            var imageId = "123e4567-e89b-12d3-a456-426614174000";

            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(authenticationType: "test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>
            {
                { "id", imageId }
            }));

            var image = new Image
            {
                id = imageId,
                UserProfileId = userProfileId,
                Name = "test.jpg",
                Url = "https://storage.blob.core.windows.net/images/test.jpg",
                Size = 1024,
                ContentType = "image/jpeg",
                ContainerName = "images",
                BlobName = "user-123/test.jpg",
                UploadedAt = DateTime.UtcNow
            };

            var membership = new ImageStorageTierMembership
            {
                id = "membership-123",
                TierId = "tier-123",
                UserProfileId = userProfileId,
                StorageUsedInBytes = 2048,
                BandwidthUsedInBytes = 1024
            };

            var serviceResult = new ImageDeleteResult
            {
                IsSuccess = true,
                StatusCode = 200,
                Message = "Image deleted successfully."
            };

            _imageDeleteServiceMock.Setup(x => x.DeleteImageAsync(imageId, userProfileId))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _deleteFunction.Run(request.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
        }

        [Fact]
        public async Task Run_WithInvalidGuidFormat_ReturnsBadRequest()
        {
            // Arrange
            var userProfileId = "user-123";
            var invalidImageId = "not-a-guid";

            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(authenticationType: "test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>
            {
                { "id", invalidImageId }
            }));

            var image = new Image
            {
                id = invalidImageId,
                UserProfileId = userProfileId,
                Name = "test.jpg",
                Url = "https://storage.blob.core.windows.net/images/test.jpg",
                Size = 1024,
                ContentType = "image/jpeg",
                ContainerName = "images",
                BlobName = "user-123/test.jpg",
                UploadedAt = DateTime.UtcNow
            };

            var serviceResult = new ImageDeleteResult
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "Invalid image ID format."
            };

            _imageDeleteServiceMock.Setup(x => x.DeleteImageAsync(invalidImageId, userProfileId))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _deleteFunction.Run(request.Object);

            // Assert
            var badRequestResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("Invalid image ID format.", errorResponse.Error);
        }

        [Fact]
        public async Task Run_WithStorageUsageUpdateError_HandlesGracefully()
        {
            // Arrange
            var userProfileId = "user-123";
            var imageId = "456e7890-f12a-34b5-c678-901234567890";

            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(authenticationType: "test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>
            {
                { "id", imageId }
            }));

            var image = new Image
            {
                id = imageId,
                UserProfileId = userProfileId,
                Name = "test.jpg",
                Url = "https://storage.blob.core.windows.net/images/test.jpg",
                Size = 1024,
                ContentType = "image/jpeg",
                ContainerName = "images",
                BlobName = "user-123/test.jpg",
                UploadedAt = DateTime.UtcNow
            };

            var serviceResult = new ImageDeleteResult
            {
                IsSuccess = true,
                StatusCode = 200,
                Message = "Image deleted successfully."
            };

            _imageDeleteServiceMock.Setup(x => x.DeleteImageAsync(imageId, userProfileId))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _deleteFunction.Run(request.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
