using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    public class UploadTests
    {
        private readonly Mock<ILogger<Upload>> _loggerMock;
        private readonly Mock<IImageUploadService> _imageUploadServiceMock;
        private readonly Mock<IJwtValidationService> _jwtValidationServiceMock;
        private readonly Mock<IUserProfileService> _userProfileServiceMock;
        private readonly Upload _uploadFunction;

        public UploadTests()
        {
            _loggerMock = new Mock<ILogger<Upload>>();
            _imageUploadServiceMock = new Mock<IImageUploadService>();
            _jwtValidationServiceMock = new Mock<IJwtValidationService>();
            _userProfileServiceMock = new Mock<IUserProfileService>();

            _uploadFunction = new Upload(
                _loggerMock.Object,
                _imageUploadServiceMock.Object,
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

            // Act
            var result = await _uploadFunction.Run(request.Object);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Run_WithNoUserProfileId_ReturnsUnauthorized()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("name", "Test User"));
            httpContext.User = new ClaimsPrincipal(identity);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = await _uploadFunction.Run(request.Object);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Run_WithNoFormFiles_ReturnsBadRequest()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("oid", "user-123"));
            httpContext.User = new ClaimsPrincipal(identity);

            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.HasFormContentType).Returns(true);
            request.Setup(x => x.Form).Returns(formCollection);

            // Act
            var result = await _uploadFunction.Run(request.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("No file provided in the request.", errorResponse.Error);
        }

        [Fact]
        public async Task Run_WithInvalidFileType_ReturnsBadRequest()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("oid", "user-123"));
            httpContext.User = new ClaimsPrincipal(identity);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(x => x.Length).Returns(1024);
            fileMock.Setup(x => x.ContentType).Returns("text/plain");
            fileMock.Setup(x => x.FileName).Returns("test.txt");

            var fileCollection = new FormFileCollection { fileMock.Object };
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), fileCollection);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.HasFormContentType).Returns(true);
            request.Setup(x => x.Form).Returns(formCollection);

            var serviceResult = new ImageUploadResult
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "Invalid file type. Only image files are allowed."
            };

            _imageUploadServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<IFormFile>(), "user-123"))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _uploadFunction.Run(request.Object);

            // Assert
            var badRequestResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("Invalid file type. Only image files are allowed.", errorResponse.Error);
        }

        [Fact]
        public async Task Run_WithNoTierMembership_ReturnsBadRequest()
        {
            // Arrange
            var userProfileId = "user-123";
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(x => x.Length).Returns(1024);
            fileMock.Setup(x => x.ContentType).Returns("image/jpeg");
            fileMock.Setup(x => x.FileName).Returns("test.jpg");

            var fileCollection = new FormFileCollection { fileMock.Object };
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), fileCollection);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.HasFormContentType).Returns(true);
            request.Setup(x => x.Form).Returns(formCollection);

            var serviceResult = new ImageUploadResult
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "No storage tier assigned to user."
            };

            _imageUploadServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<IFormFile>(), userProfileId))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _uploadFunction.Run(request.Object);

            // Assert
            var badRequestResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("No storage tier assigned to user.", errorResponse.Error);
        }

        [Fact]
        public async Task Run_WithFileTooLarge_ReturnsBadRequest()
        {
            // Arrange
            var userProfileId = "user-123";
            var tierId = "tier-123";

            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            // File is 6MB, exceeds Starter tier limit of 5MB
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(x => x.Length).Returns(6 * 1024 * 1024);
            fileMock.Setup(x => x.ContentType).Returns("image/jpeg");
            fileMock.Setup(x => x.FileName).Returns("large.jpg");

            var fileCollection = new FormFileCollection { fileMock.Object };
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), fileCollection);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.HasFormContentType).Returns(true);
            request.Setup(x => x.Form).Returns(formCollection);

            var membership = new ImageStorageTierMembership
            {
                id = "membership-123",
                TierId = tierId,
                UserProfileId = userProfileId,
                StorageUsedInBytes = 0,
                BandwidthUsedInBytes = 0
            };

            var tier = new ImageStorageTier
            {
                id = tierId,
                Name = "Starter",
                CostInDollars = 0,
                StorageInGB = 5,
                BandwidthInGB = 25
            };

            var serviceResult = new ImageUploadResult
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "File size exceeds limit for your subscription tier (5 MB)."
            };

            _imageUploadServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<IFormFile>(), userProfileId))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _uploadFunction.Run(request.Object);

            // Assert
            var badRequestResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Contains("File size exceeds limit for your subscription tier", errorResponse.Error);
        }

        [Fact]
        public async Task Run_WithStorageQuotaExceeded_ReturnsInsufficientStorage()
        {
            // Arrange
            var userProfileId = "user-123";
            var tierId = "tier-123";

            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(x => x.Length).Returns(1024 * 1024); // 1MB file
            fileMock.Setup(x => x.ContentType).Returns("image/jpeg");
            fileMock.Setup(x => x.FileName).Returns("test.jpg");

            var fileCollection = new FormFileCollection { fileMock.Object };
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), fileCollection);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.HasFormContentType).Returns(true);
            request.Setup(x => x.Form).Returns(formCollection);

            var membership = new ImageStorageTierMembership
            {
                id = "membership-123",
                TierId = tierId,
                UserProfileId = userProfileId,
                StorageUsedInBytes = 5L * 1024 * 1024 * 1024, // Already used full 5GB
                BandwidthUsedInBytes = 0
            };

            var tier = new ImageStorageTier
            {
                id = tierId,
                Name = "Starter",
                CostInDollars = 0,
                StorageInGB = 5,
                BandwidthInGB = 25
            };

            var serviceResult = new ImageUploadResult
            {
                IsSuccess = false,
                StatusCode = 507,
                ErrorMessage = "Storage quota exceeded for your subscription tier."
            };

            _imageUploadServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<IFormFile>(), userProfileId))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _uploadFunction.Run(request.Object);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(507, objectResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(objectResult.Value);
            Assert.Equal("Storage quota exceeded for your subscription tier.", errorResponse.Error);
        }

        [Fact]
        public async Task Run_WithBandwidthLimitExceeded_ReturnsPaymentRequired()
        {
            // Arrange
            var userProfileId = "user-123";
            var tierId = "tier-123";

            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(x => x.Length).Returns(1024 * 1024); // 1MB file
            fileMock.Setup(x => x.ContentType).Returns("image/jpeg");
            fileMock.Setup(x => x.FileName).Returns("test.jpg");

            var fileCollection = new FormFileCollection { fileMock.Object };
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), fileCollection);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.HasFormContentType).Returns(true);
            request.Setup(x => x.Form).Returns(formCollection);

            var membership = new ImageStorageTierMembership
            {
                id = "membership-123",
                TierId = tierId,
                UserProfileId = userProfileId,
                StorageUsedInBytes = 0,
                BandwidthUsedInBytes = 25L * 1024 * 1024 * 1024 // Already used full 25GB bandwidth
            };

            var tier = new ImageStorageTier
            {
                id = tierId,
                Name = "Starter",
                CostInDollars = 0,
                StorageInGB = 5,
                BandwidthInGB = 25
            };

            var serviceResult = new ImageUploadResult
            {
                IsSuccess = false,
                StatusCode = 402,
                ErrorMessage = "Bandwidth limit exceeded for your subscription tier."
            };

            _imageUploadServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<IFormFile>(), userProfileId))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _uploadFunction.Run(request.Object);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(402, objectResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(objectResult.Value);
            Assert.Equal("Bandwidth limit exceeded for your subscription tier.", errorResponse.Error);
        }

        [Fact]
        public async Task Run_WithFileCountLimitExceeded_ReturnsForbidden()
        {
            // Arrange
            var userProfileId = "user-123";
            var tierId = "tier-123";

            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(x => x.Length).Returns(1024);
            fileMock.Setup(x => x.ContentType).Returns("image/jpeg");
            fileMock.Setup(x => x.FileName).Returns("test.jpg");

            var fileCollection = new FormFileCollection { fileMock.Object };
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), fileCollection);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);
            request.Setup(x => x.HasFormContentType).Returns(true);
            request.Setup(x => x.Form).Returns(formCollection);

            var membership = new ImageStorageTierMembership
            {
                id = "membership-123",
                TierId = tierId,
                UserProfileId = userProfileId,
                StorageUsedInBytes = 0,
                BandwidthUsedInBytes = 0
            };

            var tier = new ImageStorageTier
            {
                id = tierId,
                Name = "Starter",
                CostInDollars = 0,
                StorageInGB = 5,
                BandwidthInGB = 25
            };

            var serviceResult = new ImageUploadResult
            {
                IsSuccess = false,
                StatusCode = 403,
                ErrorMessage = "Maximum number of files reached for your subscription tier."
            };

            _imageUploadServiceMock.Setup(x => x.UploadImageAsync(It.IsAny<IFormFile>(), userProfileId))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _uploadFunction.Run(request.Object);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(objectResult.Value);
            Assert.Contains("Maximum number of files reached", errorResponse.Error);
        }
    }
}