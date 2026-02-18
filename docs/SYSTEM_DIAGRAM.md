# System Diagram

This diagram shows the major runtime components and integrations of the OnePageAuthor API Platform.

```mermaid
flowchart LR
  %% External actors
  user[End Users / Admins]
  client[Client Apps
  (Web / Mobile / Admin tools)]

  %% Edge / identity
  entra[Microsoft Entra ID
  (JWT issuer)]
  frontDoor[Azure Front Door]
  dns[Azure DNS]

  %% Compute (Azure Functions)
  subgraph functions[Azure Functions Apps (Isolated Worker)]
    fa[function-app
    (Core author/content APIs)]
    imageApi[ImageAPI
    (Image upload/metadata)]
    isw[InkStainedWretchFunctions
    (Domains, localization, external integrations)]
    stripeFx[InkStainedWretchStripe
    (Stripe billing/subscriptions)]
  end

  %% Data plane
  cosmos[Azure Cosmos DB
  (NoSQL, repositories)]
  blob[Azure Blob Storage
  (Images/media)]

  %% Third-party integrations
  stripe[Stripe API]
  prh[Penguin Random House API]
  amazon[Amazon Product Advertising API]

  %% Observability
  appi[Application Insights]

  %% Flows
  user --> client
  client -->|HTTPS| frontDoor
  dns -. domain resolution .-> frontDoor

  client -->|Obtain JWT| entra
  entra -->|JWT| client

  frontDoor -->|Routes requests| fa
  frontDoor -->|Routes requests| imageApi
  frontDoor -->|Routes requests| isw
  frontDoor -->|Routes requests| stripeFx

  fa <--> cosmos
  isw <--> cosmos
  stripeFx <--> cosmos
  imageApi <--> blob

  stripeFx <--> stripe
  isw <--> prh
  isw <--> amazon

  fa -->|Telemetry| appi
  imageApi -->|Telemetry| appi
  isw -->|Telemetry| appi
  stripeFx -->|Telemetry| appi
```

## Notes

- Authentication is performed via JWTs issued by Microsoft Entra ID; protected endpoints validate tokens.
- Cosmos DB access is centralized behind repository patterns in the shared library.
- Image operations use Blob Storage for content and Cosmos DB for metadata where applicable.
- Stripe billing logic is isolated in the Stripe-focused Functions app; webhook handling also lives there.

## Related docs

- See docs/DEPLOYMENT_ARCHITECTURE.md for deployment-focused architecture details.
- See docs/Complete-System-Documentation.md for a deeper platform overview.
