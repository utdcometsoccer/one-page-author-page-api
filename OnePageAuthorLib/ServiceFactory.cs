using Microsoft.Extensions.DependencyInjection;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using System.Text.Json;

namespace InkStainedWretch.OnePageAuthorAPI
{

    /// <summary>
    /// Provides factory methods for dependency injection and repository/service creation for the OnePageAuthorAPI.
    /// </summary>
    public static class ServiceFactory
    {
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
            var services = new ServiceCollection();       
            // register cosmos client
            services.AddSingleton(provider => {
                var cosmosClient = new Microsoft.Azure.Cosmos.CosmosClient(endpointUri, primaryKey);
                return cosmosClient;
            });
            // rgister database
            services.AddSingleton(provider => {                
                var dbManager = provider.GetRequiredService<ICosmosDatabaseManager>();
                // Ensure database exists and return it
                return dbManager.EnsureDatabaseAsync(endpointUri, primaryKey, databaseId).GetAwaiter().GetResult();
            });
            services.AddTransient<IAuthorDataService, AuthorDataService>();
            services.AddTransient<IContainerManager<Entities.Author>, AuthorsContainerManager>();
            services.AddTransient<IContainerManager<Entities.Book>, BooksContainerManager>();
            services.AddTransient<IContainerManager<Entities.Article>, ArticlesContainerManager>();
            services.AddTransient<IContainerManager<Entities.Social>, SocialsContainerManager>();
            services.AddTransient<ICosmosDatabaseManager, CosmosDatabaseManager>();            
            return services.BuildServiceProvider();
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
            if (entityType == typeof(Entities.Author))
            {
                return new NoSQL.AuthorRepository((Microsoft.Azure.Cosmos.Container)container);
            }
            if (entityType == typeof(Entities.Book))
            {
                return new NoSQL.GenericRepository<Entities.Book>((Microsoft.Azure.Cosmos.Container)container);
            }
            if (entityType == typeof(Entities.Article))
            {
                return new NoSQL.GenericRepository<Entities.Article>((Microsoft.Azure.Cosmos.Container)container);
            }
            if (entityType == typeof(Entities.Social))
            {
                return new NoSQL.GenericRepository<Entities.Social>((Microsoft.Azure.Cosmos.Container)container);
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
        public static AuthorDataService CreateAuthorDataService(
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
