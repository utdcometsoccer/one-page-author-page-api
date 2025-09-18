using InkStainedWretch.OnePageAuthorAPI.API;
using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;

namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class AuthorRegistrationContainerManager : IContainerManager<AuthorRegistration>
    {
        private readonly Database _database;
        private readonly string _containerName = "AuthorRegistration";

        public AuthorRegistrationContainerManager(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "AuthorRegistrationContainerManager: The provided Database is null.");
            _database = database;
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
