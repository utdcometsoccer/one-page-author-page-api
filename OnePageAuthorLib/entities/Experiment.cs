namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Represents an A/B test experiment configuration stored in Cosmos DB.
    /// Each experiment defines multiple variants and their associated configurations.
    /// </summary>
    public class Experiment
    {
        /// <summary>
        /// Unique identifier for the experiment (used as Cosmos DB id).
        /// </summary>
        public string id { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable name of the experiment.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Page where this experiment should be active (e.g., 'landing', 'pricing').
        /// </summary>
        public string Page { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this experiment is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Available variants for this experiment.
        /// Each variant contains specific configuration values.
        /// </summary>
        public List<ExperimentVariant> Variants { get; set; } = new();

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last updated timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a variant within an A/B test experiment.
    /// </summary>
    public class ExperimentVariant
    {
        /// <summary>
        /// Variant identifier (e.g., 'control', 'variant_a', 'variant_b').
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable name for the variant.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Traffic allocation percentage (0-100). Sum of all variants should equal 100.
        /// </summary>
        public int TrafficPercentage { get; set; } = 0;

        /// <summary>
        /// Variant-specific configuration as key-value pairs.
        /// </summary>
        public Dictionary<string, object> Config { get; set; } = new();
    }

    /// <summary>
    /// Response model for experiment assignment.
    /// </summary>
    public class AssignedExperiment
    {
        /// <summary>
        /// Experiment identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Experiment name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Assigned variant identifier.
        /// </summary>
        public string Variant { get; set; } = string.Empty;

        /// <summary>
        /// Variant-specific configuration.
        /// </summary>
        public Dictionary<string, object> Config { get; set; } = new();
    }

    /// <summary>
    /// Request model for getting experiment assignments.
    /// </summary>
    public class GetExperimentsRequest
    {
        /// <summary>
        /// Optional user ID for consistent bucketing across sessions.
        /// If not provided, sessionId will be used for bucketing.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Page identifier (e.g., 'landing', 'pricing').
        /// </summary>
        public string Page { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for experiment assignments.
    /// </summary>
    public class GetExperimentsResponse
    {
        /// <summary>
        /// List of experiments assigned to the user/session.
        /// </summary>
        public List<AssignedExperiment> Experiments { get; set; } = new();

        /// <summary>
        /// Session identifier for tracking (generated if not provided).
        /// </summary>
        public string SessionId { get; set; } = string.Empty;
    }
}
