using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Manages the Cosmos DB container for AuthorInvitation entities.
    /// </summary>
    public class AuthorInvitationsContainerManager : IContainerManager<AuthorInvitation>
    {
        private readonly Database _database;
        private readonly string _containerName = "AuthorInvitations";

        public AuthorInvitationsContainerManager(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Ensures the AuthorInvitations container exists with partition key /EmailAddress.
        /// </summary>
        public async Task<Container> EnsureContainerAsync()
        {
            var response = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/EmailAddress"
            );
            return response.Container;
        }
    }
}
