using System.Collections.Concurrent;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Services
{
    /// <summary>
    /// In-memory rate limiting service that tracks requests per IP address.
    /// For production, consider using Redis or a distributed cache.
    /// </summary>
    public class RateLimitService : IRateLimitService
    {
        private readonly ILogger<RateLimitService> _logger;
        private readonly ConcurrentDictionary<string, RequestTracker> _requestTrackers = new();
        private readonly int _maxRequestsPerMinute;
        private readonly TimeSpan _windowDuration;

        public RateLimitService(ILogger<RateLimitService> logger, int maxRequestsPerMinute = 10)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxRequestsPerMinute = maxRequestsPerMinute;
            _windowDuration = TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Checks if a request from the given IP address should be allowed.
        /// </summary>
        public async Task<bool> IsRequestAllowedAsync(string ipAddress, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                _logger.LogWarning("Rate limit check called with null or empty IP address");
                return true; // Allow if IP is unknown
            }

            var key = GetKey(ipAddress, endpoint);
            var tracker = _requestTrackers.GetOrAdd(key, _ => new RequestTracker());

            // Clean up old requests outside the window
            var cutoffTime = DateTime.UtcNow.Subtract(_windowDuration);
            tracker.Requests.RemoveAll(t => t < cutoffTime);

            var currentCount = tracker.Requests.Count;
            var isAllowed = currentCount < _maxRequestsPerMinute;

            if (!isAllowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for IP: {IpAddress}, Endpoint: {Endpoint}, Count: {Count}/{Max}",
                    ipAddress, endpoint, currentCount, _maxRequestsPerMinute);
            }

            return await Task.FromResult(isAllowed);
        }

        /// <summary>
        /// Records a request from the given IP address.
        /// </summary>
        public async Task RecordRequestAsync(string ipAddress, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return;

            var key = GetKey(ipAddress, endpoint);
            var tracker = _requestTrackers.GetOrAdd(key, _ => new RequestTracker());
            
            tracker.Requests.Add(DateTime.UtcNow);
            
            _logger.LogDebug("Recorded request for IP: {IpAddress}, Endpoint: {Endpoint}", ipAddress, endpoint);
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets the number of remaining requests allowed for the IP address.
        /// </summary>
        public async Task<int> GetRemainingRequestsAsync(string ipAddress, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return _maxRequestsPerMinute;

            var key = GetKey(ipAddress, endpoint);
            
            if (!_requestTrackers.TryGetValue(key, out var tracker))
                return _maxRequestsPerMinute;

            // Clean up old requests
            var cutoffTime = DateTime.UtcNow.Subtract(_windowDuration);
            tracker.Requests.RemoveAll(t => t < cutoffTime);

            var currentCount = tracker.Requests.Count;
            var remaining = Math.Max(0, _maxRequestsPerMinute - currentCount);

            return await Task.FromResult(remaining);
        }

        private static string GetKey(string ipAddress, string endpoint)
        {
            return $"{ipAddress}:{endpoint}";
        }

        /// <summary>
        /// Internal class to track requests per IP/endpoint combination.
        /// </summary>
        private class RequestTracker
        {
            public List<DateTime> Requests { get; } = new();
        }
    }
}
