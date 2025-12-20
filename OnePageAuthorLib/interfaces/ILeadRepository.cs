using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Repository interface for Lead entities.
    /// </summary>
    public interface ILeadRepository
    {
        /// <summary>
        /// Finds an existing lead by email address.
        /// </summary>
        Task<Lead?> GetByEmailAsync(string email, string emailDomain);

        /// <summary>
        /// Gets a lead by its ID.
        /// </summary>
        Task<Lead?> GetByIdAsync(string id, string emailDomain);

        /// <summary>
        /// Creates a new lead.
        /// </summary>
        Task<Lead> AddAsync(Lead lead);

        /// <summary>
        /// Updates an existing lead.
        /// </summary>
        Task<Lead> UpdateAsync(Lead lead);

        /// <summary>
        /// Gets leads by source within a date range.
        /// </summary>
        Task<IList<Lead>> GetBySourceAsync(string source, DateTime? startDate = null, DateTime? endDate = null);
    }
}
