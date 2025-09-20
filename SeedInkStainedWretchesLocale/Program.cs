using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.API;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Cosmos;

class Program
{
	static async Task Main()
	{
		using (IHost host = Host.CreateDefaultBuilder()
								.ConfigureServices(services =>
								{
									var config = new ConfigurationBuilder()
										.AddUserSecrets<Program>()
										.Build();
									string endpointUri = config["EndpointUri"] ?? throw new InvalidOperationException("EndpointUri is not set.");
									string primaryKey = config["PrimaryKey"] ?? throw new InvalidOperationException("PrimaryKey is not set.");
									string databaseId = config["DatabaseId"] ?? throw new InvalidOperationException("DatabaseId is not set.");

									// Register CosmosClient as a Singleton
									services.AddSingleton(serviceProvider =>
									{
										return new CosmosClient(endpointUri, primaryKey);
									});									
									services.AddTransient<Microsoft.Azure.Cosmos.Database>(ServiceProvider =>
									{
										var cosmosClient = ServiceProvider.GetRequiredService<CosmosClient>();
										var database = cosmosClient.GetDatabase(databaseId);
										return database;
									});
									// Read Cosmos DB settings from environment variables or config
									services.AddInkStainedWretchServices();
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
						foreach (var field in obj.EnumerateObject())
						{
							var prop = pocoType.GetProperty(field.Name);
							if (prop is not null && prop.CanWrite)
							{
								prop.SetValue(pocoInstance, field.Value.GetString() ?? string.Empty);
							}
						}
						await repository.AddAsync(pocoInstance);
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
							foreach (var field in item.EnumerateObject())
							{
								var prop = pocoType.GetProperty(field.Name);
								if (prop is not null && prop.CanWrite)
								{
									prop.SetValue(pocoInstance, field.Value.GetString() ?? string.Empty);
								}
							}
							await repository.AddAsync(pocoInstance);
						}
					}
				}
			}
		}
	}
}