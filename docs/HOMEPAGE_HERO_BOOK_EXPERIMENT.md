# Homepage Hero Book Experiment — Frontend Integration Guide

This guide explains how to integrate the homepage hero book A/B experiment into the frontend application. The experiment lets you show a book-first hero section on the homepage instead of the default author-first layout, with the API controlling which visitors enter the treatment group.

---

## How it works

```
Frontend                              API
  │                                    │
  │  GET /api/GetAuthorData/…          │
  │  X-Session-Id: <stable id>  ──────>│
  │                                    │  1. Resolve author + books (single DB read)
  │                                    │  2. Find book with IsFeaturedHeroBook = true
  │                                    │  3. Bucket session into experiment variant
  │<──────────────────────────────────  │
  │  200 OK                            │
  │  { …authorFields…,                 │
  │    featuredBook: { … } | null,     │
  │    experiment: {                   │
  │      homepageHeroVariant:          │
  │        "control" | "featured-book-hero"
  │    }                               │
  │  }                                 │
```

1. The frontend sends a persistent session ID in the `X-Session-Id` request header.
2. The API resolves the author's data and, if the author has a book flagged as the featured hero, populates `featuredBook`.
3. The API buckets the session ID into either the `"control"` or `"featured-book-hero"` variant.
4. The frontend reads `experiment.homepageHeroVariant` and renders accordingly.

> **Safety guarantee**: `homepageHeroVariant` is **never** `"featured-book-hero"` when `featuredBook` is `null`. If the author has not configured a featured book, every visitor receives `"control"`.

---

## Session ID management

A **stable, persistent session ID** must be sent with every homepage API call. Without it the API always returns `"control"`, which is correct behaviour — the experiment requires a bucketing key to produce a sticky assignment.

### Generating and persisting the session ID

```typescript
/** Returns the existing session ID from storage, or creates and stores a new one. */
function getOrCreateSessionId(): string {
  const storageKey = 'opa_session_id';
  let id = localStorage.getItem(storageKey);
  if (!id) {
    id = crypto.randomUUID();
    localStorage.setItem(storageKey, id);
  }
  return id;
}
```

- Use `localStorage` so the ID survives page refreshes but stays browser-local.
- For authenticated users, you may substitute the user's stable identity (e.g. an Entra ID `oid` claim) — just ensure it is consistent across sessions.
- Never send a freshly-generated random value on every request; that defeats sticky bucketing.

---

## API call

```
GET /api/GetAuthorData/{tld}/{sld}/{languageName}/{regionName?}
```

| Header | Required for experiment | Notes |
|---|---|---|
| `X-Session-Id` | Yes | Stable per-user/session GUID. Absent → always `"control"` |

```typescript
async function fetchHomepageData(
  tld: string,
  sld: string,
  language: string,
  region?: string
): Promise<AuthorResponse> {
  const sessionId = getOrCreateSessionId();
  const regionSegment = region ? `/${region}` : '';

  const response = await fetch(
    `/api/GetAuthorData/${tld}/${sld}/${language}${regionSegment}`,
    { headers: { 'X-Session-Id': sessionId } }
  );

  if (!response.ok) throw new Error(`HTTP ${response.status}`);
  return response.json();
}
```

---

## Response shape

### TypeScript interfaces

```typescript
interface AuthorResponse {
  // Existing fields — unchanged
  name: string;
  welcome: string;
  aboutMe: string;
  headshot: string;
  books: Book[];
  copyright: string;
  social: SocialLink[];
  email: string;
  articles: Article[];

  // New — nullable; null when no featured book is configured for this author
  featuredBook: FeaturedBookDto | null;

  // New — always present in responses from GetAuthorData
  experiment: HomepageExperimentDto;
}

interface FeaturedBookDto {
  title: string;
  subtitle: string | null;          // optional subtitle
  authorName: string;
  description: string;
  coverImageUrl: string;
  coverImageAlt: string;            // falls back to title when not explicitly set
  primaryCtaLabel: string;          // e.g. "Buy Now"
  primaryCtaUrl: string;            // link for the primary CTA button
  secondaryCtaLabel: string | null; // optional, e.g. "Learn More"
  secondaryCtaUrl: string | null;   // optional secondary CTA link
  formats: string[];                // e.g. ["Hardcover", "Paperback", "eBook"]
}

interface HomepageExperimentDto {
  homepageHeroVariant: 'control' | 'featured-book-hero';
}
```

