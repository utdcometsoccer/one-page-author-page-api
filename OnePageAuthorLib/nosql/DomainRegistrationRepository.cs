using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository for DomainRegistration with partition key Upn.
    /// </summary>
    public class DomainRegistrationRepository : IDomainRegistrationRepository
    {
        private readonly IDataContainer _container;

        public DomainRegistrationRepository(Container container)
        {
            _container = new CosmosContainerWrapper(container);
        }

        public DomainRegistrationRepository(IDataContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public async Task<DomainRegistration> CreateAsync(DomainRegistration domainRegistration)
        {
            if (string.IsNullOrWhiteSpace(domainRegistration.Upn))
                throw new InvalidOperationException("DomainRegistration.Upn is required for partition key.");
            
            if (string.IsNullOrWhiteSpace(domainRegistration.id))
            {
                domainRegistration.id = Guid.NewGuid().ToString();
            }

            domainRegistration.CreatedAt = DateTime.UtcNow;
            
            var response = await _container.CreateItemAsync(domainRegistration, new PartitionKey(domainRegistration.Upn));
            return response.Resource;
        }

        public async Task<DomainRegistration?> GetByIdAsync(string id, string upn)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(upn))
                return null;

            try
            {
                var result = await _container.ReadItemAsync<DomainRegistration>(id, new PartitionKey(upn));
                return result;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<DomainRegistration>> GetByUserAsync(string upn)
        {
            if (string.IsNullOrWhiteSpace(upn))
                return Enumerable.Empty<DomainRegistration>();

            var query = new QueryDefinition("SELECT * FROM c WHERE c.upn = @upn ORDER BY c.createdAt DESC")
                .WithParameter("@upn", upn);

            var results = new List<DomainRegistration>();
            using var iterator = _container.GetItemQueryIterator<DomainRegistration>(query);
            
            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                if (page?.Resource != null)
                {
                    results.AddRange(page.Resource);
                }
            }
            
            return results;
        }

        public async Task<DomainRegistration> UpdateAsync(DomainRegistration domainRegistration)
        {
            if (string.IsNullOrWhiteSpace(domainRegistration.id))
                throw new InvalidOperationException("DomainRegistration.id must be provided.");
            if (string.IsNullOrWhiteSpace(domainRegistration.Upn))
                throw new InvalidOperationException("DomainRegistration.Upn is required for partition key.");

            var response = await _container.ReplaceItemAsync(
                domainRegistration, 
                domainRegistration.id, 
                new PartitionKey(domainRegistration.Upn));
            return response.Resource;
        }

        public async Task<bool> DeleteAsync(string id, string upn)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(upn))
                return false;

            try
            {
                await _container.DeleteItemAsync<DomainRegistration>(id, new PartitionKey(upn));
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<DomainRegistration?> GetByDomainAsync(string topLevelDomain, string secondLevelDomain)
        {
            if (string.IsNullOrWhiteSpace(topLevelDomain) || string.IsNullOrWhiteSpace(secondLevelDomain))
                return null;

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.domain.topLevelDomain = @tld AND c.domain.secondLevelDomain = @sld")
                .WithParameter("@tld", topLevelDomain)
                .WithParameter("@sld", secondLevelDomain);

            try
            {
                using var iterator = _container.GetItemQueryIterator<DomainRegistration>(query);
                
                if (iterator.HasMoreResults)
                {
                    var page = await iterator.ReadNextAsync();
                    return page?.Resource?.FirstOrDefault();
                }
                
                return null;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<DomainRegistration?> GetByIdCrossPartitionAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);

            try
            {
                using var iterator = _container.GetItemQueryIterator<DomainRegistration>(query);

                if (iterator.HasMoreResults)
                {
                    var page = await iterator.ReadNextAsync();
                    return page?.Resource?.FirstOrDefault();
                }

                return null;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}