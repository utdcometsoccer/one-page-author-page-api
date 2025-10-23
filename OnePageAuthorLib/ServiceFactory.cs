using Microsoft.Extensions.DependencyInjection;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.API.Penguin;
using InkStainedWretch.OnePageAuthorLib.API.Amazon;
using InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement;
using InkStainedWretch.OnePageAuthorAPI.Interfaces.Authormanagement;

namespace InkStainedWretch.OnePageAuthorAPI
{
    /// <summary>
    /// Provides factory methods for dependency injection and repository/service creation for the OnePageAuthorAPI.
    /// </summary>
    public static class ServiceFactory
    {
        /// <summary>
        /// Registers a CosmosClient singleton using the provided endpoint and key.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="endpointUri">Cosmos DB account endpoint URI.</param>
        /// <param name="primaryKey">Cosmos DB primary key.</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddCosmosClient(this IServiceCollection services, string endpointUri, string primaryKey)
        {
            if (string.IsNullOrWhiteSpace(endpointUri))
                throw new ArgumentException("endpointUri cannot be null or empty.", nameof(endpointUri));
            if (string.IsNullOrWhiteSpace(primaryKey))
                throw new ArgumentException("primaryKey cannot be null or empty.", nameof(primaryKey));

            services.AddSingleton(sp => new Microsoft.Azure.Cosmos.CosmosClient(endpointUri, primaryKey));
            return services;
        }

