using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Moq.Protected;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Functions;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.Services;
using InkStainedWretch.OnePageAuthorLib.Models;

namespace OnePageAuthor.Test.InkStainedWretchFunctions;

public class CheckDomainAvailabilityTests
{
    // -------------------------------------------------------------------------
    // DomainAvailabilityValidator Tests
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("example.com")]
    [InlineData("my-domain.net")]
    [InlineData("xn--nxasmq6b.com")]  // punycode label
    [InlineData("abc.io")]
    public void DomainAvailabilityValidator_ValidDomain_ReturnsTrue(string domain)
    {
        var result = DomainAvailabilityValidator.IsValid(domain, out var error);
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
    public void DomainAvailabilityValidator_InvalidDomain_ReturnsFalseWithMessage(string? domain, string expectedFragment)
    {
        var result = DomainAvailabilityValidator.IsValid(domain, out var error);
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains(expectedFragment, error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DomainAvailabilityValidator_DomainExceeding253Chars_ReturnsFalse()
    {
        var longLabel = new string('a', 63);
        var domain = $"{longLabel}.{longLabel}.{longLabel}.{longLabel}.com";  // way over 253
        var result = DomainAvailabilityValidator.IsValid(domain, out var error);
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

    [Fact]
    public async Task AddRdapClient_ServiceFactory_ConfiguresUserAgentHeader_OnOutgoingRequests()
    {
        // Verifies that ServiceFactory.AddRdapClient (not a manually-constructed HttpClient)
        // sends the User-Agent header that prevents rdap.org from returning HTTP 403.
        HttpRequestMessage? capturedRequest = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
                   .Setup<Task<HttpResponseMessage>>(
                       "SendAsync",
                       ItExpr.IsAny<HttpRequestMessage>(),
                       ItExpr.IsAny<CancellationToken>())
                   .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                   .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRdapClient("https://rdap.org/");

        // Replace the primary handler so requests are captured without hitting the network.
        services.ConfigureAll<HttpClientFactoryOptions>(
            o => o.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = mockHandler.Object));

        await using var sp = services.BuildServiceProvider();
        var rdapClient = sp.GetRequiredService<IRdapClient>();

        await rdapClient.CheckAvailabilityAsync("example.com");

        Assert.NotNull(capturedRequest);
        var userAgent = capturedRequest!.Headers.UserAgent.ToString();
        Assert.Contains("OnePageAuthor-DomainAvailability", userAgent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("example.com.")]    // trailing FQDN dot stripped to "example.com"
    [InlineData("EXAMPLE.COM.")]    // upper-case + trailing dot
    public async Task RdapClient_TrailingDotAndCasing_NormalizesBeforeRequest(string rawDomain)
    {
        // Capture the URL that was actually requested.
        string? capturedPath = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
                   capturedPath = req.RequestUri?.PathAndQuery)
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://rdap.org/")
        };
        var logger = new Mock<ILogger<RdapClient>>().Object;
        var client = new RdapClient(httpClient, logger);

        var result = await client.CheckAvailabilityAsync(rawDomain);

        // Domain stored in the response must not have a trailing dot or upper-case letters.
        Assert.Equal("example.com", result.Domain);
        // The URL path must also use the cleaned domain and the expected RDAP path prefix.
        Assert.StartsWith("/domain/example.com", capturedPath, StringComparison.Ordinal);
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

    private static CheckDomainAvailability BuildFunction(
        IRdapClient rdapClient,
        IWhmcsService? whmcsService = null)
    {
        var logger = new Mock<ILogger<CheckDomainAvailability>>().Object;
        var whmcs = whmcsService ?? CreateUnconfiguredWhmcsMock().Object;
        return new CheckDomainAvailability(logger, rdapClient, whmcs);
    }

    /// <summary>Creates a WHMCS mock that reports itself as not configured (RDAP fallback).</summary>
    private static Mock<IWhmcsService> CreateUnconfiguredWhmcsMock()
    {
        var mock = new Mock<IWhmcsService>();
        mock.Setup(w => w.IsConfigured).Returns(false);
        return mock;
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var rdap = new Mock<IRdapClient>().Object;
        var whmcs = CreateUnconfiguredWhmcsMock().Object;
        Assert.Throws<ArgumentNullException>(() => new CheckDomainAvailability(null!, rdap, whmcs));
    }

    [Fact]
    public void Constructor_NullRdapClient_Throws()
    {
        var logger = new Mock<ILogger<CheckDomainAvailability>>().Object;
        var whmcs = CreateUnconfiguredWhmcsMock().Object;
        Assert.Throws<ArgumentNullException>(() => new CheckDomainAvailability(logger, null!, whmcs));
    }

    [Fact]
    public void Constructor_NullWhmcsService_Throws()
    {
        var logger = new Mock<ILogger<CheckDomainAvailability>>().Object;
        var rdap = new Mock<IRdapClient>().Object;
        Assert.Throws<ArgumentNullException>(() => new CheckDomainAvailability(logger, rdap, null!));
    }

    [Fact]
    public async Task Run_MissingDomainParameter_Returns400()
    {
        var function = BuildFunction(new Mock<IRdapClient>().Object);
        var req = CreateRequest(null);

        var result = await function.Run(req);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<DomainAvailabilityErrorResponse>(bad.Value);
        Assert.Equal("InvalidDomain", error.Error);
    }

    [Fact]
    public async Task Run_EmptyDomainParameter_Returns400()
    {
        var function = BuildFunction(new Mock<IRdapClient>().Object);
        var req = CreateRequest("   ");

        var result = await function.Run(req);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<DomainAvailabilityErrorResponse>(bad.Value);
    }

    [Fact]
    public async Task Run_InvalidDomainFormat_Returns400WithValidationMessage()
    {
        var function = BuildFunction(new Mock<IRdapClient>().Object);
        var req = CreateRequest("sub.domain.com"); // subdomain not allowed

        var result = await function.Run(req);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<DomainAvailabilityErrorResponse>(bad.Value);
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
        var error = Assert.IsType<DomainAvailabilityErrorResponse>(obj.Value);
        Assert.Equal("RdapLookupFailed", error.Error);
    }

    [Fact]
    public async Task Run_ClientCancellation_Returns499()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var rdapMock = new Mock<IRdapClient>();
        rdapMock.Setup(r => r.CheckAvailabilityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

        // Create a request whose RequestAborted token is already cancelled.
        var reqMock = new Mock<HttpRequest>();
        var queryDict = new Dictionary<string, StringValues> { ["domain"] = "example.com" };
        reqMock.Setup(r => r.Query).Returns(new QueryCollection(queryDict));

        var contextMock = new Mock<HttpContext>();
        contextMock.Setup(c => c.RequestAborted).Returns(cts.Token);
        reqMock.Setup(r => r.HttpContext).Returns(contextMock.Object);

        var function = BuildFunction(rdapMock.Object);
        var result = await function.Run(reqMock.Object);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(499, status.StatusCode);
    }

    [Fact]
    public async Task Run_RdapTimeoutNotClientCancellation_Returns502()
    {
        // Simulate a non-client-initiated OperationCanceledException (e.g. HttpClient timeout).
        var rdapMock = new Mock<IRdapClient>();
        rdapMock.Setup(r => r.CheckAvailabilityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException("Timeout", new TimeoutException()));

        var function = BuildFunction(rdapMock.Object);
        var req = CreateRequest("example.com");  // RequestAborted = CancellationToken.None (not cancelled)

        var result = await function.Run(req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, obj.StatusCode);
        var error = Assert.IsType<DomainAvailabilityErrorResponse>(obj.Value);
        Assert.Equal("RdapLookupFailed", error.Error);
        Assert.Contains("did not respond in time", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // RdapClient Retry Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RdapClient_FirstAttemptTimesOut_RetriesAndSucceeds()
    {
        var callCount = 0;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .Returns<HttpRequestMessage, CancellationToken>((_, _) =>
               {
                   callCount++;
                   if (callCount == 1)
                       throw new TaskCanceledException("Simulated timeout", new TimeoutException());
                   return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
               });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://rdap.org/") };
        var logger = new Mock<ILogger<RdapClient>>().Object;
        var client = new RdapClient(httpClient, logger);

        var result = await client.CheckAvailabilityAsync("newdomain123.com");

        Assert.Equal("newdomain123.com", result.Domain);
        Assert.True(result.Available);
        Assert.Equal(2, callCount);  // first attempt + one retry
    }

    [Fact]
    public async Task RdapClient_FirstAttemptTransientHttpError_RetriesAndSucceeds()
    {
        var callCount = 0;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .Returns<HttpRequestMessage, CancellationToken>((_, _) =>
               {
                   callCount++;
                   if (callCount == 1)
                       throw new HttpRequestException("Simulated transient error");
                   return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
               });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://rdap.org/") };
        var logger = new Mock<ILogger<RdapClient>>().Object;
        var client = new RdapClient(httpClient, logger);

        var result = await client.CheckAvailabilityAsync("example.com");

        Assert.Equal("example.com", result.Domain);
        Assert.False(result.Available);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task RdapClient_BothAttemptsTimeOut_PropagatesException()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .Throws(new TaskCanceledException("Simulated timeout", new TimeoutException()));

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://rdap.org/") };
        var logger = new Mock<ILogger<RdapClient>>().Object;
        var client = new RdapClient(httpClient, logger);

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => client.CheckAvailabilityAsync("example.com"));
    }

    [Fact]
    public async Task RdapClient_ClientCancellation_DoesNotRetry()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var callCount = 0;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .Returns<HttpRequestMessage, CancellationToken>((_, ct) =>
               {
                   callCount++;
                   ct.ThrowIfCancellationRequested();
                   return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
               });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://rdap.org/") };
        var logger = new Mock<ILogger<RdapClient>>().Object;
        var client = new RdapClient(httpClient, logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.CheckAvailabilityAsync("example.com", cts.Token));

        // No retry should be attempted when the caller cancels.
        Assert.Equal(1, callCount);
    }

    // -------------------------------------------------------------------------
    // .ng TLD — DomainAvailabilityValidator Tests
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("example.ng")]              // direct .ng registration
    [InlineData("example.com.ng")]          // commercial second-level .ng
    [InlineData("example.name.ng")]         // personal second-level .ng
    [InlineData("example.org.ng")]          // organisation second-level .ng
    [InlineData("EXAMPLE.COM.NG")]          // upper-case normalised to lower
    public void DomainAvailabilityValidator_ValidNgDomain_ReturnsTrue(string domain)
    {
        var result = DomainAvailabilityValidator.IsValid(domain, out var error);
        Assert.True(result, $"Expected valid but got error: {error}");
        Assert.Null(error);
    }

    [Theory]
    [InlineData("sub.example.com.ng", "Subdomains")]    // four labels — too deep
    [InlineData("a.b.c.ng", "Subdomains")]              // four labels — too deep
    public void DomainAvailabilityValidator_InvalidNgDomain_ReturnsFalseWithMessage(
        string domain, string expectedFragment)
    {
        var result = DomainAvailabilityValidator.IsValid(domain, out var error);
        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains(expectedFragment, error, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // .ng TLD — CheckDomainAvailability Function Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Run_NgDomain_WhmcsConfigured_Available_Returns200WithAvailableTrue()
    {
        var whmcsMock = new Mock<IWhmcsService>();
        whmcsMock.Setup(w => w.IsConfigured).Returns(true);
        whmcsMock.Setup(w => w.CheckDomainAvailabilityAsync("mynewdomain.ng"))
                 .ReturnsAsync(true);

        var function = BuildFunction(new Mock<IRdapClient>().Object, whmcsMock.Object);
        var req = CreateRequest("mynewdomain.ng");

        var result = await function.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DomainAvailabilityResponse>(ok.Value);
        Assert.Equal("mynewdomain.ng", response.Domain);
        Assert.True(response.Available);
        Assert.Equal("whmcs", response.RdapSource);

        // RDAP must NOT have been called.
        whmcsMock.Verify(w => w.CheckDomainAvailabilityAsync("mynewdomain.ng"), Times.Once);
    }

    [Fact]
    public async Task Run_NgSecondLevelDomain_WhmcsConfigured_Unavailable_Returns200WithAvailableFalse()
    {
        var whmcsMock = new Mock<IWhmcsService>();
        whmcsMock.Setup(w => w.IsConfigured).Returns(true);
        whmcsMock.Setup(w => w.CheckDomainAvailabilityAsync("example.com.ng"))
                 .ReturnsAsync(false);

        var function = BuildFunction(new Mock<IRdapClient>().Object, whmcsMock.Object);
        var req = CreateRequest("example.com.ng");

        var result = await function.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DomainAvailabilityResponse>(ok.Value);
        Assert.Equal("example.com.ng", response.Domain);
        Assert.False(response.Available);
        Assert.Equal("whmcs", response.RdapSource);
    }

    [Fact]
    public async Task Run_NgDomain_WhmcsNotConfigured_FallsBackToRdap()
    {
        var rdapMock = new Mock<IRdapClient>();
        rdapMock.Setup(r => r.CheckAvailabilityAsync("mynewdomain.ng", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DomainAvailabilityResponse
                {
                    Domain = "mynewdomain.ng",
                    Available = true,
                    CheckedAt = DateTime.UtcNow,
                    RdapStatus = 404,
                    RdapSource = "rdap.org"
                });

        var whmcsMock = CreateUnconfiguredWhmcsMock();

        var function = BuildFunction(rdapMock.Object, whmcsMock.Object);
        var req = CreateRequest("mynewdomain.ng");

        var result = await function.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DomainAvailabilityResponse>(ok.Value);
        Assert.True(response.Available);
        Assert.Equal("rdap.org", response.RdapSource);

        // WHMCS must NOT have been called since it is not configured.
        whmcsMock.Verify(w => w.CheckDomainAvailabilityAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Run_NgDomain_WhmcsHttpRequestException_Returns502()
    {
        var whmcsMock = new Mock<IWhmcsService>();
        whmcsMock.Setup(w => w.IsConfigured).Returns(true);
        whmcsMock.Setup(w => w.CheckDomainAvailabilityAsync(It.IsAny<string>()))
                 .ThrowsAsync(new HttpRequestException("WHMCS error"));

        var function = BuildFunction(new Mock<IRdapClient>().Object, whmcsMock.Object);
        var req = CreateRequest("example.ng");

        var result = await function.Run(req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, obj.StatusCode);
        var error = Assert.IsType<DomainAvailabilityErrorResponse>(obj.Value);
        Assert.Equal("WhmcsLookupFailed", error.Error);
    }

    [Fact]
    public async Task Run_NgDomain_WhmcsInvalidOperationException_Returns502()
    {
        var whmcsMock = new Mock<IWhmcsService>();
        whmcsMock.Setup(w => w.IsConfigured).Returns(true);
        whmcsMock.Setup(w => w.CheckDomainAvailabilityAsync(It.IsAny<string>()))
                 .ThrowsAsync(new InvalidOperationException("WHMCS API error"));

        var function = BuildFunction(new Mock<IRdapClient>().Object, whmcsMock.Object);
        var req = CreateRequest("example.ng");

        var result = await function.Run(req);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, obj.StatusCode);
        var error = Assert.IsType<DomainAvailabilityErrorResponse>(obj.Value);
        Assert.Equal("WhmcsLookupFailed", error.Error);
    }

    [Fact]
    public async Task Run_NgDomain_ClientCancellation_Returns499()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var whmcsMock = new Mock<IWhmcsService>();
        whmcsMock.Setup(w => w.IsConfigured).Returns(true);
        whmcsMock.Setup(w => w.CheckDomainAvailabilityAsync(It.IsAny<string>()))
                 .ThrowsAsync(new OperationCanceledException());

        var reqMock = new Mock<HttpRequest>();
        var queryDict = new Dictionary<string, StringValues> { ["domain"] = "example.ng" };
        reqMock.Setup(r => r.Query).Returns(new QueryCollection(queryDict));

        var contextMock = new Mock<HttpContext>();
        contextMock.Setup(c => c.RequestAborted).Returns(cts.Token);
        reqMock.Setup(r => r.HttpContext).Returns(contextMock.Object);

        var function = BuildFunction(new Mock<IRdapClient>().Object, whmcsMock.Object);
        var result = await function.Run(reqMock.Object);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(499, status.StatusCode);
    }

    [Fact]
    public async Task Run_NonNgDomain_NeverCallsWhmcs()
    {
        var whmcsMock = new Mock<IWhmcsService>();
        whmcsMock.Setup(w => w.IsConfigured).Returns(true);

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

        var function = BuildFunction(rdapMock.Object, whmcsMock.Object);
        var req = CreateRequest("example.com");

        var result = await function.Run(req);

        Assert.IsType<OkObjectResult>(result);
        whmcsMock.Verify(w => w.CheckDomainAvailabilityAsync(It.IsAny<string>()), Times.Never);
    }
}