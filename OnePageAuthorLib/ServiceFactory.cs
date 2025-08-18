using Microsoft.Extensions.DependencyInjection;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;

namespace InkStainedWretch.OnePageAuthorAPI
{
    /// <summary>
    /// Provides factory methods for dependency injection and repository/service creation for the OnePageAuthorAPI.
    /// </summary>
    public static class ServiceFactory
    {
        /// <summary>
        /// Creates a LocaleDataService using endpointUri, primaryKey, and databaseId.
        /// Ensures the Locales container exists and returns a fully initialized LocaleDataService.
        /// </summary>
        /// <param name="endpointUri">The Cosmos DB endpoint URI.</param>
        /// <param name="primaryKey">The Cosmos DB primary key.</param>
        /// <param name="databaseId">The Cosmos DB database ID.</param>
        /// <returns>An initialized ILocaleDataService instance.</returns>
        public static ILocaleDataService CreateLocaleDataService(string endpointUri, string primaryKey, string databaseId)
        {
            var provider = CreateProvider(endpointUri, primaryKey, databaseId);
            var localesContainerManager = provider.GetRequiredService<IContainerManager<Entities.Locale>>();
            var localesContainer = localesContainerManager.EnsureContainerAsync().GetAwaiter().GetResult();
            var localeRepo = new NoSQL.LocaleRepository(localesContainer);
            return new API.LocaleDataService(localeRepo);
    }
    // Private static ServiceProvider for lazy loading
    private static ServiceProvider? _serviceProvider;

        // Private method to initialize ServiceProvider
        private static ServiceProvider InitializeProvider(string endpointUri, string primaryKey, string databaseId)
        {
            var services = new ServiceCollection();
            // Register CosmosClient as a singleton (only one instance injected)
            var cosmosClient = new Microsoft.Azure.Cosmos.CosmosClient(endpointUri, primaryKey);
            services.AddSingleton(cosmosClient);
            // Register Database as a singleton (only one instance injected)
            services.AddSingleton(provider => {
                var dbManager = provider.GetRequiredService<ICosmosDatabaseManager>();
                return dbManager.EnsureDatabaseAsync(endpointUri, primaryKey, databaseId).GetAwaiter().GetResult();
            });
            services.AddTransient<IContainerManager<Entities.Author>, AuthorsContainerManager>();
            services.AddTransient<IContainerManager<Entities.Book>, BooksContainerManager>();
            services.AddTransient<IContainerManager<Entities.Article>>(provider =>
                new ArticlesContainerManager(provider.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));
            services.AddTransient<IContainerManager<Entities.Social>>(provider =>
                new SocialsContainerManager(provider.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));
            services.AddTransient<IContainerManager<Entities.Locale>>(provider =>
                new LocalesContainerManager(provider.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));
            services.AddTransient<ICosmosDatabaseManager, CosmosDatabaseManager>();            
            return services.BuildServiceProvider();
        }
        /// <summary>
        /// Overloaded: Creates an AuthorDataService using endpointUri, primaryKey, and databaseId.
        /// Ensures all containers exist and returns a fully initialized AuthorDataService.
        /// </summary>
        /// <param name="endpointUri">The Cosmos DB endpoint URI.</param>
        /// <param name="primaryKey">The Cosmos DB primary key.</param>
        /// <param name="databaseId">The Cosmos DB database ID.</param>
        /// <returns>An initialized AuthorDataService instance.</returns>
        public static IAuthorDataService CreateAuthorDataService(string endpointUri, string primaryKey, string databaseId)
        {
            var provider = CreateProvider(endpointUri, primaryKey, databaseId);
            var authorsContainerManager = provider.GetRequiredService<IContainerManager<Entities.Author>>();
            var booksContainerManager = provider.GetRequiredService<IContainerManager<Entities.Book>>();
                var articlesContainerManager = provider.GetRequiredService<IContainerManager<Entities.Article>>(); // Updated constructor
                var socialsContainerManager = provider.GetRequiredService<IContainerManager<Entities.Social>>(); // Updated constructor

            var authorsContainer = authorsContainerManager.EnsureContainerAsync().GetAwaiter().GetResult();
            var booksContainer = booksContainerManager.EnsureContainerAsync().GetAwaiter().GetResult();
            var articlesContainer = articlesContainerManager.EnsureContainerAsync().GetAwaiter().GetResult();
            var socialsContainer = socialsContainerManager.EnsureContainerAsync().GetAwaiter().GetResult();

            return CreateAuthorDataService(authorsContainer, booksContainer, articlesContainer, socialsContainer);
        }
        /// <summary>
        /// Creates and configures a ServiceProvider for dependency injection.
        /// Registers all required container managers and the Cosmos database manager.
        /// </summary>
        /// <param name="endpointUri">The Cosmos DB endpoint URI.</param>
        /// <param name="primaryKey">The Cosmos DB primary key.</param>
        /// <param name="databaseId">The Cosmos DB database ID.</param>
        /// <returns>A configured ServiceProvider instance.</returns>
        public static ServiceProvider CreateProvider(string endpointUri, string primaryKey, string databaseId)
        {
            if (string.IsNullOrWhiteSpace(endpointUri))
                throw new ArgumentException("endpointUri cannot be null, empty, or whitespace.", nameof(endpointUri));
            if (string.IsNullOrWhiteSpace(primaryKey))
                throw new ArgumentException("primaryKey cannot be null, empty, or whitespace.", nameof(primaryKey));
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentException("databaseId cannot be null, empty, or whitespace.", nameof(databaseId));

            if (_serviceProvider == null)
            {
                _serviceProvider = InitializeProvider(endpointUri, primaryKey, databaseId);
            }
            return _serviceProvider;
        }