        /// <summary>
        /// Registers a Cosmos Database singleton by ensuring it exists using the already-registered CosmosClient.
        /// Call after AddCosmosClient.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="databaseId">Cosmos DB database id.</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddCosmosDatabase(this IServiceCollection services, string databaseId)
        {
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentException("databaseId cannot be null or empty.", nameof(databaseId));

            services.AddSingleton(sp =>
            {
                var client = sp.GetRequiredService<Microsoft.Azure.Cosmos.CosmosClient>();
                return client.CreateDatabaseIfNotExistsAsync(databaseId).GetAwaiter().GetResult().Database;
            });
            return services;
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
            services.AddSingleton(provider =>
            {
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
            services.AddTransient<IContainerManager<Entities.UserProfile>>(provider =>
                new UserProfilesContainerManager(provider.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));
            services.AddTransient<ICosmosDatabaseManager, CosmosDatabaseManager>();
            return services.BuildServiceProvider();
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
            if (entityType == typeof(Entities.UserProfile))
            {
                return new NoSQL.UserProfileRepository(cosmosContainer);
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
            if (typeof(TEntity) == typeof(Entities.UserProfile) && typeof(TRepository) == typeof(NoSQL.UserProfileRepository))
            {
                return new NoSQL.UserProfileRepository(container) as TRepository;
            }
            if (typeof(TRepository) == typeof(NoSQL.GenericRepository<TEntity>))
            {
                return Activator.CreateInstance(typeof(NoSQL.GenericRepository<TEntity>), container) as TRepository;
            }
            throw new ArgumentException($"No repository factory for type {typeof(TEntity).Name}");
        }


        /// <summary>
        /// Registers AuthorDataService and its Cosmos container managers in DI.
        /// Requires that a singleton Microsoft.Azure.Cosmos.Database and CosmosClient are already registered.
        /// </summary>
        public static IServiceCollection AddAuthorDataService(this IServiceCollection services)
        {
            // Register container managers
            services.AddTransient<IContainerManager<Entities.Author>, AuthorsContainerManager>();
            services.AddTransient<IContainerManager<Entities.Book>>(sp =>
                new BooksContainerManager(
                    sp.GetRequiredService<Microsoft.Azure.Cosmos.CosmosClient>(),
                    sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));
            services.AddTransient<IContainerManager<Entities.Article>>(sp =>
                new ArticlesContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));
            services.AddTransient<IContainerManager<Entities.Social>>(sp =>
                new SocialsContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));

            // Register data service (singleton is fine; containers and Cosmos SDK are thread-safe)
            services.AddSingleton<IAuthorDataService>(sp =>
            {
                var authorsContainer = sp.GetRequiredService<IContainerManager<Entities.Author>>().EnsureContainerAsync().GetAwaiter().GetResult();
                var booksContainer = sp.GetRequiredService<IContainerManager<Entities.Book>>().EnsureContainerAsync().GetAwaiter().GetResult();
                var articlesContainer = sp.GetRequiredService<IContainerManager<Entities.Article>>().EnsureContainerAsync().GetAwaiter().GetResult();
                var socialsContainer = sp.GetRequiredService<IContainerManager<Entities.Social>>().EnsureContainerAsync().GetAwaiter().GetResult();

                var authorRepo = new NoSQL.AuthorRepository(authorsContainer);
                var bookRepo = new NoSQL.GenericRepository<Entities.Book>(booksContainer);
                var articleRepo = new NoSQL.GenericRepository<Entities.Article>(articlesContainer);
                var socialRepo = new NoSQL.GenericRepository<Entities.Social>(socialsContainer);
                return new AuthorDataService(authorRepo, bookRepo, articleRepo, socialRepo);
            });

            return services;
        }

        /// <summary>
        /// Creates a UserProfileRepository by ensuring the UserProfiles container exists.
        /// Partition key is /Upn.
        /// </summary>
        public static NoSQL.UserProfileRepository CreateUserProfileRepository(string endpointUri, string primaryKey, string databaseId)
        {
            var provider = CreateProvider(endpointUri, primaryKey, databaseId);
            var profilesContainerManager = provider.GetRequiredService<IContainerManager<Entities.UserProfile>>();
            var container = profilesContainerManager.EnsureContainerAsync().GetAwaiter().GetResult();
            return new NoSQL.UserProfileRepository(container);
        }

        public static IServiceCollection AddStripeServices(this IServiceCollection services)
        {
            // Register Stripe services here
            services.AddTransient<ICreateCustomer, CreateCustomer>();
            services.AddScoped<IPriceService, PricesService>();
            services.AddScoped<IPriceServiceWrapper, PricesServiceWrapper>();
            services.AddScoped<ICheckoutSessionService, CheckoutSessionsService>();
            services.AddScoped<ISubscriptionService, SubscriptionsService>();
            services.AddScoped<IListSubscriptions, ListSubscriptions>();
            services.AddScoped<ICancelSubscription, CancelSubscriptionService>();
            services.AddScoped<IUpdateSubscription, UpdateSubscriptionService>();
            services.AddScoped<IInvoicePreview>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InvoicePreviewService>>();
                var http = new HttpClient();
                var keyProvider = sp.GetRequiredService<IStripeApiKeyProvider>();
                return new InvoicePreviewService(logger, http, keyProvider);
            });
            services.AddScoped<IStripeApiKeyProvider, StripeApiKeyProvider>();
            services.AddScoped<IStripeInvoiceServiceHelper>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<StripeInvoiceServiceHelper>>();
                var http = new HttpClient();
                var keyProvider = sp.GetRequiredService<IStripeApiKeyProvider>();
                return new StripeInvoiceServiceHelper(logger, http, keyProvider);
            });
            services.AddScoped<IStripeWebhookSecretProvider, StripeWebhookSecretProvider>();
            services.AddScoped<IStripeWebhookHandler, StripeWebhookHandler>();
            return services;
        }

        /// <summary>
        /// Registers orchestrators for Stripe flows that coordinate between identity, persistence, and Stripe APIs.
        /// </summary>
        public static IServiceCollection AddStripeOrchestrators(this IServiceCollection services)
        {
            services.AddScoped<IEnsureCustomerForUser, EnsureCustomerForUser>();
            return services;
        }

        /// <summary>
        /// Registers user profile services for user identity management.
        /// </summary>
        public static IServiceCollection AddUserProfileServices(this IServiceCollection services)
        {
            services.AddScoped<API.IUserProfileService, API.UserProfileService>();
            return services;
        }

        /// <summary>
        /// Registers Ink Stained Wretch domain services and Cosmos container managers for
        /// author-management localization. Includes:
        /// <list type="bullet">
        /// <item><description><see cref="ILocalizationTextProvider"/> implementation</description></item>
        /// <item><description>Typed <see cref="IContainerManager{T}"/> for each author management POCO (partition key: Culture)</description></item>
        /// </list>
        /// Call this after registering a singleton <see cref="Microsoft.Azure.Cosmos.Database"/> in DI.
        /// </summary>
        /// <param name="services">The DI service collection.</param>
        /// <returns>The modified <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddInkStainedWretchServices(this IServiceCollection services)
        {
            services.AddTransient<ILocalizationTextProvider, LocalizationTextProvider>();
            services.AddTransient<IContainerManager<ArticleForm>, AuthorManagementContainerManager<ArticleForm>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<ArticleForm>(database, "ArticleForm");
            });

            services.AddTransient<IContainerManager<BookForm>, AuthorManagementContainerManager<BookForm>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<BookForm>(database, "BookForm");
            });

            services.AddTransient<IContainerManager<BookList>, AuthorManagementContainerManager<BookList>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<BookList>(database, "BookList");
            });

            services.AddTransient<IContainerManager<Checkout>, AuthorManagementContainerManager<Checkout>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<Checkout>(database, "Checkout");
            });

            services.AddTransient<IContainerManager<DomainRegistration>, AuthorManagementContainerManager<DomainRegistration>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<DomainRegistration>(database, "DomainRegistration");
            });

            services.AddTransient<IContainerManager<ErrorPage>, AuthorManagementContainerManager<ErrorPage>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<ErrorPage>(database, "ErrorPage");
            });

            services.AddTransient<IContainerManager<ImageManager>, AuthorManagementContainerManager<ImageManager>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<ImageManager>(database, "ImageManager");
            });

            services.AddTransient<IContainerManager<LoginRegister>, AuthorManagementContainerManager<LoginRegister>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<LoginRegister>(database, "LoginRegister");
            });

            services.AddTransient<IContainerManager<Navbar>, AuthorManagementContainerManager<Navbar>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<Navbar>(database, "Navbar");
            });

            services.AddTransient<IContainerManager<ThankYou>, AuthorManagementContainerManager<ThankYou>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<ThankYou>(database, "ThankYou");
            });

            services.AddTransient<IContainerManager<AuthorRegistration>, AuthorManagementContainerManager<AuthorRegistration>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<AuthorRegistration>(database, "AuthorRegistration");
            });
            services.AddTransient<IContainerManager<AuthGuard>, AuthorManagementContainerManager<AuthGuard>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<AuthGuard>(database, "AuthGuard");
            });
            services.AddTransient<IContainerManager<PenguinRandomHouseAuthorList>, AuthorManagementContainerManager<PenguinRandomHouseAuthorList>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<PenguinRandomHouseAuthorList>(database, "PenguinRandomHouseAuthorList");
            });
            services.AddTransient<IContainerManager<PenguinRandomHouseAuthorDetail>, AuthorManagementContainerManager<PenguinRandomHouseAuthorDetail>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<PenguinRandomHouseAuthorDetail>(database, "PenguinRandomHouseAuthorDetail");
            });
            services.AddTransient<IContainerManager<ArticleList>, AuthorManagementContainerManager<ArticleList>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<ArticleList>(database, "ArticleList");
            });
            services.AddTransient<IContainerManager<AuthorDocList>, AuthorManagementContainerManager<AuthorDocList>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<AuthorDocList>(database, "AuthorDocList");
            });
            services.AddTransient<IContainerManager<AuthorMainForm>, AuthorManagementContainerManager<AuthorMainForm>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<AuthorMainForm>(database, "AuthorMainForm");
            });
            services.AddTransient<IContainerManager<ChooseCulture>, AuthorManagementContainerManager<ChooseCulture>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<ChooseCulture>(database, "ChooseCulture");
            });
            services.AddTransient<IContainerManager<ChooseSubscription>, AuthorManagementContainerManager<ChooseSubscription>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<ChooseSubscription>(database, "ChooseSubscription");
            });
            services.AddTransient<IContainerManager<CountdownIndicator>, AuthorManagementContainerManager<CountdownIndicator>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<CountdownIndicator>(database, "CountdownIndicator");
            });
            services.AddTransient<IContainerManager<DomainInput>, AuthorManagementContainerManager<DomainInput>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<DomainInput>(database, "DomainInput");
            });
            services.AddTransient<IContainerManager<DomainRegistrationsList>, AuthorManagementContainerManager<DomainRegistrationsList>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<DomainRegistrationsList>(database, "DomainRegistrationsList");
            });
            services.AddTransient<IContainerManager<OpenLibraryAuthorForm>, AuthorManagementContainerManager<OpenLibraryAuthorForm>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<OpenLibraryAuthorForm>(database, "OpenLibraryAuthorForm");
            });
            services.AddTransient<IContainerManager<SocialForm>, AuthorManagementContainerManager<SocialForm>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<SocialForm>(database, "SocialForm");
            });
            services.AddTransient<IContainerManager<SocialList>, AuthorManagementContainerManager<SocialList>>(servicesProvider =>
            {
                var database = servicesProvider.GetRequiredService<Microsoft.Azure.Cosmos.Database>();
                return new AuthorManagementContainerManager<SocialList>(database, "SocialList");
            });
            return services;
        }

        /// <summary>
        /// Registers the Locales container and ILocaleDataService in DI.
        /// Requires a singleton Microsoft.Azure.Cosmos.Database in DI.
        /// </summary>
        public static IServiceCollection AddLocaleDataService(this IServiceCollection services)
        {
            services.AddTransient<IContainerManager<Entities.Locale>>(sp =>
                new LocalesContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));

            services.AddSingleton<ILocaleDataService>(sp =>
            {
                var localesContainer = sp.GetRequiredService<IContainerManager<Entities.Locale>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                var localeRepo = new NoSQL.LocaleRepository(localesContainer);
                return new API.LocaleDataService(localeRepo);
            });
            return services;
        }

        /// <summary>
        /// Registers LocaleRepository in DI by ensuring the Locales container exists.
        /// Requires Microsoft.Azure.Cosmos.Database in DI.
        /// </summary>
        public static IServiceCollection AddLocaleRepository(this IServiceCollection services)
        {
            services.AddTransient<IContainerManager<Entities.Locale>>(sp =>
                new LocalesContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));

            services.AddSingleton(sp =>
            {
                var localesContainer = sp.GetRequiredService<IContainerManager<Entities.Locale>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.LocaleRepository(localesContainer);
            });
            return services;
        }

        /// <summary>
        /// Registers UserProfile repository in DI by ensuring the UserProfiles container exists (partition key /Upn).
        /// Requires Microsoft.Azure.Cosmos.Database in DI.
        /// </summary>
        public static IServiceCollection AddUserProfileRepository(this IServiceCollection services)
        {
            services.AddTransient<IContainerManager<Entities.UserProfile>>(sp =>
                new UserProfilesContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));

            services.AddSingleton<API.IUserProfileRepository>(sp =>
            {
                var container = sp.GetRequiredService<IContainerManager<Entities.UserProfile>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.UserProfileRepository(container);
            });
            return services;
        }

        /// <summary>
        /// Registers author-related repositories (Author, Book, Article, Social) in DI.
        /// Requires Microsoft.Azure.Cosmos.Database and CosmosClient in DI. Ensures containers exist.
        /// </summary>
        public static IServiceCollection AddAuthorRepositories(this IServiceCollection services)
        {
            // Ensure the container managers are available
            services.AddTransient<IContainerManager<Entities.Author>, AuthorsContainerManager>();
            services.AddTransient<IContainerManager<Entities.Book>>(sp =>
                new BooksContainerManager(
                    sp.GetRequiredService<Microsoft.Azure.Cosmos.CosmosClient>(),
                    sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));
            services.AddTransient<IContainerManager<Entities.Article>>(sp =>
                new ArticlesContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));
            services.AddTransient<IContainerManager<Entities.Social>>(sp =>
                new SocialsContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));

            // Register concrete repositories as singletons after ensuring containers
            services.AddSingleton(sp =>
            {
                var c = sp.GetRequiredService<IContainerManager<Entities.Author>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.AuthorRepository(c);
            });
            services.AddSingleton(sp =>
            {
                var c = sp.GetRequiredService<IContainerManager<Entities.Book>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.GenericRepository<Entities.Book>(c);
            });
            services.AddSingleton(sp =>
            {
                var c = sp.GetRequiredService<IContainerManager<Entities.Article>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.GenericRepository<Entities.Article>(c);
            });
            services.AddSingleton(sp =>
            {
                var c = sp.GetRequiredService<IContainerManager<Entities.Social>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.GenericRepository<Entities.Social>(c);
            });
            return services;
        }

        /// <summary>
        /// Registers Image API repositories and ensures containers exist for tiers, memberships, usage, and images.
        /// </summary>
        public static IServiceCollection AddImageApiRepositories(this IServiceCollection services)
        {
            services.AddTransient<IContainerManager<Entities.ImageAPI.ImageStorageTier>>(sp =>
                new ImageStorageTiersContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));
            services.AddTransient<IContainerManager<Entities.ImageAPI.ImageStorageTierMembership>>(sp =>
                new ImageStorageTierMembershipsContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));
            services.AddTransient<IContainerManager<Entities.ImageAPI.ImageStorageUsage>>(sp =>
                new ImageStorageUsagesContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));
            services.AddTransient<IContainerManager<Entities.ImageAPI.Image>>(sp =>
                new NoSQL.ImageAPI.ImagesContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));

            // Register custom repositories as singletons after ensuring containers
            services.AddSingleton<API.ImageAPI.IImageStorageTierRepository>(sp =>
            {
                var c = sp.GetRequiredService<IContainerManager<Entities.ImageAPI.ImageStorageTier>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.ImageStorageTierRepository(c);
            });

            services.AddSingleton<API.ImageAPI.IImageStorageTierMembershipRepository>(sp =>
            {
                var c = sp.GetRequiredService<IContainerManager<Entities.ImageAPI.ImageStorageTierMembership>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.ImageStorageTierMembershipRepository(c);
            });

            services.AddSingleton<API.ImageAPI.IImageStorageUsageRepository>(sp =>
            {
                var c = sp.GetRequiredService<IContainerManager<Entities.ImageAPI.ImageStorageUsage>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.ImageStorageUsageRepository(c);
            });

            services.AddSingleton<InkStainedWretch.OnePageAuthorAPI.API.ImageAPI.IImageStorageTierRepository>(sp =>
            {
                var c = sp.GetRequiredService<IContainerManager<Entities.ImageAPI.ImageStorageTier>>();
                var container = c.EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.ImageStorageTierRepository(container);
            });

            services.AddSingleton<InkStainedWretch.OnePageAuthorAPI.API.ImageAPI.IImageRepository>(sp =>
            {
                var c = sp.GetRequiredService<IContainerManager<Entities.ImageAPI.Image>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.ImageAPI.ImageRepository(c);
            });

            return services;
        }

        /// <summary>
        /// Registers Image API business services for upload, user images, deletion, and tier management operations.
        /// </summary>
        public static IServiceCollection AddImageApiServices(this IServiceCollection services)
        {
            services.AddTransient<API.ImageAPI.IImageStorageTierService, API.ImageServices.ImageStorageTierService>();
            services.AddTransient<API.ImageServices.IImageUploadService, API.ImageServices.ImageUploadService>();
            services.AddTransient<API.ImageServices.IUserImageService, API.ImageServices.UserImageService>();
            services.AddTransient<API.ImageServices.IImageDeleteService, API.ImageServices.ImageDeleteService>();

            return services;
        }

        /// <summary>
        /// Registers Penguin Random House API services for external API integration.
        /// </summary>
        public static IServiceCollection AddPenguinRandomHouseServices(this IServiceCollection services)
        {
            // Register configuration
            services.AddSingleton<IPenguinRandomHouseConfig, PenguinRandomHouseConfig>();

            // Register HTTP client and service
            services.AddHttpClient<IPenguinRandomHouseService, PenguinRandomHouseService>();

            return services;
        }

        /// <summary>
        /// Registers Amazon Product Advertising API services for external API integration.
        /// </summary>
        public static IServiceCollection AddAmazonProductServices(this IServiceCollection services)
        {
            // Register configuration
            services.AddSingleton<IAmazonProductConfig, AmazonProductConfig>();

            // Register HTTP client and service
            services.AddHttpClient<IAmazonProductService, AmazonProductService>();

            return services;
        }

        /// <summary>
        /// Registers JWT authentication services including validation and token introspection.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns>The IServiceCollection for chaining.</returns>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
        {
            // Register HTTP client for token introspection
            services.AddHttpClient();

            // Register authentication services
            services.AddScoped<InkStainedWretch.OnePageAuthorAPI.Authentication.IJwtValidationService,
                              InkStainedWretch.OnePageAuthorAPI.Authentication.JwtValidationService>();
            services.AddScoped<InkStainedWretch.OnePageAuthorAPI.Authentication.ITokenIntrospectionService,
                              InkStainedWretch.OnePageAuthorAPI.Authentication.TokenIntrospectionService>();

            return services;
        }

        /// <summary>
        /// Registers domain registration services for domain management.
        /// </summary>
        public static IServiceCollection AddDomainRegistrationServices(this IServiceCollection services)
        {
            services.AddScoped<Interfaces.IUserIdentityService, API.UserIdentityService>();
            services.AddScoped<Interfaces.IDomainRegistrationService, API.DomainRegistrationService>();
            return services;
        }

        /// <summary>
        /// Registers domain registration repository and container manager.
        /// Call this after registering a singleton Database in DI.
        /// </summary>
        public static IServiceCollection AddDomainRegistrationRepository(this IServiceCollection services)
        {
            services.AddTransient<IContainerManager<Entities.DomainRegistration>>(sp =>
                new DomainRegistrationsContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));

            services.AddSingleton<Interfaces.IDomainRegistrationRepository>(sp =>
            {
                var container = sp.GetRequiredService<IContainerManager<Entities.DomainRegistration>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.DomainRegistrationRepository(container);
            });

            return services;
        }

        /// <summary>
        /// Registers StateProvince repository services for state/province management.
        /// Call this after registering a singleton Database in DI.
        /// </summary>
        public static IServiceCollection AddStateProvinceRepository(this IServiceCollection services)
        {
            services.AddTransient<IContainerManager<Entities.StateProvince>>(sp =>
                new NoSQL.StateProvincesContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));

            // Register the old Guid-based repository for backward compatibility
            services.AddSingleton<API.IStateProvinceRepository>(sp =>
            {
                var container = sp.GetRequiredService<IContainerManager<Entities.StateProvince>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.StateProvinceRepository(container);
            });

            // Register the new string-based repository
            services.AddSingleton<API.IStringStateProvinceRepository>(sp =>
            {
                var container = sp.GetRequiredService<IContainerManager<Entities.StateProvince>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.StringStateProvinceRepository(container);
            });

            return services;
        }

        /// <summary>
        /// Registers StateProvince services for state/province management.
        /// </summary>
        public static IServiceCollection AddStateProvinceServices(this IServiceCollection services)
        {
            services.AddScoped<Interfaces.IStateProvinceService, API.StateProvinceService>();
            return services;
        }

        /// <summary>
        /// Registers DNS zone service for Azure DNS zone management.
        /// </summary>
        public static IServiceCollection AddDnsZoneService(this IServiceCollection services)
        {
            services.AddScoped<Interfaces.IDnsZoneService, API.DnsZoneService>();
            return services;
        }

        /// <summary>
        /// Registers Azure Front Door services for domain management.
        /// </summary>
        public static IServiceCollection AddFrontDoorServices(this IServiceCollection services)
        {
            services.AddScoped<Interfaces.IFrontDoorService, API.FrontDoorService>();
            return services;
        }

        /// <summary>
        /// Registers Google Domains service for domain registration via Google Domains API.
        /// </summary>
        public static IServiceCollection AddGoogleDomainsService(this IServiceCollection services)
        {
            services.AddScoped<Interfaces.IGoogleDomainsService, API.GoogleDomainsService>();
            return services;
        }

        /// <summary>
        /// Registers Language repository services for language management.
        /// Call this after registering a singleton Database in DI.
        /// </summary>
        public static IServiceCollection AddLanguageRepository(this IServiceCollection services)
        {
            services.AddTransient<IContainerManager<Entities.Language>>(sp =>
                new NoSQL.LanguagesContainerManager(sp.GetRequiredService<Microsoft.Azure.Cosmos.Database>()));

            services.AddSingleton<API.ILanguageRepository>(sp =>
            {
                var container = sp.GetRequiredService<IContainerManager<Entities.Language>>()
                    .EnsureContainerAsync().GetAwaiter().GetResult();
                return new NoSQL.LanguageRepository(container);
            });

            return services;
        }

        /// <summary>
        /// Registers Language services for language management.
        /// </summary>
        public static IServiceCollection AddLanguageServices(this IServiceCollection services)
        {
            services.AddScoped<Interfaces.ILanguageService, API.LanguageService>();
            return services;
        }
    }
}
