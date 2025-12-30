using InkStainedWretch.OnePageAuthorLib.Extensions;
using InkStainedWretch.OnePageAuthorLib.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace OnePageAuthor.Test.ErrorHandling;

/// <summary>
/// Unit tests for standardized error response extensions.
/// Tests error response formats and exception handling.
/// </summary>
public class ErrorResponseExtensionsTests
{
    private readonly Mock<ILogger> _mockLogger;

    public ErrorResponseExtensionsTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void CreateErrorResult_ReturnsStandardizedErrorResponse()
    {
        // Arrange
        var statusCode = StatusCodes.Status400BadRequest;
        var errorMessage = "Test error message";
        var details = "Test details";
        var traceId = "test-trace-id";

        // Act
        var result = ErrorResponseExtensions.CreateErrorResult(statusCode, errorMessage, details, traceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(statusCode, result.StatusCode);
        
        var errorResponse = result.Value as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal(statusCode, errorResponse!.StatusCode);
        Assert.Equal(errorMessage, errorResponse.Error);
        Assert.Equal(details, errorResponse.Details);
        Assert.Equal(traceId, errorResponse.TraceId);
        Assert.NotNull(errorResponse.Timestamp);
    }

    [Fact]
    public void CreateErrorResult_GeneratesTraceIdWhenNotProvided()
    {
        // Arrange
        var statusCode = StatusCodes.Status500InternalServerError;
        var errorMessage = "Internal server error";

        // Act
        var result = ErrorResponseExtensions.CreateErrorResult(statusCode, errorMessage);

        // Assert
        var errorResponse = result.Value as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse!.TraceId);
        Assert.NotEmpty(errorResponse.TraceId);
    }

    [Fact]
    public void HandleException_ArgumentException_Returns400BadRequest()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");

        // Act
        var result = ErrorResponseExtensions.HandleException(exception, _mockLogger.Object);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        
        var errorResponse = result.Value as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Invalid request parameters", errorResponse!.Error);
        Assert.Null(errorResponse.Details); // Should not include details by default
    }

    [Fact]
    public void HandleException_ArgumentNullException_Returns400BadRequest()
    {
        // Arrange
        var exception = new ArgumentNullException("param");

        // Act
        var result = ErrorResponseExtensions.HandleException(exception, _mockLogger.Object);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        
        var errorResponse = result.Value as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Required parameter is missing", errorResponse!.Error);
    }

    [Fact]
    public void HandleException_InvalidOperationException_Returns400BadRequest()
    {
        // Arrange
        var exception = new InvalidOperationException("Invalid operation");

        // Act
        var result = ErrorResponseExtensions.HandleException(exception, _mockLogger.Object);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        
        var errorResponse = result.Value as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Invalid operation", errorResponse!.Error);
    }

    [Fact]
    public void HandleException_UnauthorizedAccessException_Returns401Unauthorized()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Unauthorized");

        // Act
        var result = ErrorResponseExtensions.HandleException(exception, _mockLogger.Object);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        
        var errorResponse = result.Value as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Unauthorized access", errorResponse!.Error);
    }

    [Fact]
    public void HandleException_KeyNotFoundException_Returns404NotFound()
    {
        // Arrange
        var exception = new KeyNotFoundException("Resource not found");

        // Act
        var result = ErrorResponseExtensions.HandleException(exception, _mockLogger.Object);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        
        var errorResponse = result.Value as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("Resource not found", errorResponse!.Error);
    }

    [Fact]
    public void HandleException_GenericException_Returns500InternalServerError()
    {
        // Arrange
        var exception = new Exception("Unexpected error");

        // Act
        var result = ErrorResponseExtensions.HandleException(exception, _mockLogger.Object);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        
        var errorResponse = result.Value as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal("An unexpected error occurred", errorResponse!.Error);
    }

    [Fact]
    public void HandleException_IncludeDetails_IncludesExceptionMessage()
    {
        // Arrange
        var exceptionMessage = "Detailed error information";
        var exception = new Exception(exceptionMessage);

        // Act
        var result = ErrorResponseExtensions.HandleException(exception, _mockLogger.Object, includeDetails: true);

        // Assert
        var errorResponse = result.Value as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.Equal(exceptionMessage, errorResponse!.Details);
    }

    [Fact]
    public void HandleException_LogsErrorWithTraceId()
    {
        // Arrange
        var exception = new Exception("Test exception");

        // Act
        var result = ErrorResponseExtensions.HandleException(exception, _mockLogger.Object);

        // Assert
        var errorResponse = result.Value as ErrorResponse;
        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse!.TraceId);
        
        // Verify logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TraceId")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ErrorResponse_HasCorrectJsonPropertyNames()
    {
        // Arrange
        var errorResponse = new ErrorResponse
        {
            StatusCode = 400,
            Error = "Test error",
            Details = "Test details",
            TraceId = "test-trace-id"
        };

        // Act & Assert
        Assert.Equal(400, errorResponse.StatusCode);
        Assert.Equal("Test error", errorResponse.Error);
        Assert.Equal("Test details", errorResponse.Details);
        Assert.Equal("test-trace-id", errorResponse.TraceId);
        Assert.NotNull(errorResponse.Timestamp);
    }
}
