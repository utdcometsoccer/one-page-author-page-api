using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.API;
using Microsoft.Extensions.Hosting;

class Program
{
    static async Task Main()
    {
        using (IHost host = Host.CreateDefaultBuilder()
                                .ConfigureServices(services =>
                                {
                                    var config = new ConfigurationBuilder()
                                        .AddUserSecrets<Program>()
                                        .AddEnvironmentVariables()
                                        .Build();
                                    // Standardize configuration keys to match other applications
                                    string endpointUri = config["COSMOSDB_ENDPOINT_URI"] ?? config["EndpointUri"] ?? throw new InvalidOperationException("COSMOSDB_ENDPOINT_URI is required");
                                    string primaryKey = config["COSMOSDB_PRIMARY_KEY"] ?? config["PrimaryKey"] ?? throw new InvalidOperationException("COSMOSDB_PRIMARY_KEY is required");
                                    string databaseId = config["COSMOSDB_DATABASE_ID"] ?? config["DatabaseId"] ?? throw new InvalidOperationException("COSMOSDB_DATABASE_ID is required");

                                    // Register Cosmos via extensions and domain services
                                    services
                                        .AddCosmosClient(endpointUri, primaryKey)
                                        .AddCosmosDatabase(databaseId)
                                        .AddInkStainedWretchServices();
                                })
                                .Build())
        {

            // Enumerate data folder for inkstainedwretch.language-country.json files
            string dataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(dataRoot))
            {
                Console.WriteLine($"Data folder not found: {dataRoot}");
                return;
            }

            var jsonFiles = Directory.GetFiles(dataRoot, "inkstainedwretch.*.json", SearchOption.TopDirectoryOnly);
            var filePattern = new Regex(@"inkstainedwretch\.([a-z]{2})-([a-z]{2})\.json", RegexOptions.IgnoreCase);

            foreach (var file in jsonFiles)
            {
                var fileName = Path.GetFileName(file);
                var match = filePattern.Match(fileName);
                if (!match.Success)
                    continue;

                string language = match.Groups[1].Value;
                string country = match.Groups[2].Value;
                string culture = $"{language}-{country}";

                Console.WriteLine($"Processing file: {fileName} (Culture: {culture})");

                string json = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Iterate over each top-level object and process containers
                foreach (var property in root.EnumerateObject())
                {
                    string containerName = property.Name;
                    Console.WriteLine($"Processing container: {containerName}");

                    // Dynamically get the container manager for the POCO type
                    // Use loaded assemblies to find the POCO type
                    Type? pocoType = null;
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var candidate = asm.GetType($"InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement.{containerName}");
                        if (candidate != null)
                        {
                            pocoType = candidate;
                            break;
                        }
                    }
                    if (pocoType is null)
                    {
                        Console.WriteLine($"POCO class not found for {containerName}, skipping.");
                        continue;
                    }

                    var containerManagerType = typeof(IContainerManager<>).MakeGenericType(pocoType);
                    var containerManagerObj = host.Services.GetService(containerManagerType);
                    if (containerManagerObj is null)
                    {
                        Console.WriteLine($"ContainerManager not found for {containerName}, skipping.");
                        continue;
                    }
                    dynamic containerManager = containerManagerObj;
                    var cosmosContainerObj = await containerManager.EnsureContainerAsync();
                    if (cosmosContainerObj is null)
                    {
                        Console.WriteLine($"Cosmos container could not be ensured for {containerName}, skipping.");
                        continue;
                    }
                    dynamic cosmosContainer = cosmosContainerObj;

                    var repositoryType = typeof(InkStainedWretch.OnePageAuthorAPI.NoSQL.GenericRepository<>).MakeGenericType(pocoType);
                    var repositoryObj = Activator.CreateInstance(repositoryType, cosmosContainer);
                    if (repositoryObj is null)
                    {
                        Console.WriteLine($"Repository could not be created for {containerName}, skipping.");
                        continue;
                    }
                    dynamic repository = repositoryObj;

                    // Check for existing data before adding
                    var obj = property.Value;
                    if (obj.ValueKind == JsonValueKind.Object)
                    {
                        var pocoInstanceObj = Activator.CreateInstance(pocoType);
                        if (pocoInstanceObj is null)
                        {
                            Console.WriteLine($"POCO instance could not be created for {containerName}, skipping.");
                            continue;
                        }
                        dynamic pocoInstance = pocoInstanceObj;
                        var cultureProp = pocoType.GetProperty("Culture");
                        if (cultureProp is not null && cultureProp.CanWrite)
                            cultureProp.SetValue(pocoInstance, culture);

                        // Check if item already exists for this culture
                        bool itemExists = await CheckIfCultureExistsAsync(repository, culture, containerName);
                        if (itemExists)
                        {
                            Console.WriteLine($"Skipping {containerName} for culture {culture} - already exists");
                            continue;
                        }

                        foreach (var field in obj.EnumerateObject())
                        {
                            var prop = pocoType.GetProperty(field.Name);
                            if (prop is not null && prop.CanWrite)
                            {
                                prop.SetValue(pocoInstance, field.Value.GetString() ?? string.Empty);
                            }
                        }
                        try
                        {
                            await repository.AddAsync(pocoInstance);
                            Console.WriteLine($"Added {containerName} for culture {culture}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to add {containerName} for culture {culture}: {ex.Message}");
                        }
                    }
                    else if (obj.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in obj.EnumerateArray())
                        {
                            var pocoInstanceObj = Activator.CreateInstance(pocoType);
                            if (pocoInstanceObj is null)
                            {
                                Console.WriteLine($"POCO instance could not be created for {containerName}, skipping item.");
                                continue;
                            }
                            dynamic pocoInstance = pocoInstanceObj;
                            var cultureProp = pocoType.GetProperty("Culture");
                            if (cultureProp is not null && cultureProp.CanWrite)
                                cultureProp.SetValue(pocoInstance, culture);

                            // Set properties from JSON first so we can check for existing items
                            foreach (var field in item.EnumerateObject())
                            {
                                var prop = pocoType.GetProperty(field.Name);
                                if (prop is not null && prop.CanWrite)
                                {
                                    prop.SetValue(pocoInstance, field.Value.GetString() ?? string.Empty);
                                }
                            }

                            // Check if this specific array item already exists
                            bool itemExists = await CheckIfIndividualItemExistsAsync(repository, pocoInstance, containerName);
                            if (itemExists)
                            {
                                Console.WriteLine($"Skipping array item in {containerName} for culture {culture} - already exists");
                                continue;
                            }

                            try
                            {
                                await repository.AddAsync(pocoInstance);
                                Console.WriteLine($"Added array item in {containerName} for culture {culture}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to add array item in {containerName} for culture {culture}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Check if data already exists for a specific culture in the container
    /// </summary>
    private static async Task<bool> CheckIfCultureExistsAsync(dynamic repository, string culture, string containerName)
    {
        try
        {
            // Query for items with the specific culture
            var queryDefinition = new Microsoft.Azure.Cosmos.QueryDefinition(
                "SELECT VALUE COUNT(1) FROM c WHERE c.Culture = @culture")
                .WithParameter("@culture", culture);

            // Get the underlying container from the repository
            var containerProperty = repository.GetType().GetField("_container",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (containerProperty?.GetValue(repository) is not InkStainedWretch.OnePageAuthorAPI.NoSQL.IDataContainer container)
            {
                Console.WriteLine($"Could not access container for {containerName}, allowing insert");
                return false;
            }

            using var iterator = container.GetItemQueryIterator<int>(queryDefinition);
            var response = await iterator.ReadNextAsync();
            var count = response.Resource.FirstOrDefault();

            return count > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking existing data for {containerName} culture {culture}: {ex.Message}");
            // If we can't check, allow the insert (it will fail safely if duplicate)
            return false;
        }
    }

    /// <summary>
    /// Check if a specific individual item already exists in the container
    /// This method checks for exact matches based on key properties
    /// </summary>
    private static async Task<bool> CheckIfIndividualItemExistsAsync(dynamic repository, dynamic pocoInstance, string containerName)
    {
        try
        {
            // Get the type of the poco instance
            var pocoType = pocoInstance.GetType();

            // Build a query to check for existing items based on key properties
            var cultureProperty = pocoType.GetProperty("Culture");
            var idProperty = pocoType.GetProperty("Id") ?? pocoType.GetProperty("id");

            if (cultureProperty == null)
            {
                Console.WriteLine($"Could not find Culture property for {containerName}, skipping duplicate check");
                return false;
            }

            string culture = cultureProperty.GetValue(pocoInstance)?.ToString() ?? "";

            // Check if ID is set and use it for more specific matching
            if (idProperty != null)
            {
                string id = idProperty.GetValue(pocoInstance)?.ToString() ?? "";
                if (!string.IsNullOrEmpty(id))
                {
                    var idQueryDefinition = new Microsoft.Azure.Cosmos.QueryDefinition(
                        "SELECT VALUE COUNT(1) FROM c WHERE c.Culture = @culture AND c.id = @id")
                        .WithParameter("@culture", culture)
                        .WithParameter("@id", id);

                    return await ExecuteCountQuery(repository, idQueryDefinition, containerName);
                }
            }

            // For array items without explicit IDs, try to find other identifying properties
            // Look for common properties that might uniquely identify items
            var identifyingProperties = new[] { "Name", "Title", "Key", "Code", "Type", "Label" };
            var queryConditions = new List<string> { "c.Culture = @culture" };
            var parameters = new Dictionary<string, object> { { "@culture", culture } };

            foreach (var propName in identifyingProperties)
            {
                var prop = pocoType.GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(pocoInstance)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        string paramName = $"@{propName.ToLower()}";
                        queryConditions.Add($"c.{propName} = {paramName}");
                        parameters.Add(paramName, value);
                    }
                }
            }

            // If we found additional identifying properties, use them for a more specific query
            if (queryConditions.Count > 1)
            {
                string whereClause = string.Join(" AND ", queryConditions);
                var specificQuery = new Microsoft.Azure.Cosmos.QueryDefinition($"SELECT VALUE COUNT(1) FROM c WHERE {whereClause}");

                foreach (var param in parameters)
                {
                    specificQuery = specificQuery.WithParameter(param.Key, param.Value);
                }

                return await ExecuteCountQuery(repository, specificQuery, containerName);
            }

            // Fallback: For array items, be more permissive to allow multiple items per culture
            // Only block if we can't determine uniqueness
            Console.WriteLine($"No unique identifying properties found for {containerName} array item, allowing insert");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking existing individual item for {containerName}: {ex.Message}");
            // If we can't check, allow the insert (it will fail safely if duplicate)
            return false;
        }
    }

    /// <summary>
    /// Execute a count query against the repository container
    /// </summary>
    private static async Task<bool> ExecuteCountQuery(dynamic repository, Microsoft.Azure.Cosmos.QueryDefinition queryDefinition, string containerName)
    {
        try
        {
            // Get the underlying container from the repository
            var containerProperty = repository.GetType().GetField("_container",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (containerProperty?.GetValue(repository) is not InkStainedWretch.OnePageAuthorAPI.NoSQL.IDataContainer container)
            {
                Console.WriteLine($"Could not access container for {containerName}, allowing insert");
                return false;
            }

            using var iterator = container.GetItemQueryIterator<int>(queryDefinition);
            var response = await iterator.ReadNextAsync();
            var count = response.Resource.FirstOrDefault();

            return count > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing count query for {containerName}: {ex.Message}");
            return false;
        }
    }
}