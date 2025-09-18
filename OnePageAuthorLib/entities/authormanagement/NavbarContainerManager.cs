using InkStainedWretch.OnePageAuthorAPI.API;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class NavbarContainerManager : IContainerManager<Navbar>
    {
        private readonly Database _database;
        private readonly string _containerName = "Navbar";

        public NavbarContainerManager(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "NavbarContainerManager: The provided Database is null.");
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
