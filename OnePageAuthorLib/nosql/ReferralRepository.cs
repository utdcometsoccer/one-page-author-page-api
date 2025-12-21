using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository for Referral entities with partition key ReferrerId.
    /// </summary>
    public class ReferralRepository : IReferralRepository
    {
        private readonly IDataContainer _container;

        public ReferralRepository(Container container)
        {
            _container = new CosmosContainerWrapper(container);
        }

        public ReferralRepository(IDataContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public async Task<Referral?> GetByIdAsync(string id, string referrerId)
        {
            try
            {
                return await _container.ReadItemAsync<Referral>(id, new PartitionKey(referrerId));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IList<Referral>> GetByReferrerIdAsync(string referrerId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.ReferrerId = @referrerId")
                .WithParameter("@referrerId", referrerId);
            
            var results = new List<Referral>();
            using var iterator = _container.GetItemQueryIterator<Referral>(query);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }
            
            return results;
        }

        public async Task<Referral?> GetByReferralCodeAsync(string referralCode)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.ReferralCode = @referralCode")
                .WithParameter("@referralCode", referralCode);
            
            using var iterator = _container.GetItemQueryIterator<Referral>(query);
            
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.Resource.FirstOrDefault();
            }
            
            return null;
        }

        public async Task<bool> ExistsByReferrerAndEmailAsync(string referrerId, string referredEmail)
        {
            var query = new QueryDefinition(
                "SELECT VALUE COUNT(1) FROM c WHERE c.ReferrerId = @referrerId AND c.ReferredEmail = @referredEmail")
                .WithParameter("@referrerId", referrerId)
                .WithParameter("@referredEmail", referredEmail);
            
            using var iterator = _container.GetItemQueryIterator<int>(query);
            
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.Resource.FirstOrDefault() > 0;
            }
            
            return false;
        }

        public async Task<Referral> AddAsync(Referral referral)
        {
            if (string.IsNullOrWhiteSpace(referral.ReferrerId))
                throw new InvalidOperationException("Referral.ReferrerId is required for partition key.");
            
            if (string.IsNullOrWhiteSpace(referral.id))
            {
                referral.id = Guid.NewGuid().ToString();
            }
            
            var response = await _container.CreateItemAsync(referral, new PartitionKey(referral.ReferrerId));
            return response.Resource;
        }

        public async Task<Referral> UpdateAsync(Referral referral)
        {
            if (string.IsNullOrWhiteSpace(referral.id))
                throw new InvalidOperationException("Referral.id must be provided.");
            
            if (string.IsNullOrWhiteSpace(referral.ReferrerId))
                throw new InvalidOperationException("Referral.ReferrerId is required for partition key.");
            
            var response = await _container.ReplaceItemAsync(referral, referral.id, new PartitionKey(referral.ReferrerId));
            return response.Resource;
        }

        public async Task<bool> DeleteAsync(string id, string referrerId)
        {
            try
            {
                await _container.DeleteItemAsync<Referral>(id, new PartitionKey(referrerId));
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}
