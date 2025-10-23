# OnePageAuthor Complete System Documentation

*Generated on 2025-09-29 14:04:52 UTC*

This comprehensive documentation covers all components, APIs, and utilities in the OnePageAuthor system.

## System Overview

The OnePageAuthor system consists of multiple Azure Functions, core libraries, and utility applications that work together to provide a complete author management and content publishing platform.

## Architecture Components

### Azure Functions (API Layer)
- **ImageAPI**: Image upload, management and retrieval
- **InkStainedWretchFunctions**: Domain registration and external integrations
- **InkStainedWretchStripe**: Payment processing and subscription management
- **function-app**: Core author data and localization services

### Libraries (Business Logic Layer)
- **OnePageAuthorLib**: Core business logic, entities, and data access services

### Utilities (Data Management Layer)
- **SeedAPIData**: API data initialization
- **SeedImageStorageTiers**: Storage tier configuration
- **SeedInkStainedWretchesLocale**: Comprehensive localization for all containers (North America: US, CA, MX in EN, ES, FR, AR, ZH-CN, ZH-TW)

### Testing (Quality Assurance Layer)  
- **OnePageAuthor.Test**: Unit and integration tests
- **IntegrationTestAuthorDataService**: Author data service validation

## Authentication

All API endpoints require JWT Bearer token authentication:

`
Authorization: Bearer <your-jwt-token>
`

## Project Details

### Azure Functions

#### ImageAPI

Image management API for uploading, retrieving, and deleting user images

**Functions:**
- `Delete`: Azure Function for deleting an image by its ID. Uses the ImageDeleteService for business logic.
  - `HttpRequest)`: Deletes a user's image by ID.
- `Upload`: Azure Function for uploading image files to Azure Blob Storage. Uses the ImageUploadService for business logic and validation.
  - `HttpRequest)`: Uploads an image file to Azure Blob Storage with subscription tier validation.
- `User`: Azure Function for retrieving all images uploaded by the authenticated user. Uses the UserImageService for business logic.
  - `HttpRequest)`: Retrieves all images uploaded by the authenticated user.
- `WhoAmI`: Azure Function for retrieving information about the authenticated user. Returns user identity and claims information from JWT token.
  - `HttpRequest)`: Returns information about the authenticated user from JWT token claims.
- `FunctionExecutorHostBuilderExtensions`: System.Xml.XmlElement
  - `IHostBuilder)`: Configures an optimized function executor to the invocation pipeline.
- `FunctionExecutorAutoStartup`: System.Xml.XmlElement
  - `IHostBuilder)`: System.Xml.XmlElement
- `GeneratedFunctionMetadataProvider`: System.Xml.XmlElement
  - `String)`
  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
- `WorkerHostBuilderFunctionMetadataProviderExtension`: System.Xml.XmlElement
  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
- `FunctionMetadataProviderAutoStartup`: System.Xml.XmlElement
  - `IHostBuilder)`: System.Xml.XmlElement

#### InkStainedWretchFunctions

Core application functions for domain registration and external API integration

**Functions:**
- `DomainRegistrationFunction`: HTTP endpoint to create and manage domain registrations.
  - `CreateDomainRegistrationRequest)`: Creates a new domain registration for the authenticated user.
  - `HttpRequest)`: Gets all domain registrations for the authenticated user.
  - `String)`: Gets a specific domain registration by ID for the authenticated user.
- `PenguinRandomHouseFunction`: Azure Function for calling Penguin Random House API
  - `String)`: Searches for authors by name and returns the unmodified JSON response from Penguin Random House API.
  - `String)`: Gets titles by author key and returns the unmodified JSON response from Penguin Random House API.
- `LocalizedText`: System.Xml.XmlElement
  - `ILocalizationTextProvider)`: System.Xml.XmlElement
  - `String)`: Handles HTTP GET requests for localized text.
- `FunctionExecutorHostBuilderExtensions`: System.Xml.XmlElement
  - `IHostBuilder)`: Configures an optimized function executor to the invocation pipeline.
- `FunctionExecutorAutoStartup`: System.Xml.XmlElement
  - `IHostBuilder)`: System.Xml.XmlElement
