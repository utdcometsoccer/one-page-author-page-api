using System.Text.Json.Serialization;

namespace InkStainedWretch.OnePageAuthorLib.Models;

/// <summary>
/// Standardized error response format for all API endpoints.
/// Provides consistent error information to clients.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// HTTP status code for the error (e.g., 400, 404, 500).
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>
    /// Human-readable error message describing what went wrong.
    /// </summary>
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Optional detailed error message for debugging (not exposed to clients in production).
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }

    /// <summary>
    /// Unique identifier for tracking this error instance.
    /// </summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    /// <summary>
    /// Timestamp when the error occurred (ISO 8601 format).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; private set; }

    /// <summary>
    /// Initializes a new instance of the ErrorResponse class with the current UTC timestamp.
    /// </summary>
    public ErrorResponse()
    {
        Timestamp = DateTime.UtcNow.ToString("o");
    }
}
