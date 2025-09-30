using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace OnePageAuthor.Test.API
{
    public class UserIdentityServiceTests
    {
        private readonly UserIdentityService _service;

        public UserIdentityServiceTests()
        {
            _service = new UserIdentityService();
        }

        [Fact]
        public void GetUserUpn_Success_WithUpnClaim()
        {
            // Arrange
            var upn = "test@example.com";
            var claims = new List<Claim>
            {
                new Claim("upn", upn),
                new Claim("oid", "test-oid-123")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);

            // Act
            var result = _service.GetUserUpn(user);

            // Assert
            Assert.Equal(upn, result);
        }

        [Fact]
        public void GetUserUpn_Success_WithEmailClaim()
        {
            // Arrange
            var email = "user@domain.com";
            var claims = new List<Claim>
            {
                new Claim("email", email),
                new Claim("oid", "test-oid-123")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);

            // Act
            var result = _service.GetUserUpn(user);

            // Assert
            Assert.Equal(email, result);
        }

        [Fact]
        public void GetUserUpn_Success_PreferUpnOverEmail()
        {
            // Arrange
            var upn = "test@example.com";
            var email = "different@domain.com";
            var claims = new List<Claim>
            {
                new Claim("upn", upn),
                new Claim("email", email),
                new Claim("oid", "test-oid-123")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);

            // Act
            var result = _service.GetUserUpn(user);

            // Assert
            Assert.Equal(upn, result); // Should prefer UPN over email
        }

        [Fact]
        public void GetUserUpn_ThrowsException_NullUser()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _service.GetUserUpn(null!));
            
            Assert.Equal("User is not authenticated", exception.Message);
        }

        [Fact]
        public void GetUserUpn_ThrowsException_UserNotAuthenticated()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("upn", "test@example.com")
            };
            var identity = new ClaimsIdentity(claims); // Not authenticated
            var user = new ClaimsPrincipal(identity);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _service.GetUserUpn(user));
            
            Assert.Equal("User is not authenticated", exception.Message);
        }

        [Fact]
        public void GetUserUpn_ThrowsException_MissingUpnAndEmailClaims()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("oid", "test-oid-123")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _service.GetUserUpn(user));
            
            Assert.Equal("User UPN or email claim is required", exception.Message);
        }

        [Fact]
        public void GetUserUpn_ThrowsException_EmptyUpnClaim()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("upn", ""),
                new Claim("oid", "test-oid-123")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _service.GetUserUpn(user));
            
            Assert.Equal("User UPN or email claim is required", exception.Message);
        }

        [Fact]
        public void GetUserUpn_ThrowsException_WhitespaceUpnClaim()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("upn", "   "),
                new Claim("oid", "test-oid-123")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _service.GetUserUpn(user));
            
            Assert.Equal("User UPN or email claim is required", exception.Message);
        }

        [Fact]
        public void GetUserUpn_Success_FallbackToEmailWhenUpnEmpty()
        {
            // Arrange
            var email = "user@domain.com";
            var claims = new List<Claim>
            {
                new Claim("upn", ""), // Empty UPN
                new Claim("email", email),
                new Claim("oid", "test-oid-123")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);

            // Act
            var result = _service.GetUserUpn(user);

            // Assert
            Assert.Equal(email, result);
        }
    }
}