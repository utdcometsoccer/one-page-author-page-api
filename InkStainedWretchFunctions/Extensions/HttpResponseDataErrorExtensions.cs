using System.Net;
using InkStainedWretch.OnePageAuthorLib.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorLib.Extensions;

/// <summary>
/// Extension methods for creating standardized error responses for HttpResponseData-based Azure Functions.
/// These extensions require Microsoft.Azure.Functions.Worker package to be referenced.
/// </summary>
public static class HttpResponseDataErrorExtensions
{
    /// <summary>
    /// Creates a standardized error response with HttpResponseData (for Azure Functions using HttpTrigger).
    /// </summary>
    /// <param name="req">The HTTP request data.</param>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="error">Human-readable error message.</param>
    /// <param name="details">Optional detailed error information (use only in development).</param>
    /// <param name="traceId">Optional trace ID for tracking.</param>
    /// <returns>HttpResponseData with standardized error format.</returns>
    public static async Task<HttpResponseData> CreateErrorResponseAsync(
        this HttpRequestData req,
        HttpStatusCode statusCode,
        string error,
        string? details = null,
        string? traceId = null)
    {
        var errorResponse = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Error = error,
            Details = details,
            TraceId = traceId ?? Guid.NewGuid().ToString()
        };

        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(errorResponse);
        return response;
    }

    /// <summary>
    /// Handles exceptions and creates standardized error responses.
    /// Logs the exception and returns appropriate HTTP status codes based on exception type.
    /// </summary>
    /// <param name="req">The HTTP request data.</param>
    /// <param name="ex">The exception to handle.</param>
    /// <param name="logger">Logger instance for logging the exception.</param>
    /// <param name="includeDetails">Whether to include exception details in response (only for development).</param>
    /// <returns>HttpResponseData with standardized error format.</returns>
    public static async Task<HttpResponseData> HandleExceptionAsync(
        this HttpRequestData req,
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
            ArgumentNullException => (HttpStatusCode.BadRequest, "Required parameter is missing"),
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid request parameters"),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid operation"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            NotSupportedException => (HttpStatusCode.BadRequest, "Operation not supported"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        return await req.CreateErrorResponseAsync(
            statusCode,
            message,
            includeDetails ? ex.Message : null,
            traceId);
    }
}