- `GeneratedFunctionMetadataProvider`: System.Xml.XmlElement
  - `String)`
  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
- `WorkerHostBuilderFunctionMetadataProviderExtension`: System.Xml.XmlElement
  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
- `FunctionMetadataProviderAutoStartup`: System.Xml.XmlElement
  - `IHostBuilder)`: System.Xml.XmlElement

#### InkStainedWretchStripe

Stripe payment processing functions for subscription management and billing

**Functions:**
- `CreateStripeCustomer`: Azure Function for creating a Stripe customer. Handles HTTP POST requests, validates the incoming payload, and delegates customer creation logic.
- `FunctionExecutorHostBuilderExtensions`: System.Xml.XmlElement
  - `IHostBuilder)`: Configures an optimized function executor to the invocation pipeline.
- `FunctionExecutorAutoStartup`: System.Xml.XmlElement
  - `IHostBuilder)`: System.Xml.XmlElement
- `GeneratedFunctionMetadataProvider`: System.Xml.XmlElement
  - `String)`
  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
- `WorkerHostBuilderFunctionMetadataProviderExtension`: System.Xml.XmlElement
  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
- `FunctionMetadataProviderAutoStartup`: System.Xml.XmlElement
  - `IHostBuilder)`: System.Xml.XmlElement

#### function-app

Main application functions for author data and localization services

**Functions:**
- `FunctionExecutorHostBuilderExtensions`: System.Xml.XmlElement
  - `IHostBuilder)`: Configures an optimized function executor to the invocation pipeline.
- `FunctionExecutorAutoStartup`: System.Xml.XmlElement
  - `IHostBuilder)`: System.Xml.XmlElement
- `GeneratedFunctionMetadataProvider`: System.Xml.XmlElement
  - `String)`
  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
- `WorkerHostBuilderFunctionMetadataProviderExtension`: System.Xml.XmlElement
  - `IHostBuilder)`: Adds the GeneratedFunctionMetadataProvider to the service collection. During initialization, the worker will return generated function metadata instead of relying on the Azure Functions host for function indexing.
- `FunctionMetadataProviderAutoStartup`: System.Xml.XmlElement
  - `IHostBuilder)`: System.Xml.XmlElement


### Libraries

#### OnePageAuthorLib

Core library containing business logic, entities, and data services

