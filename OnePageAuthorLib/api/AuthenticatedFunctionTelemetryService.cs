using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace InkStainedWretch.OnePageAuthorLib.API
{
    /// <summary>
    /// Application Insights telemetry service for tracking authenticated function calls.
    /// Emits custom events for Log Analytics and Power BI dashboard integration.
    /// </summary>
    public interface IAuthenticatedFunctionTelemetryService
    {
        /// <summary>
        /// Tracks an authenticated function call with user context and operation details.
        /// </summary>
        void TrackAuthenticatedFunctionCall(
            string functionName,
            string? userId,
            string? userEmail,
            Dictionary<string, string>? additionalProperties = null);

        /// <summary>
        /// Tracks an error in an authenticated function with full context.
        /// </summary>
        void TrackAuthenticatedFunctionError(
            string functionName,
            string? userId,
            string? userEmail,
            string errorMessage,
            string? errorType = null,
            Dictionary<string, string>? additionalProperties = null);

        /// <summary>
        /// Tracks successful completion of an authenticated function operation.
        /// </summary>
        void TrackAuthenticatedFunctionSuccess(
            string functionName,
            string? userId,
            string? userEmail,
            Dictionary<string, string>? additionalProperties = null,
            Dictionary<string, double>? metrics = null);
    }

    /// <summary>
    /// Implementation of authenticated function telemetry service.
    /// </summary>
    public class AuthenticatedFunctionTelemetryService : IAuthenticatedFunctionTelemetryService
    {
        private readonly ILogger<AuthenticatedFunctionTelemetryService> _logger;

        // Event name constants
        private const string FunctionCallEvent = "AuthenticatedFunctionCall";
        private const string FunctionErrorEvent = "AuthenticatedFunctionError";
        private const string FunctionSuccessEvent = "AuthenticatedFunctionSuccess";

        public AuthenticatedFunctionTelemetryService(
            ILogger<AuthenticatedFunctionTelemetryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static string GetEmailDomain(string? userEmail)
        {
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return "unknown";
            }

            return userEmail.Contains('@') ? userEmail.Split('@')[1] : "unknown";
        }

        private void TrackEvent(
            string eventName,
            string functionName,
            string? userId,
            string? userEmail,
            Dictionary<string, string>? additionalProperties,
            Dictionary<string, double>? metrics = null)
        {
            var scope = new Dictionary<string, object?>
            {
                ["EventName"] = eventName,
                ["FunctionName"] = functionName ?? string.Empty,
                ["UserId"] = userId ?? "Anonymous",
                ["UserEmailDomain"] = GetEmailDomain(userEmail),
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };

            if (additionalProperties != null)
            {
                foreach (var prop in additionalProperties)
                {
                    scope[prop.Key] = prop.Value;
                }
            }

            if (metrics != null)
            {
                foreach (var metric in metrics)
                {
                    scope[$"Metric.{metric.Key}"] = metric.Value;
                }
            }

            using (_logger.BeginScope(scope))
            {
                _logger.LogInformation("TelemetryEvent {EventName}", eventName);
            }
        }

        public void TrackAuthenticatedFunctionCall(
            string functionName,
            string? userId,
            string? userEmail,
            Dictionary<string, string>? additionalProperties = null)
        {
            TrackEvent(FunctionCallEvent, functionName, userId, userEmail, additionalProperties);
        }

        public void TrackAuthenticatedFunctionError(
            string functionName,
            string? userId,
            string? userEmail,
            string errorMessage,
            string? errorType = null,
            Dictionary<string, string>? additionalProperties = null)
        {
            additionalProperties ??= new Dictionary<string, string>();
            additionalProperties["ErrorMessage"] = errorMessage ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(errorType))
            {
                additionalProperties["ErrorType"] = errorType;
            }

            TrackEvent(FunctionErrorEvent, functionName, userId, userEmail, additionalProperties);
        }

        public void TrackAuthenticatedFunctionSuccess(
            string functionName,
            string? userId,
            string? userEmail,
            Dictionary<string, string>? additionalProperties = null,
            Dictionary<string, double>? metrics = null)
        {
            TrackEvent(FunctionSuccessEvent, functionName, userId, userEmail, additionalProperties, metrics);
        }

        /// <summary>
        /// Helper method to extract user ID from ClaimsPrincipal.
        /// </summary>
        public static string? ExtractUserId(ClaimsPrincipal? user)
        {
            if (user == null) return null;
            
            // Try common claim types for user ID
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst("oid")?.Value
                ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        }

        /// <summary>
        /// Helper method to extract user email from ClaimsPrincipal.
        /// </summary>
        public static string? ExtractUserEmail(ClaimsPrincipal? user)
        {
            if (user == null) return null;
            
            // Try common claim types for email
            return user.FindFirst(ClaimTypes.Email)?.Value
                ?? user.FindFirst("email")?.Value
                ?? user.FindFirst("preferred_username")?.Value
                ?? user.FindFirst("upn")?.Value
                ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        }
    }
}
