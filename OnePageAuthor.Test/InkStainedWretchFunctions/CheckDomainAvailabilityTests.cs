using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Moq.Protected;
using InkStainedWretch.OnePageAuthorAPI.Functions;
using InkStainedWretch.OnePageAuthorAPI.Functions.DomainAvailability.Models;
using InkStainedWretch.OnePageAuthorAPI.Functions.DomainAvailability.Services;
using InkStainedWretch.OnePageAuthorAPI.Functions.DomainAvailability.Validation;

namespace OnePageAuthor.Test.InkStainedWretchFunctions;

public class CheckDomainAvailabilityTests
{
    // -------------------------------------------------------------------------
    // DomainValidator Tests
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("example.com")]
    [InlineData("my-domain.net")]
    [InlineData("xn--nxasmq6b.com")]  // punycode label
    [InlineData("abc.io")]
    public void DomainValidator_ValidDomain_ReturnsTrue(string domain)
    {
        var result = DomainValidator.IsValid(domain, out var error);
        Assert.True(result, $"Expected valid but got error: {error}");
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null, "must not be empty")]
    [InlineData("", "must not be empty")]
    [InlineData("   ", "must not be empty")]
    [InlineData("nodot", "valid TLD")]
    [InlineData("sub.domain.com", "Subdomains")]
    [InlineData("-leading.com", "invalid characters")]
    [InlineData("trailing-.com", "invalid characters")]
    [InlineData("example.1", "not valid")]       // numeric-only TLD
    [InlineData("example.c", "not valid")]        // single-char TLD
    public void DomainValidator_InvalidDomain_ReturnsFalseWithMessage(string? domain, string expectedFragment)
    {
        var result = DomainValidator.IsValid(domain, out var error);
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains(expectedFragment, error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DomainValidator_DomainExceeding253Chars_ReturnsFalse()
    {
        var longLabel = new string('a', 63);
        var domain = $"{longLabel}.{longLabel}.{longLabel}.{longLabel}.com";  // way over 253
        var result = DomainValidator.IsValid(domain, out var error);
        // Either too long or subdomain error - both are invalid
        Assert.False(result);
        Assert.NotNull(error);
    }

    // -------------------------------------------------------------------------
    // RdapClient Tests
    // -------------------------------------------------------------------------

    private static HttpClient CreateFakeHttpClient(HttpStatusCode statusCode)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage(statusCode));

        return new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://rdap.org/")
        };
    }

    [Fact]
    public async Task RdapClient_200Response_ReturnsDomainRegistered()
    {
        var httpClient = CreateFakeHttpClient(HttpStatusCode.OK);
        var logger = new Mock<ILogger<RdapClient>>().Object;
        var client = new RdapClient(httpClient, logger);

        var result = await client.CheckAvailabilityAsync("example.com");

        Assert.Equal("example.com", result.Domain);
        Assert.False(result.Available);
        Assert.Equal(200, result.RdapStatus);
        Assert.Equal("rdap.org", result.RdapSource);
    }

    [Fact]
    public async Task RdapClient_404Response_ReturnsDomainAvailable()
    {
        var httpClient = CreateFakeHttpClient(HttpStatusCode.NotFound);
        var logger = new Mock<ILogger<RdapClient>>().Object;
        var client = new RdapClient(httpClient, logger);

        var result = await client.CheckAvailabilityAsync("newdomain123.com");

        Assert.Equal("newdomain123.com", result.Domain);
        Assert.True(result.Available);
        Assert.Equal(404, result.RdapStatus);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task RdapClient_UnexpectedStatus_ThrowsHttpRequestException(HttpStatusCode statusCode)
    {
        var httpClient = CreateFakeHttpClient(statusCode);
        var logger = new Mock<ILogger<RdapClient>>().Object;
        var client = new RdapClient(httpClient, logger);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.CheckAvailabilityAsync("example.com"));
    }

    [Fact]
    public void RdapClient_NullHttpClient_ThrowsArgumentNullException()
    {
        var logger = new Mock<ILogger<RdapClient>>().Object;
        Assert.Throws<ArgumentNullException>(() => new RdapClient(null!, logger));
    }

    [Fact]
    public void RdapClient_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RdapClient(new HttpClient(), null!));
    }

    // -------------------------------------------------------------------------
    // CheckDomainAvailability Function Tests
    // -------------------------------------------------------------------------

    private static HttpRequest CreateRequest(string? domain)
    {
        var mock = new Mock<HttpRequest>();
        var queryDict = new Dictionary<string, StringValues>();
        if (domain is not null)
            queryDict["domain"] = domain;

        mock.Setup(r => r.Query).Returns(new QueryCollection(queryDict));

        // Provide a minimal HttpContext so RequestAborted doesn't throw.
        var contextMock = new Mock<HttpContext>();
        contextMock.Setup(c => c.RequestAborted).Returns(CancellationToken.None);
        mock.Setup(r => r.HttpContext).Returns(contextMock.Object);

        return mock.Object;
    }

    private static CheckDomainAvailability BuildFunction(IRdapClient rdapClient)
    {
        var logger = new Mock<ILogger<CheckDomainAvailability>>().Object;
        return new CheckDomainAvailability(logger, rdapClient);
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var rdap = new Mock<IRdapClient>().Object;
        Assert.Throws<ArgumentNullException>(() => new CheckDomainAvailability(null!, rdap));
    }

    [Fact]
    public void Constructor_NullRdapClient_Throws()
    {
        var logger = new Mock<ILogger<CheckDomainAvailability>>().Object;
        Assert.Throws<ArgumentNullException>(() => new CheckDomainAvailability(logger, null!));
    }

    [Fact]
    public async Task Run_MissingDomainParameter_Returns400()
    {
        var function = BuildFunction(new Mock<IRdapClient>().Object);
        var req = CreateRequest(null);

        var result = await function.Run(req);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(bad.Value);
        Assert.Equal("InvalidDomain", error.Error);
    }

    [Fact]
    public async Task Run_EmptyDomainParameter_Returns400()
    {
        var function = BuildFunction(new Mock<IRdapClient>().Object);
        var req = CreateRequest("   ");

        var result = await function.Run(req);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ErrorResponse>(bad.Value);
    }

    [Fact]
    public async Task Run_InvalidDomainFormat_Returns400WithValidationMessage()
    {
        var function = BuildFunction(new Mock<IRdapClient>().Object);
        var req = CreateRequest("sub.domain.com"); // subdomain not allowed

        var result = await function.Run(req);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(bad.Value);
        Assert.Equal("InvalidDomain", error.Error);
        Assert.Contains("Subdomain", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Run_RegisteredDomain_Returns200WithAvailableFalse()
    {
        var rdapMock = new Mock<IRdapClient>();
        rdapMock.Setup(r => r.CheckAvailabilityAsync("example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DomainAvailabilityResponse
                {
                    Domain = "example.com",
                    Available = false,
                    CheckedAt = DateTime.UtcNow,
                    RdapStatus = 200,
                    RdapSource = "rdap.org"
                });

        var function = BuildFunction(rdapMock.Object);
        var req = CreateRequest("example.com");

        var result = await function.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DomainAvailabilityResponse>(ok.Value);
        Assert.Equal("example.com", response.Domain);
        Assert.False(response.Available);
    }

    [Fact]
    public async Task Run_AvailableDomain_Returns200WithAvailableTrue()
    {
        var rdapMock = new Mock<IRdapClient>();
        rdapMock.Setup(r => r.CheckAvailabilityAsync("mynewdomain123.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DomainAvailabilityResponse
                {
                    Domain = "mynewdomain123.com",
                    Available = true,
                    CheckedAt = DateTime.UtcNow,
                    RdapStatus = 404,
                    RdapSource = "rdap.org"
                });

        var function = BuildFunction(rdapMock.Object);
        var req = CreateRequest("mynewdomain123.com");

        var result = await function.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DomainAvailabilityResponse>(ok.Value);
        Assert.True(response.Available);
    }

    [Fact]
    public async Task Run_RdapHttpRequestException_Returns502()
    {
        var rdapMock = new Mock<IRdapClient>();
        rdapMock.Setup(r => r.CheckAvailabilityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("RDAP error", null, HttpStatusCode.ServiceUnavailable));

        var function = BuildFunction(rdapMock.Object);
        var req = CreateRequest("example.com");

        var result = await function.Run(req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, obj.StatusCode);
        var error = Assert.IsType<ErrorResponse>(obj.Value);
        Assert.Equal("RdapLookupFailed", error.Error);
    }
}
