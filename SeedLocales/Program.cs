using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;

class Program
{
    static async Task Main()
    {
        var config = new ConfigurationBuilder()
                                    .AddUserSecrets<Program>()
                                    .Build();
        // Read Cosmos DB settings from environment variables or config
        string endpointUri = config["EndpointUri"] ?? throw new InvalidOperationException("EndpointUri is not set.");
        string primaryKey = config["PrimaryKey"] ?? throw new InvalidOperationException("PrimaryKey is not set.");
        string databaseId = config["DatabaseId"] ?? throw new InvalidOperationException("DatabaseId is not set.");

        // Use DI factory to get required services
        var provider = InkStainedWretch.OnePageAuthorAPI.ServiceFactory.CreateProvider(endpointUri, primaryKey, databaseId);
        var dbManager = provider.GetRequiredService<ICosmosDatabaseManager>();
        var localesContainerManager = provider.GetRequiredService<IContainerManager<Locale>>();

        // Ensure database and container exist
        var database = await dbManager.EnsureDatabaseAsync(endpointUri, primaryKey, databaseId);
        var localesContainer = await localesContainerManager.EnsureContainerAsync();

        // Create repository
        var localeRepository = InkStainedWretch.OnePageAuthorAPI.ServiceFactory.CreateRepository<InkStainedWretch.OnePageAuthorAPI.NoSQL.LocaleRepository, Locale>(localesContainer);

        string dataRoot = Utility.GetDataRoot();
        if (!Directory.Exists(dataRoot))
        {
            Console.WriteLine($"Data folder not found: {dataRoot}");
            return;
        }

        var localeList = new List<Locale>();
        foreach (var file in Directory.GetFiles(dataRoot, "*.json"))
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string[] parts = fileName.Split('-');
            string language = parts.Length > 0 ? parts[0] : "";
            string region = parts.Length > 1 ? parts[1] : "";

            string json = File.ReadAllText(file);
            LocaleResponse? response = null;
            try
            {
                response = JsonSerializer.Deserialize<LocaleResponse>(json);
                if (response == null)
                {
                    Console.WriteLine($"Warning: File '{fileName}' did not produce a valid LocaleResponse and will be skipped.");
                    continue;
                }
                var locale = new Locale(response, language, region);
                localeList.Add(locale);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to deserialize {fileName}: {ex.Message}");
                continue;
            }
        }

        // Save all locales to Cosmos DB (overwrite if exists)
        if (localeRepository == null)
        {
            throw new InvalidOperationException("LocaleRepository is null.");
        }
        foreach (var locale in localeList)
        {
            try
            {
                // Try to get existing locale by language and region
                var existingLocales = await localeRepository.GetByLanguageAndRegionAsync(locale.LanguageName, locale.RegionName);
                var existingLocale = existingLocales.FirstOrDefault();
                
                if (existingLocale != null)
                {
                    Console.WriteLine($"Locale {locale.LanguageName}-{locale.RegionName} already exists (id: {existingLocale.id}), updating...");
                    // Update the existing locale with new values but keep the existing id
                    locale.id = existingLocale.id;
                    await localeRepository.UpdateAsync(locale);
                }
                else
                {
                    Console.WriteLine($"Adding new locale {locale.LanguageName}-{locale.RegionName} (id: {locale.id})...");
                    await localeRepository.AddAsync(locale);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing locale {locale.id}: {ex.Message}");
                // Try to add anyway in case it was a different error
                try
                {
                    await localeRepository.AddAsync(locale);
                }
                catch (Exception addEx)
                {
                    Console.WriteLine($"Failed to add locale {locale.id}: {addEx.Message}");
                }
            }
        }

        // Print all locales
        foreach (var locale in localeList)
        {
            Console.WriteLine($"Locale: id={locale.id}, Language={locale.LanguageName}, Region={locale.RegionName}");
            Console.WriteLine($"  Welcome: {locale.Welcome}");
            Console.WriteLine($"  AboutMe: {locale.AboutMe}");
            Console.WriteLine($"  MyBooks: {locale.MyBooks}");
            Console.WriteLine($"  Loading: {locale.Loading}");
            Console.WriteLine($"  EmailPrompt: {locale.EmailPrompt}");
            Console.WriteLine($"  ContactMe: {locale.ContactMe}");
            Console.WriteLine($"  EmailLinkText: {locale.EmailLinkText}");
            Console.WriteLine($"  NoEmail: {locale.NoEmail}");
            Console.WriteLine($"  SwitchToLight: {locale.SwitchToLight}");
            Console.WriteLine($"  SwitchToDark: {locale.SwitchToDark}");
            Console.WriteLine($"  Articles: {locale.Articles}");
            Console.WriteLine();
        }
    }
}