        /// <summary>
        /// Creates a repository instance for the specified entity type and Cosmos container.
        /// </summary>
        /// <param name="entityType">The entity type for which to create the repository.</param>
        /// <param name="container">The Cosmos DB container instance.</param>
        /// <returns>A repository instance for the specified entity type.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity type is not supported.</exception>
        public static object CreateRepository(Type entityType, object container)
        {
            var cosmosContainer = container is IDataContainer icc
                ? icc
                : new NoSQL.CosmosContainerWrapper((Microsoft.Azure.Cosmos.Container)container);
            if (entityType == typeof(Entities.Author))
            {
                return new NoSQL.AuthorRepository(cosmosContainer);
            }
            if (entityType == typeof(Entities.Book))
            {
                return new NoSQL.GenericRepository<Entities.Book>(cosmosContainer);
            }
            if (entityType == typeof(Entities.Article))
            {
                return new NoSQL.GenericRepository<Entities.Article>(cosmosContainer);
            }
            if (entityType == typeof(Entities.Social))
            {
                return new NoSQL.GenericRepository<Entities.Social>(cosmosContainer);
            }
            if (entityType == typeof(Entities.Locale))
            {
                return new NoSQL.LocaleRepository(cosmosContainer);
            }
            throw new ArgumentException($"No repository factory for type {entityType.Name}");
        }

        /// <summary>
        /// Creates a strongly-typed repository instance for the specified entity type and Cosmos container.
        /// </summary>
        /// <typeparam name="TRepository">The repository type to create.</typeparam>
        /// <typeparam name="TEntity">The entity type for the repository.</typeparam>
        /// <param name="container">The Cosmos DB container instance.</param>
        /// <returns>A repository instance of type TRepository for the specified entity type.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity type is not supported.</exception>
        public static TRepository? CreateRepository<TRepository, TEntity>(Microsoft.Azure.Cosmos.Container container)
            where TRepository : class
            where TEntity : class
        {
            if (typeof(TEntity) == typeof(Entities.Author) && typeof(TRepository) == typeof(NoSQL.AuthorRepository))
            {
                return new NoSQL.AuthorRepository(container) as TRepository;
            }
            if (typeof(TEntity) == typeof(Entities.Locale) && typeof(TRepository) == typeof(NoSQL.LocaleRepository))
            {
                return new NoSQL.LocaleRepository(container) as TRepository;
            }
            if (typeof(TRepository) == typeof(NoSQL.GenericRepository<TEntity>))
            {
                return Activator.CreateInstance(typeof(NoSQL.GenericRepository<TEntity>), container) as TRepository;
            }
            throw new ArgumentException($"No repository factory for type {typeof(TEntity).Name}");
        }

        /// <summary>
        /// Creates an AuthorDataService instance using the provided Cosmos DB containers for authors, books, articles, and socials.
        /// </summary>
        /// <param name="authorsContainer">The Cosmos DB container for authors.</param>
        /// <param name="booksContainer">The Cosmos DB container for books.</param>
        /// <param name="articlesContainer">The Cosmos DB container for articles.</param>
        /// <param name="socialsContainer">The Cosmos DB container for socials.</param>
        /// <returns>An initialized AuthorDataService instance.</returns>
        public static IAuthorDataService CreateAuthorDataService(
        Microsoft.Azure.Cosmos.Container authorsContainer,
        Microsoft.Azure.Cosmos.Container booksContainer,
        Microsoft.Azure.Cosmos.Container articlesContainer,
        Microsoft.Azure.Cosmos.Container socialsContainer)
        {
            var authorRepo = new NoSQL.AuthorRepository(authorsContainer);
            var bookRepo = new NoSQL.GenericRepository<Entities.Book>(booksContainer);
            var articleRepo = new NoSQL.GenericRepository<Entities.Article>(articlesContainer);
            var socialRepo = new NoSQL.GenericRepository<Entities.Social>(socialsContainer);
            return new AuthorDataService(authorRepo, bookRepo, articleRepo, socialRepo);
        }
    }
}