### Example response — control variant (no featured book)

```json
{
  "name": "Jane Author",
  "welcome": "Welcome to my page!",
  "aboutMe": "About me text…",
  "headshot": "https://cdn.example.com/jane.jpg",
  "books": [ { "title": "My Novel", "url": "https://…", "cover": "https://…" } ],
  "copyright": "© 2025 Jane Author",
  "social": [],
  "email": "jane@example.com",
  "articles": [],
  "featuredBook": null,
  "experiment": { "homepageHeroVariant": "control" }
}
```

### Example response — treatment variant (featured book configured)

```json
{
  "name": "Jane Author",
  "welcome": "Welcome to my page!",
  "aboutMe": "About me text…",
  "headshot": "https://cdn.example.com/jane.jpg",
  "books": [ { "title": "The Great Novel", "url": "https://…", "cover": "https://…" } ],
  "copyright": "© 2025 Jane Author",
  "social": [],
  "email": "jane@example.com",
  "articles": [],
  "featuredBook": {
    "title": "The Great Novel",
    "subtitle": "A Story of Discovery",
    "authorName": "Jane Author",
    "description": "A sweeping tale set across three continents…",
    "coverImageUrl": "https://cdn.example.com/great-novel-cover.jpg",
    "coverImageAlt": "The Great Novel cover art",
    "primaryCtaLabel": "Buy Now",
    "primaryCtaUrl": "https://bookshop.org/great-novel",
    "secondaryCtaLabel": "Read an excerpt",
    "secondaryCtaUrl": "https://janeauthor.com/excerpt",
    "formats": ["Hardcover", "Paperback", "eBook"]
  },
  "experiment": { "homepageHeroVariant": "featured-book-hero" }
}
```

---

## Rendering logic

```typescript
function renderHomepage(data: AuthorResponse): void {
  const variant = data.experiment.homepageHeroVariant;

  if (variant === 'featured-book-hero' && data.featuredBook) {
    renderBookHero(data.featuredBook, data);
  } else {
    renderAuthorHero(data);
  }
}

function renderBookHero(book: FeaturedBookDto, author: AuthorResponse): void {
  // Book-first hero layout:
  // - Large cover image  (book.coverImageUrl / book.coverImageAlt)
  // - Title + optional subtitle (book.title, book.subtitle)
  // - Author byline      (book.authorName)
  // - Description        (book.description)
  // - Primary CTA button (book.primaryCtaLabel → book.primaryCtaUrl)
  // - Optional secondary CTA (book.secondaryCtaLabel → book.secondaryCtaUrl)
  // - Format badges      (book.formats)
}

function renderAuthorHero(author: AuthorResponse): void {
  // Existing author-first layout — no changes needed
}
```

> **Always guard on both fields.** Even though the API never sends `"featured-book-hero"` when `featuredBook` is null, defensive code (`variant === 'featured-book-hero' && data.featuredBook`) protects against future API changes or locally mocked data.

---

## React example

```tsx
import { useEffect, useState } from 'react';

function getOrCreateSessionId(): string {
  const key = 'opa_session_id';
  let id = localStorage.getItem(key);
  if (!id) { id = crypto.randomUUID(); localStorage.setItem(key, id); }
  return id;
}

function useHomepageData(tld: string, sld: string, language: string) {
  const [data, setData] = useState<AuthorResponse | null>(null);

  useEffect(() => {
    fetchHomepageData(tld, sld, language)
      .then(setData)
      .catch(console.error);
  }, [tld, sld, language]);

  return data;
}

export function Homepage({ tld, sld, language }: { tld: string; sld: string; language: string }) {
  const data = useHomepageData(tld, sld, language);

  if (!data) return <LoadingSpinner />;

  const showBookHero =
    data.experiment.homepageHeroVariant === 'featured-book-hero' &&
    data.featuredBook != null;

  return (
    <main>
      {showBookHero ? (
        <BookHero book={data.featuredBook!} />
      ) : (
        <AuthorHero author={data} />
      )}
      {/* Rest of page — books list, articles, etc. */}
    </main>
  );
}

function BookHero({ book }: { book: FeaturedBookDto }) {
  return (
    <section aria-label="Featured book">
      <img src={book.coverImageUrl} alt={book.coverImageAlt} />
      <h1>{book.title}</h1>
      {book.subtitle && <p className="subtitle">{book.subtitle}</p>}
      <p className="byline">By {book.authorName}</p>
      <p>{book.description}</p>

      <a href={book.primaryCtaUrl} className="btn-primary">
        {book.primaryCtaLabel}
      </a>

      {book.secondaryCtaLabel && book.secondaryCtaUrl && (
        <a href={book.secondaryCtaUrl} className="btn-secondary">
          {book.secondaryCtaLabel}
        </a>
      )}

      {book.formats.length > 0 && (
        <ul aria-label="Available formats">
          {book.formats.map(fmt => <li key={fmt}>{fmt}</li>)}
        </ul>
      )}
    </section>
  );
}
```

