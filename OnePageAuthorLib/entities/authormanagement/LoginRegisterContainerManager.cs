using InkStainedWretch.OnePageAuthorAPI.API;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class LoginRegisterContainerManager : IContainerManager<LoginRegister>
    {
        private readonly Database _database;
        private readonly string _containerName = "LoginRegister";

        public LoginRegisterContainerManager(Database database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "LoginRegisterContainerManager: The provided Database is null.");
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
