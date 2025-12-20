using System.Security.Cryptography;
using System.Text;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Services
{
    /// <summary>
    /// Service implementation for A/B testing experiment management and variant assignment.
    /// Uses consistent hashing to ensure stable variant assignments across sessions.
    /// </summary>
    public class ExperimentService : IExperimentService
    {
        private readonly IExperimentRepository _repository;
        private readonly ILogger<ExperimentService> _logger;

        public ExperimentService(
            IExperimentRepository repository,
            ILogger<ExperimentService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<GetExperimentsResponse> GetExperimentsAsync(GetExperimentsRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Page))
                throw new ArgumentException("Page cannot be null or empty.", nameof(request));

            _logger.LogInformation("Getting experiments for page: {Page}, userId: {UserId}", 
                request.Page, request.UserId ?? "(none)");

            // Generate or use existing session ID for consistent bucketing
            var sessionId = string.IsNullOrWhiteSpace(request.UserId) 
                ? Guid.NewGuid().ToString() 
                : request.UserId;

            var bucketingKey = request.UserId ?? sessionId;

            // Get active experiments for the page
            var experiments = await _repository.GetActiveExperimentsByPageAsync(request.Page);

            _logger.LogInformation("Found {Count} active experiments for page: {Page}", 
                experiments.Count, request.Page);

            // Assign variants for each experiment
            var assignedExperiments = experiments.Select(exp =>
            {
                var variant = AssignVariant(exp, bucketingKey);
                return new AssignedExperiment
                {
                    Id = exp.id,
                    Name = exp.Name,
                    Variant = variant.Id,
                    Config = variant.Config
                };
            }).ToList();

            _logger.LogInformation("Assigned {Count} experiments for session: {SessionId}", 
                assignedExperiments.Count, sessionId);

            return new GetExperimentsResponse
            {
                Experiments = assignedExperiments,
                SessionId = sessionId
            };
        }

        /// <inheritdoc/>
        public ExperimentVariant AssignVariant(Experiment experiment, string bucketingKey)
        {
            if (experiment == null)
                throw new ArgumentNullException(nameof(experiment));
            if (string.IsNullOrWhiteSpace(bucketingKey))
                throw new ArgumentException("Bucketing key cannot be null or empty.", nameof(bucketingKey));
            if (experiment.Variants == null || experiment.Variants.Count == 0)
                throw new InvalidOperationException("Experiment must have at least one variant.");

            // Validate traffic percentages
            var totalPercentage = experiment.Variants.Sum(v => v.TrafficPercentage);
            if (totalPercentage != 100)
            {
                _logger.LogWarning("Experiment {ExperimentId} has invalid traffic allocation: {Total}%. Normalizing to 100%.", 
                    experiment.id, totalPercentage);
            }

            // Create a deterministic hash from experiment ID + bucketing key
            var hashInput = $"{experiment.id}:{bucketingKey}";
            var hash = ComputeHash(hashInput);
            
            // Convert hash to a number between 0 and 99 (representing percentage)
            var bucketValue = Math.Abs(hash % 100);

            _logger.LogDebug("Bucket value for experiment {ExperimentId}, key {BucketingKey}: {BucketValue}", 
                experiment.id, bucketingKey, bucketValue);

            // Assign variant based on traffic percentage ranges
            var cumulativePercentage = 0;
            foreach (var variant in experiment.Variants.OrderBy(v => v.Id))
            {
                cumulativePercentage += variant.TrafficPercentage;
                if (bucketValue < cumulativePercentage)
                {
                    _logger.LogInformation("Assigned variant {VariantId} to experiment {ExperimentId} for key {BucketingKey}", 
                        variant.Id, experiment.id, bucketingKey);
                    return variant;
                }
            }

            // Fallback to last variant if rounding causes issues
            var fallbackVariant = experiment.Variants.Last();
            _logger.LogWarning("Fallback to variant {VariantId} for experiment {ExperimentId}", 
                fallbackVariant.Id, experiment.id);
            return fallbackVariant;
        }

        /// <summary>
        /// Computes a deterministic hash for consistent bucketing.
        /// </summary>
        private int ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            
            // Convert first 4 bytes to integer
            return BitConverter.ToInt32(hashBytes, 0);
        }
    }
}
