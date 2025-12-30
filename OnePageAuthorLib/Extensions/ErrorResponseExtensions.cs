using System.Net;
using InkStainedWretch.OnePageAuthorLib.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorLib.Extensions;

/// <summary>
/// Extension methods for creating standardized error responses across all API endpoints.
/// Ensures consistent error handling patterns for IActionResult-based functions.
/// </summary>
public static class ErrorResponseExtensions
{
    /// <summary>
    /// Creates a standardized error response with IActionResult (for Azure Functions using ASP.NET Core integration).
    /// </summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="error">Human-readable error message.</param>
    /// <param name="details">Optional detailed error information (use only in development).</param>
    /// <param name="traceId">Optional trace ID for tracking.</param>
    /// <returns>ObjectResult with standardized error format.</returns>
    public static ObjectResult CreateErrorResult(
        int statusCode,
        string error,
        string? details = null,
        string? traceId = null)
    {
        var errorResponse = new ErrorResponse
        {
            StatusCode = statusCode,
            Error = error,
            Details = details,
            TraceId = traceId ?? Guid.NewGuid().ToString()
        };

        return new ObjectResult(errorResponse)
        {
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Handles exceptions and creates standardized error responses for IActionResult.
    /// Logs the exception and returns appropriate HTTP status codes based on exception type.
    /// </summary>
    /// <param name="ex">The exception to handle.</param>
    /// <param name="logger">Logger instance for logging the exception.</param>
    /// <param name="includeDetails">Whether to include exception details in response (only for development).</param>
    /// <returns>ObjectResult with standardized error format.</returns>
    public static ObjectResult HandleException(
        Exception ex,
        ILogger logger,
        bool includeDetails = false)
    {
        var traceId = Guid.NewGuid().ToString();
        
        // Log the exception with trace ID
        logger.LogError(ex, "Error occurred. TraceId: {TraceId}", traceId);

        // Map exception types to HTTP status codes and messages
        var (statusCode, message) = ex switch
        {
            ArgumentNullException => (StatusCodes.Status400BadRequest, "Required parameter is missing"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request parameters"),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Invalid operation"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized access"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            NotSupportedException => (StatusCodes.Status400BadRequest, "Operation not supported"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        return CreateErrorResult(
            statusCode,
            message,
            includeDetails ? ex.Message : null,
            traceId);
    }
}
