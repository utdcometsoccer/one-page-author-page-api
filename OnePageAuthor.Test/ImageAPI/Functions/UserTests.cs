using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using ImageAPI;
using ImageAPI.Models;
using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.API.ImageServices;
using InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models;

namespace OnePageAuthor.Test.ImageAPI.Functions
{
    public class UserTests
    {
        private readonly Mock<ILogger<global::ImageAPI.User>> _loggerMock;
        private readonly Mock<IUserImageService> _userImageServiceMock;
        private readonly global::ImageAPI.User _userFunction;

        public UserTests()
        {
            _loggerMock = new Mock<ILogger<global::ImageAPI.User>>();
            _userImageServiceMock = new Mock<IUserImageService>();

            _userFunction = new global::ImageAPI.User(
                _loggerMock.Object,
                _userImageServiceMock.Object);
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
            var result = await _userFunction.Run(request.Object);

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
            var result = await _userFunction.Run(request.Object);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Run_WithValidUser_ReturnsImageList()
        {
            // Arrange
            var userProfileId = "user-123";
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);

            var now = DateTime.UtcNow;
            var userImages = new List<InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models.UserImageResponse>
            {
                new InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models.UserImageResponse
                {
                    Id = "image-1",
                    Name = "recent.jpg",
                    Url = "https://storage.blob.core.windows.net/images/recent.jpg",
                    Size = 2048,
                    UploadedAt = now
                },
                new InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models.UserImageResponse
                {
                    Id = "image-2",
                    Name = "older.png",
                    Url = "https://storage.blob.core.windows.net/images/older.png",
                    Size = 1024,
                    UploadedAt = now.AddHours(-1)
                }
            };

            var serviceResult = new UserImagesResult
            {
                IsSuccess = true,
                StatusCode = 200,
                Images = userImages
            };

            _userImageServiceMock.Setup(x => x.GetUserImagesAsync(userProfileId))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _userFunction.Run(request.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseList = Assert.IsAssignableFrom<List<InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models.UserImageResponse>>(okResult.Value);
            
            Assert.Equal(2, responseList.Count);
            
            // Verify sorting by upload date (newest first)
            Assert.Equal("image-1", responseList[0].Id);
            Assert.Equal("recent.jpg", responseList[0].Name);
            Assert.Equal(2048, responseList[0].Size);
            Assert.Equal(now, responseList[0].UploadedAt);
            
            Assert.Equal("image-2", responseList[1].Id);
            Assert.Equal("older.png", responseList[1].Name);
            Assert.Equal(1024, responseList[1].Size);
            Assert.Equal(now.AddHours(-1), responseList[1].UploadedAt);
        }

        [Fact]
        public async Task Run_WithEmptyImageList_ReturnsEmptyArray()
        {
            // Arrange
            var userProfileId = "user-123";
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);

            var serviceResult = new UserImagesResult
            {
                IsSuccess = true,
                StatusCode = 200,
                Images = new List<InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models.UserImageResponse>()
            };

            _userImageServiceMock.Setup(x => x.GetUserImagesAsync(userProfileId))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _userFunction.Run(request.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseList = Assert.IsAssignableFrom<List<InkStainedWretch.OnePageAuthorAPI.API.ImageServices.Models.UserImageResponse>>(okResult.Value);
            Assert.Empty(responseList);
        }

        [Fact]
        public async Task Run_WithRepositoryException_ReturnsInternalServerError()
        {
            // Arrange
            var userProfileId = "user-123";
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity("test");
            identity.AddClaim(new Claim("oid", userProfileId));
            httpContext.User = new ClaimsPrincipal(identity);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.HttpContext).Returns(httpContext);

            _userImageServiceMock.Setup(x => x.GetUserImagesAsync(userProfileId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _userFunction.Run(request.Object);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(objectResult.Value);
            Assert.Equal("Internal server error occurred while retrieving images.", errorResponse.Error);
        }
    }
}