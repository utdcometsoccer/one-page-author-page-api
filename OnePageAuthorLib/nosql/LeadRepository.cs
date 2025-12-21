using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository for Lead entities with duplicate detection based on email.
    /// </summary>
    public class LeadRepository : ILeadRepository
    {
        protected readonly IDataContainer _container;

        public LeadRepository(Container container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container), "LeadRepository: The provided Cosmos DB container is null.");
            _container = new CosmosContainerWrapper(container);
        }

        public LeadRepository(IDataContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// Finds an existing lead by email address.
        /// </summary>
        /// <param name="email">Email address to search for</param>
        /// <param name="emailDomain">Email domain (partition key)</param>
        /// <returns>Existing lead or null if not found</returns>
        public async Task<Lead?> GetByEmailAsync(string email, string emailDomain)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email")
                    .WithParameter("@email", email.ToLowerInvariant());

                using var iterator = _container.GetItemQueryIterator<Lead>(
                    query,
                    requestOptions: new QueryRequestOptions
                    {
                        PartitionKey = new PartitionKey(emailDomain.ToLowerInvariant())
                    });

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    var lead = response.Resource.FirstOrDefault();
                    if (lead != null)
                        return lead;
                }

                return null;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a lead by its ID.
        /// </summary>
        /// <param name="id">Lead ID</param>
        /// <param name="emailDomain">Email domain (partition key)</param>
        /// <returns>Lead or null if not found</returns>
        public async Task<Lead?> GetByIdAsync(string id, string emailDomain)
        {
            try
            {
                return await _container.ReadItemAsync<Lead>(id, new PartitionKey(emailDomain.ToLowerInvariant()));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a new lead.
        /// </summary>
        /// <param name="lead">Lead to create</param>
        /// <returns>Created lead with generated ID</returns>
        public async Task<Lead> AddAsync(Lead lead)
        {
            // Ensure email and emailDomain are lowercase for consistency
            lead.Email = lead.Email.ToLowerInvariant();
            lead.EmailDomain = lead.EmailDomain.ToLowerInvariant();

            var response = await _container.CreateItemAsync(lead);
            return response.Resource;
        }

        /// <summary>
        /// Updates an existing lead.
        /// </summary>
        /// <param name="lead">Lead to update</param>
        /// <returns>Updated lead</returns>
        public async Task<Lead> UpdateAsync(Lead lead)
        {
            if (string.IsNullOrEmpty(lead.id))
                throw new InvalidOperationException("Lead 'id' property must not be null or empty.");

            lead.UpdatedAt = DateTime.UtcNow;
            lead.Email = lead.Email.ToLowerInvariant();
            lead.EmailDomain = lead.EmailDomain.ToLowerInvariant();

            var response = await _container.ReplaceItemAsync(
                lead,
                lead.id,
                new PartitionKey(lead.EmailDomain));
            return response.Resource;
        }

        /// <summary>
        /// Gets leads by source within a date range.
        /// </summary>
        /// <param name="source">Lead source filter</param>
        /// <param name="startDate">Start date (optional)</param>
        /// <param name="endDate">End date (optional)</param>
        /// <returns>List of leads</returns>
        public async Task<IList<Lead>> GetBySourceAsync(string source, DateTime? startDate = null, DateTime? endDate = null)
        {
            var queryText = "SELECT * FROM c WHERE c.source = @source";
            
            if (startDate.HasValue)
            {
                queryText += " AND c.createdAt >= @startDate";
            }

            if (endDate.HasValue)
            {
                queryText += " AND c.createdAt <= @endDate";
            }

            var query = new QueryDefinition(queryText)
                .WithParameter("@source", source);

            if (startDate.HasValue)
            {
                query = query.WithParameter("@startDate", startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.WithParameter("@endDate", endDate.Value);
            }

            var results = new List<Lead>();
            using var iterator = _container.GetItemQueryIterator<Lead>(query);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }
    }
}
