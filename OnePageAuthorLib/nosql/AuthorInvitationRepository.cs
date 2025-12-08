using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository for AuthorInvitation with partition key EmailAddress.
    /// </summary>
    public class AuthorInvitationRepository : IAuthorInvitationRepository
    {
        private readonly IDataContainer _container;

        public AuthorInvitationRepository(Container container)
        {
            _container = new CosmosContainerWrapper(container);
        }

        public AuthorInvitationRepository(IDataContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public async Task<AuthorInvitation?> GetByIdAsync(string id)
        {
            // Query by id since we may not know the partition key
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);
            using var iterator = _container.GetItemQueryIterator<AuthorInvitation>(query);
            if (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                return page.Resource.FirstOrDefault();
            }
            return null;
        }

        public async Task<AuthorInvitation?> GetByEmailAsync(string emailAddress)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.EmailAddress = @email")
                .WithParameter("@email", emailAddress);
            using var iterator = _container.GetItemQueryIterator<AuthorInvitation>(query);
            if (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                return page.Resource.FirstOrDefault();
            }
            return null;
        }

        public async Task<IList<AuthorInvitation>> GetByDomainAsync(string domainName)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.DomainName = @domain")
                .WithParameter("@domain", domainName);
            var invitations = new List<AuthorInvitation>();
            using var iterator = _container.GetItemQueryIterator<AuthorInvitation>(query);
            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                invitations.AddRange(page.Resource);
            }
            return invitations;
        }

        public async Task<IList<AuthorInvitation>> GetPendingInvitationsAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.Status = @status")
                .WithParameter("@status", "Pending");
            var invitations = new List<AuthorInvitation>();
            using var iterator = _container.GetItemQueryIterator<AuthorInvitation>(query);
            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                invitations.AddRange(page.Resource);
            }
            return invitations;
        }

        public async Task<AuthorInvitation> AddAsync(AuthorInvitation invitation)
        {
            if (string.IsNullOrWhiteSpace(invitation.EmailAddress))
                throw new InvalidOperationException("AuthorInvitation.EmailAddress is required for partition key.");
            if (string.IsNullOrWhiteSpace(invitation.id))
            {
                invitation.id = Guid.NewGuid().ToString();
            }
            var response = await _container.CreateItemAsync(invitation, new PartitionKey(invitation.EmailAddress));
            return response.Resource;
        }

        public async Task<AuthorInvitation> UpdateAsync(AuthorInvitation invitation)
        {
            if (string.IsNullOrWhiteSpace(invitation.id))
                throw new InvalidOperationException("AuthorInvitation.id must be provided.");
            if (string.IsNullOrWhiteSpace(invitation.EmailAddress))
                throw new InvalidOperationException("AuthorInvitation.EmailAddress is required for partition key.");
            var response = await _container.ReplaceItemAsync(invitation, invitation.id, new PartitionKey(invitation.EmailAddress));
            return response.Resource;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            // Query to find the invitation first
            var existing = await GetByIdAsync(id);
            if (existing == null || string.IsNullOrWhiteSpace(existing.EmailAddress) || string.IsNullOrWhiteSpace(existing.id))
                return false;
            await _container.DeleteItemAsync<AuthorInvitation>(existing.id, new PartitionKey(existing.EmailAddress));
            return true;
        }
    }
}
