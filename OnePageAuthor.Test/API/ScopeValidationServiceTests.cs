using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace OnePageAuthor.Test.API
{
    public class ScopeValidationServiceTests
    {
        private readonly ScopeValidationService _service;

        public ScopeValidationServiceTests()
        {
            _service = new ScopeValidationService();
        }

        private static ClaimsPrincipal CreateUser(params string[] scopes)
        {
            var claims = scopes.Select(s => new Claim("scp", s)).ToList();
            var identity = new ClaimsIdentity(claims, "test");
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public void HasRequiredScope_UserHasExactScope_ReturnsTrue()
        {
            var user = CreateUser("Author.Read");

            var result = _service.HasRequiredScope(user, "Author.Read");

            Assert.True(result);
        }

        [Fact]
        public void HasRequiredScope_UserHasMultipleScopesIncludingRequired_ReturnsTrue()
        {
            // Single "scp" claim containing space-separated scope values (common IdP format)
            var user = CreateUser("openid profile Author.Read");

            var result = _service.HasRequiredScope(user, "Author.Read");

            Assert.True(result);
        }

        [Fact]
        public void HasRequiredScope_ScopeIsSubstringOfAnotherScope_ReturnsFalse()
        {
            // "Author.Read" must not match when only "Author.ReadWrite" is present
            var user = CreateUser("Author.ReadWrite");

            var result = _service.HasRequiredScope(user, "Author.Read");

            Assert.False(result);
        }

        [Fact]
        public void HasRequiredScope_UserHasScopeInSeparateClaims_ReturnsTrue()
        {
            // Some IdPs emit one "scp" claim per scope value
            var claims = new[]
            {
                new Claim("scp", "openid"),
                new Claim("scp", "Author.Read"),
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

            var result = _service.HasRequiredScope(user, "Author.Read");

            Assert.True(result);
        }

        [Fact]
        public void HasRequiredScope_UserHasNoScopes_ReturnsFalse()
        {
            var user = CreateUser();

            var result = _service.HasRequiredScope(user, "Author.Read");

            Assert.False(result);
        }

        [Fact]
        public void HasRequiredScope_UserHasDifferentScope_ReturnsFalse()
        {
            var user = CreateUser("Author.Write");

            var result = _service.HasRequiredScope(user, "Author.Read");

            Assert.False(result);
        }

        [Fact]
        public void HasRequiredScope_UserHasNoScpClaim_ReturnsFalse()
        {
            var claims = new[] { new Claim("upn", "user@example.com") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

            var result = _service.HasRequiredScope(user, "Author.Read");

            Assert.False(result);
        }

        // --- URI-mapped claim type (JwtSecurityTokenHandler with MapInboundClaims = true) ---

        [Fact]
        public void HasRequiredScope_ScopeStoredUnderUriClaimType_ReturnsTrue()
        {
            // JwtSecurityTokenHandler (MapInboundClaims = true, the default) remaps the "scp"
            // JWT claim to the URI form.  HasRequiredScope must recognise both forms.
            var claims = new[] { new Claim(ScopeValidationService.ScopeUriClaimType, "Author.Read") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

            var result = _service.HasRequiredScope(user, "Author.Read");

            Assert.True(result);
        }

        [Fact]
        public void HasRequiredScope_SpaceSeparatedScopeUnderUriClaimType_ReturnsTrue()
        {
            // Token handler may produce a space-delimited scope string under the URI claim type,
            // matching the "access_as_user Author.Read" format seen in production tokens.
            var claims = new[] { new Claim(ScopeValidationService.ScopeUriClaimType, "access_as_user Author.Read") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

            var result = _service.HasRequiredScope(user, "Author.Read");

            Assert.True(result);
        }

        [Fact]
        public void HasRequiredScope_UriClaimTypePresentButWrongScope_ReturnsFalse()
        {
            var claims = new[] { new Claim(ScopeValidationService.ScopeUriClaimType, "access_as_user") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

            var result = _service.HasRequiredScope(user, "Author.Read");

            Assert.False(result);
        }
    }
}