---

## Analytics — tracking experiment exposure

Track an exposure event as soon as the variant is rendered so experiment data is accurate:

```typescript
function trackExperimentExposure(variant: string, sessionId: string): void {
  // Google Analytics 4 example
  gtag('event', 'experiment_exposure', {
    experiment_name: 'homepage-hero',
    variant,
    session_id: sessionId,
  });

  // Alternatively with a custom analytics client:
  analytics.track('Experiment Exposure', {
    experiment: 'homepage-hero',
    variant,
    sessionId,
    timestamp: new Date().toISOString(),
  });
}

// Call immediately after deciding which layout to render:
trackExperimentExposure(data.experiment.homepageHeroVariant, getOrCreateSessionId());
```

Track conversions (e.g. clicks on the primary CTA):

```typescript
function trackBookHeroCta(book: FeaturedBookDto, ctaType: 'primary' | 'secondary'): void {
  analytics.track('Book Hero CTA Click', {
    experiment: 'homepage-hero',
    variant: 'featured-book-hero',
    bookTitle: book.title,
    ctaType,
    sessionId: getOrCreateSessionId(),
  });
}
```

---

## Variant rules summary

| `X-Session-Id` header | `featuredBook` in response | `homepageHeroVariant` |
|---|---|---|
| Missing or empty | — | `"control"` (experiment not entered) |
| Present | `null` (no featured book configured) | `"control"` |
| Present | populated | `"control"` or `"featured-book-hero"` per bucket |

---

## Backward compatibility

- `featuredBook` and `experiment` are **new optional fields**. Existing consumers that ignore unknown JSON properties are unaffected.
- The `books` array is always populated as before — `featuredBook` is an enriched view of one of those books, not a replacement.
- Removing `X-Session-Id` from requests (or not implementing it yet) means all visitors receive `"control"`, which is identical to the pre-experiment behavior.

---

## Seeding a featured book (backend / CMS)

A content editor or backend developer must set `IsFeaturedHeroBook = true` on exactly one `Book` document in Cosmos DB for the experiment to activate. The optional hero fields on the book document map directly to `FeaturedBookDto`:

| Cosmos DB `Book` field | `FeaturedBookDto` field | Notes |
|---|---|---|
| `Title` | `title` | Required |
| `Subtitle` | `subtitle` | Optional |
| `Description` | `description` | Required |
| `Cover` | `coverImageUrl` | Required |
| `CoverImageAlt` | `coverImageAlt` | Falls back to `Title` |
| `PrimaryCtaLabel` | `primaryCtaLabel` | Required; e.g. `"Buy Now"` |
| `URL` | `primaryCtaUrl` | Required |
| `SecondaryCtaLabel` | `secondaryCtaLabel` | Optional |
| `SecondaryCtaUrl` | `secondaryCtaUrl` | Optional |
| `Formats` | `formats` | Optional; e.g. `["Hardcover","eBook"]` |

> If more than one book is flagged, the API deterministically selects the one with the lexicographically lowest `id`. Flag only one book per author to avoid ambiguity.

---

## Frequently asked questions

**Q: What happens if the experiment Cosmos container is down?**  
A: The API catches all experiment-service errors and returns `"control"`. The homepage continues to work normally.

**Q: Can I test the book hero locally without being bucketed into the variant?**  
A: Yes — temporarily mock the API response with `homepageHeroVariant: "featured-book-hero"` and a populated `featuredBook` object, or ask a backend developer to set `TrafficPercentage = 100` on the treatment variant in the local `SeedExperiments` data.

**Q: Is `experiment` ever `null`?**  
A: Not in responses from the `GetAuthorData` function. The field is always set before the response is returned. It is only `null` inside the service layer before the function sets it, which is invisible to the frontend.

**Q: How long does bucketing last?**  
A: As long as the `opa_session_id` value in `localStorage` exists. Clearing browser storage will re-bucket the visitor on the next request.
