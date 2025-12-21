using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Service interface for managing leads with validation, duplicate detection, and email service integration.
    /// </summary>
    public interface ILeadService
    {
        /// <summary>
        /// Creates a new lead or returns existing lead if email already exists.
        /// </summary>
        /// <param name="request">Lead creation request</param>
        /// <param name="ipAddress">IP address of the request</param>
        /// <returns>Response with lead ID and status (created or existing)</returns>
        Task<CreateLeadResponse> CreateLeadAsync(CreateLeadRequest request, string? ipAddress);

        /// <summary>
        /// Validates an email address format.
        /// </summary>
        /// <param name="email">Email to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        bool IsValidEmail(string email);

        /// <summary>
        /// Gets a lead by ID.
        /// </summary>
        /// <param name="id">Lead ID</param>
        /// <param name="emailDomain">Email domain (partition key)</param>
        /// <returns>Lead or null if not found</returns>
        Task<Lead?> GetLeadByIdAsync(string id, string emailDomain);

        /// <summary>
        /// Gets leads by source.
        /// </summary>
        /// <param name="source">Lead source</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <returns>List of leads</returns>
        Task<IList<Lead>> GetLeadsBySourceAsync(string source, DateTime? startDate = null, DateTime? endDate = null);
    }
}
