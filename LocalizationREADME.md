# Localization & Author Management Data Flow

## Overview
This document explains how localized UI text for the Ink Stained Wretch author management experience is structured, seeded, retrieved, and served through the Azure Function endpoint.

## Data Shape
Each culture-specific JSON file (e.g. `inkstainedwretch.en-us.json`) contains top-level objects that map one-to-one with Cosmos DB containers and C# POCOs located in `OnePageAuthorLib/entities/authormanagement`.

Example excerpt:
```json
{
  "AuthorRegistration": { "authorListTitle": "Author Information", ... },
  "LoginRegister": { "loginHeader": { "title": "Login" } },
  "ThankYou": { "title": "Thank You", "message": "..." },
  "Navbar": { "brand": "Ink Stained Wretches", ... }
}
```

## Containers & Partitioning
Each top-level section is stored in its own Cosmos DB container. The partition key for all author-management containers is `/Culture` to allow fast retrieval of the full localized set by culture.

| Container | POCO | Partition Key |
|-----------|------|---------------|
| AuthorRegistration | `AuthorRegistration` | `/Culture` |
| LoginRegister | `LoginRegister` | `/Culture` |
| ThankYou | `ThankYou` | `/Culture` |
| Navbar | `Navbar` | `/Culture` |
| DomainRegistration | `DomainRegistration` | `/Culture` |
| ErrorPage | `ErrorPage` | `/Culture` |
| ImageManager | `ImageManager` | `/Culture` |
| Checkout | `Checkout` | `/Culture` |
| BookList | `BookList` | `/Culture` |
| BookForm | `BookForm` | `/Culture` |
| ArticleForm | `ArticleForm` | `/Culture` |

(Extend this table if additional POCOs are added.)

## Seeding Process
1. The seeding project enumerates `data/` and matches files with pattern: `inkstainedwretch.{language}-{country}.json`.
2. For each file:
   - Extract culture (e.g. `en-us` becomes `en-US` canonical form when needed).
   - Ensure each container exists (if missing create with `/Culture`).
   - Insert or upsert each object with its `Culture` property populated.
3. Result: All containers now contain exactly one document per culture (or more if versioning is introduced later).

## Aggregation Model
`LocalizationText` aggregates all author-management POCOs into a single object for convenience. This allows API consumers to fetch an entire culture’s localized text in one call.

## Provider Abstraction
`ILocalizationTextProvider` defines:
```csharp
Task<LocalizationText> GetLocalizationTextAsync(string culture);
```
Implementation: `LocalizationTextProvider`
- Validates culture via `CultureInfo.GetCultureInfo`.
- Queries each container by partition key.
- Fallback chain: exact culture -> first culture with matching language prefix (e.g. request `en-GB`, use `en-US` if present) -> neutral language (`en`) -> empty placeholder.
- Always returns a non-null object; placeholder carries requested culture if no data found.

### Fallback Logic
Resolution order for a request like `en-GB`:
1. Exact match: `en-GB`
2. Any other `en-XX` variant (first one encountered, deterministic by Cosmos query paging)
3. Neutral language: `en`
4. Empty typed object (all default strings) with `Culture` = `en-GB`

The returned object’s `Culture` property is normalized to the originally requested specific culture even when data is sourced from a different language-region variant.

## Azure Function Endpoint
`LocalizedText` function (HTTP GET):
```
GET /api/localizedtext/{culture}
```
Response: `200 OK` with `LocalizationText` JSON or `400 Bad Request` if invalid culture.

### Sample Request
```
GET https://localhost:7071/api/localizedtext/en-US
```
### Sample (truncated) Response
```json
{
  "authorRegistration": { "authorListTitle": "Author Information", ... },
  "loginRegister": { "loginHeader_title": "Login", ... },
  "thankYou": { "title": "Thank You", "message": "Thank you for your purchase!" },
  "navbar": { "brand": "Ink Stained Wretches", ... }
}
```

## Dependency Injection
Register services via `AddInkStainedWretchServices` (after registering a `CosmosClient` + `Database`):
```csharp
services.AddInkStainedWretchServices();
```
This registers:
- `ILocalizationTextProvider` -> `LocalizationTextProvider`
- All `IContainerManager<T>` for each POCO

## Extending Localization
1. Add new JSON section to culture files.
2. Create corresponding POCO inheriting `AuthorManagementBase`.
3. Register new container manager + DI mapping.
4. Add property to `LocalizationText` and retrieval line in `LocalizationTextProvider`.

## Error Handling
- Invalid culture -> `ArgumentException` surfaced as 400.
- Missing container item -> returns empty object (never null) for resilience.

## Future Enhancements
- Caching layer (Memory / Distributed) per culture.
- Versioning or last-modified metadata.
- Batch query optimization using transactional batch (if writes grouped).
- Additional hierarchical fallback (e.g. regional -> neutral -> default) if global default is introduced.

---
Maintainer Notes: Keep JSON schema changes synchronized across POCOs, seeding logic, and provider aggregation.