**Key Components:**
- `Article`: Represents an article authored by the author.
- `AuthorResponse`: Represents the response containing author data for the One Page Author API.
- `Book`: Represents a book authored by the author.
- `DomainRegistrationService`: Service for managing domain registration operations for authenticated users.
- `IImageDeleteService`: Service interface for handling image deletion operations.
- `IImageUploadService`: Service interface for handling image upload operations.
- `ImageDeleteService`: Service for handling image deletion operations.
- `ImageUploadService`: Service for handling image upload operations with tier-based validation.
- `IUserImageService`: Service interface for handling user image operations.
- `ImageUploadResult`: Result of an image upload operation.
- `UserImagesResult`: Result of retrieving user images.
- `ImageDeleteResult`: Result of an image deletion operation.
- `ServiceResult`: Base class for service operation results.
- `UploadImageResponse`: Response model for successful image upload.
- `UserImageResponse`: Response model for user image list item.
- `UserImageService`: Service for handling user image retrieval operations.
- `LocaleResponse`: Represents localized strings for the One Page Author API UI.
- `SocialLink`: Represents a social media link for the author.
- `UserProfileService`: Service for ensuring user profiles exist for authenticated users.
- `IAuthorRepository`: Interface for AuthorRepository, supports querying by domain and locale properties.
- `IContainerManager`1`: Generic interface for Cosmos DB container managers.
- `ICosmosDatabaseManager`: Public interface for CosmosDatabaseManager.
- `ICreateStripeCustomer`: Defines a contract for creating Stripe customers from a request payload.
- `IGenericRepository`1`: Generic repository interface for entities with Guid-based id and authorId.
- `ILocaleDataService`: Provides methods for retrieving locale data from the data source.
- `ILocaleRepository`: Interface for LocaleRepository, supports querying by id and other properties.
- `IUserProfileService`: Interface for user profile management services.
- `JwtAuthenticationHelper`: Helper class for JWT authentication in Azure Functions
- `JwtDebugHelper`: Utility class for debugging JWT tokens
- `TokenValidationTest`: Simple test class to verify token validation logic without requiring a full test framework
- `Article`: Represents an article and its associated metadata for the One Page Author API.
- `AuthGuard`: Represents authentication guard information for UI display when user authentication is required
- `LocalizationText`: Represents all localized text for the author management UI, matching the structure of inkstainedwretch.language-country.json.
- `LocalizationTextProvider`: System.Xml.XmlElement
- `PenguinRandomHouseAuthorDetail`: Represents localized text strings for the Penguin Random House author detail interface. This class contains UI text and field labels that can be localized for different cultures/languages. Used when displaying detailed information about a specific author from Penguin Random House. Inherits from AuthorManagementBase to include Culture and id properties for localization support.
- `PenguinRandomHouseAuthorList`: Represents localized text strings for the Penguin Random House author list interface. This class contains UI text that can be localized for different cultures/languages. Inherits from AuthorManagementBase to include Culture and id properties for localization support.
- `Book`: Represents a book and its associated metadata for the One Page Author API.
- `DomainRegistration`: Represents a domain registration request entity stored in Cosmos DB.
- `Domain`: Represents domain information for registration.
- `ContactInformation`: Represents contact information for domain registration.
- `DomainRegistrationStatus`: Status of a domain registration request.
- `CreateDomainRegistrationRequest`: Data transfer object for creating a domain registration request.
- `DomainDto`: Data transfer object for domain information.
- `ContactInformationDto`: Data transfer object for contact information.
- `DomainRegistrationResponse`: Data transfer object for domain registration response.
- `CreateDomainRegistrationRequest`: Data transfer object for creating a domain registration request.
- `DomainDto`: Data transfer object for domain information.
- `ContactInformationDto`: Data transfer object for contact information.
- `DomainRegistrationResponse`: Data transfer object for domain registration response.
- `Image`: Represents an uploaded image file stored in Azure Blob Storage.
- `ImageStorageTier`: Represents an image storage plan/tier with a friendly name and a monthly cost.
- `ImageStorageTierMembership`: Represents a user's membership/assignment to an ImageStorageTier.
- `Locale`: Entity representing a locale, inherits from LocaleResponse and adds an id property.
- `Social`: Represents a social profile or link associated with an author for the One Page Author API.
- `UserProfile`: Represents the authenticated user's identity details and linkage to Stripe.
- `ILocalizationTextProvider`: Abstraction for retrieving localized author management UI text objects aggregated across multiple Cosmos DB containers for a given culture.
- `IDomainRegistrationRepository`: Interface for domain registration repository operations.
- `IDomainRegistrationService`: Interface for domain registration service operations.
- `ArticlesContainerManager`: Manages the Cosmos DB container for Article entities.
- `AuthorRepository`: Repository for Author entities, supports querying by domain and locale properties.
- `AuthorsContainerManager`: Manages the Cosmos DB container for Author entities.
- `BooksContainerManager`: Manages the Cosmos DB container for Book entities.
- `CosmosDatabaseManager`: Manages creation and access to an Azure Cosmos NoSQL database.
- `DomainRegistrationRepository`: Repository for DomainRegistration with partition key Upn.
- `DomainRegistrationsContainerManager`: Manages the Cosmos DB container for DomainRegistration entities.
- `GenericRepository`1`: Generic repository for entities with Guid-based id and authorId, converting to string as necessary.
- `ImagesContainerManager`: Manages the Cosmos DB container for Image entities.
- `ImageStorageTierMembershipsContainerManager`: Manages the Cosmos DB container for ImageStorageTierMembership entities.
- `ImageStorageTiersContainerManager`: Manages the Cosmos DB container for ImageStorageTier entities.
- `LocaleRepository`: Repository for Locale entities, supports querying by id.
- `LocalesContainerManager`: Manages the Cosmos DB container for Locale entities.
- `SocialsContainerManager`: Manages the Cosmos DB container for Social entities.
- `UserProfileRepository`: Repository for UserProfile with partition key Upn.
- `UserProfilesContainerManager`: Manages the Cosmos DB container for UserProfile entities.
- `ServiceFactory`: Provides factory methods for dependency injection and repository/service creation for the OnePageAuthorAPI.
- `IPenguinRandomHouseConfig`: Configuration interface for Penguin Random House API settings
- `IPenguinRandomHouseService`: Service interface for interacting with Penguin Random House API
- `PenguinRandomHouseConfig`: Configuration implementation for Penguin Random House API settings Reads settings from local configuration (appsettings.json, local.settings.json, environment variables)
- `PenguinRandomHouseService`: Service implementation for calling Penguin Random House API
- `CreateCustomer`: Basic implementation that builds an initialized response object from the request.
- `ListSubscriptions`: Service for listing Stripe subscriptions following Stripe's sample patterns.
- `SubscriptionMappers`: Mapping helpers from Stripe entities to our DTOs. Kept simple for unit testing.
- `IStripeCheckoutSessionService`: Abstraction for creating Stripe Checkout Sessions.
- `IStripePriceServiceWrapper`: System.Xml.XmlElement
- `IListSubscriptions`: Abstraction for listing Stripe subscriptions.
- `CancelSubscriptionRequest`: Optional settings when cancelling a subscription.
- `CancelSubscriptionResponse`: Result of cancelling a subscription.
- `CreateCheckoutSessionRequest`: Represents the payload for creating a Stripe Checkout Session.
- `CreateCheckoutSessionResponse`: Represents the response payload after creating a Stripe Checkout Session.
- `CreateCustomerRequest`: Represents the payload for creating a Stripe customer.
- `CreateCustomerResponse`: Represents the result of a request to create a Stripe customer.
- `CreateSubscriptionRequest`: Represents the payload for creating a subscription from a Stripe price.
- `GetCheckoutSessionResponse`: Represents details of a Stripe Checkout Session for retrieval endpoints.
- `SubscriptionCreateResponse`: Response for creating a subscription, returning identifiers needed by the client.
- `SubscriptionListResponse`: Lightweight DTO wrapper for Stripe subscriptions that is easier for clients to consume and includes pagination helpers.
- `SubscriptionDto`: Simplified subscription model exposing commonly used fields.
- `SubscriptionItemDto`: Simplified representation of a subscription item (price/product + quantity).
- `SubscriptionPlan`: Represents a subscription plan that serializes to the structure in sample-subscription-plan.json.
- `SubscriptionPlanListResponse`: A response model analogous to StripePriceListResponse but containing SubscriptionPlan items.
- `SubscriptionsResponse`: Represents the result of a request to list Stripe subscriptions.


### Utilities & Tools

#### SeedAPIData

Data seeding utility for populating API with initial data

#### SeedImageStorageTiers

Utility for seeding image storage tier configurations

#### SeedInkStainedWretchesLocale

Comprehensive, idempotent localization seeding utility for all UI components and containers. Supports North American countries (US, CA, MX) in multiple languages: English (EN), Spanish (ES), French (FR), Arabic (AR), Simplified Chinese (ZH-CN), and Traditional Chinese (ZH-TW). Features automatic container creation, duplicate detection, and support for both standard (en-us) and extended (zh-cn-us) locale codes.


### Testing Projects

#### OnePageAuthor.Test

Unit and integration tests for the OnePageAuthor application

#### IntegrationTestAuthorDataService

Integration testing utility for author data service validation


## Development Information

### Build Configuration
All projects are configured to automatically generate XML documentation during Debug builds.

### Documentation Generation
This documentation is automatically generated from source code XML comments and can be regenerated using:
`
.\Generate-ApiDocumentation.ps1
`

### Project Statistics
- **Total Projects**: 11
- **Azure Functions**: 4
- **Libraries**: 4
- **Utilities**: 4
- **Test Projects**: 2
- **Documented Members**: 298

---

*Last updated: 2025-09-29 14:04:52 UTC*
*Generated from: OnePageAuthor API Documentation System*
