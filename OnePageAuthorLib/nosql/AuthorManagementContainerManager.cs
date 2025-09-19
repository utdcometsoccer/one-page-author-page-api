using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    public class AuthorManagementContainerManager<T> : IContainerManager<T> where T : AuthorManagementBase
    {
        private readonly Database _database;
        private readonly string _containerName;

        public AuthorManagementContainerManager(Database database, string containerName)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "AuthorManagementContainerManager: The provided Database is null.");
            _database = database;
            _containerName = containerName;
        }

        public async Task<Container> EnsureContainerAsync()
        {
            var containerResponse = await _database.CreateContainerIfNotExistsAsync(
                id: _containerName,
                partitionKeyPath: "/Culture"
            );
            return containerResponse.Container;
        }       
    }

}