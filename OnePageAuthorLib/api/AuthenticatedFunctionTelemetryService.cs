using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using System.Globalization;
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
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<AuthenticatedFunctionTelemetryService> _logger;

        // Event name constants
        private const string FunctionCallEvent = "AuthenticatedFunctionCall";
        private const string FunctionErrorEvent = "AuthenticatedFunctionError";
        private const string FunctionSuccessEvent = "AuthenticatedFunctionSuccess";

        public AuthenticatedFunctionTelemetryService(
            TelemetryClient telemetryClient,
            ILogger<AuthenticatedFunctionTelemetryService> logger)
        {
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void TrackAuthenticatedFunctionCall(
            string functionName,
            string? userId,
            string? userEmail,
            Dictionary<string, string>? additionalProperties = null)
        {
            var telemetry = new EventTelemetry(FunctionCallEvent);
            telemetry.Properties["FunctionName"] = functionName ?? string.Empty;
            telemetry.Properties["UserId"] = userId ?? "Anonymous";
            
            // Only store email domain for privacy
            if (!string.IsNullOrEmpty(userEmail))
            {
                var domain = userEmail.Contains('@') ? userEmail.Split('@')[1] : "unknown";
                telemetry.Properties["UserEmailDomain"] = domain;
            }
            else
            {
                telemetry.Properties["UserEmailDomain"] = "unknown";
            }
            
            telemetry.Properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            // Add any additional properties
            if (additionalProperties != null)
            {
                foreach (var prop in additionalProperties)
                {
                    telemetry.Properties[prop.Key] = prop.Value;
                }
            }

            _telemetryClient.TrackEvent(telemetry);
            _logger.LogDebug(
                "Tracked {EventName} for function {FunctionName} by user {UserId}",
                FunctionCallEvent,
                functionName,
                userId ?? "Anonymous");
        }

        public void TrackAuthenticatedFunctionError(
            string functionName,
            string? userId,
            string? userEmail,
            string errorMessage,
            string? errorType = null,
            Dictionary<string, string>? additionalProperties = null)
        {
            var telemetry = new EventTelemetry(FunctionErrorEvent);
            telemetry.Properties["FunctionName"] = functionName ?? string.Empty;
            telemetry.Properties["UserId"] = userId ?? "Anonymous";
            
            // Only store email domain for privacy
            if (!string.IsNullOrEmpty(userEmail))
            {
                var domain = userEmail.Contains('@') ? userEmail.Split('@')[1] : "unknown";
                telemetry.Properties["UserEmailDomain"] = domain;
            }
            else
            {
                telemetry.Properties["UserEmailDomain"] = "unknown";
            }
            
            telemetry.Properties["ErrorMessage"] = errorMessage ?? string.Empty;
            
            if (!string.IsNullOrEmpty(errorType))
            {
                telemetry.Properties["ErrorType"] = errorType;
            }
            
            telemetry.Properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            // Add any additional properties
            if (additionalProperties != null)
            {
                foreach (var prop in additionalProperties)
                {
                    telemetry.Properties[prop.Key] = prop.Value;
                }
            }

            _telemetryClient.TrackEvent(telemetry);
            _logger.LogDebug(
                "Tracked {EventName} for function {FunctionName} by user {UserId} with error: {ErrorMessage}",
                FunctionErrorEvent,
                functionName,
                userId ?? "Anonymous",
                errorMessage);
        }

        public void TrackAuthenticatedFunctionSuccess(
            string functionName,
            string? userId,
            string? userEmail,
            Dictionary<string, string>? additionalProperties = null,
            Dictionary<string, double>? metrics = null)
        {
            var telemetry = new EventTelemetry(FunctionSuccessEvent);
            telemetry.Properties["FunctionName"] = functionName ?? string.Empty;
            telemetry.Properties["UserId"] = userId ?? "Anonymous";
            
            // Only store email domain for privacy
            if (!string.IsNullOrEmpty(userEmail))
            {
                var domain = userEmail.Contains('@') ? userEmail.Split('@')[1] : "unknown";
                telemetry.Properties["UserEmailDomain"] = domain;
            }
            else
            {
                telemetry.Properties["UserEmailDomain"] = "unknown";
            }
            
            telemetry.Properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            // Add any additional properties
            if (additionalProperties != null)
            {
                foreach (var prop in additionalProperties)
                {
                    telemetry.Properties[prop.Key] = prop.Value;
                }
            }

            // Application Insights v3 removed EventTelemetry.Metrics; store numeric values as properties.
            if (metrics != null)
            {
                foreach (var metric in metrics)
                {
                    telemetry.Properties[$"Metric.{metric.Key}"] = metric.Value.ToString(CultureInfo.InvariantCulture);
                }
            }

            _telemetryClient.TrackEvent(telemetry);
            _logger.LogDebug(
                "Tracked {EventName} for function {FunctionName} by user {UserId}",
                FunctionSuccessEvent,
                functionName,
                userId ?? "Anonymous");
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
