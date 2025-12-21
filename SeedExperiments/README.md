# A/B Testing Experiments Seeder

This console application seeds sample A/B test experiments into Cosmos DB for testing the experiments API.

## Prerequisites

- .NET 10.0 SDK
- Azure Cosmos DB account
- Environment variables configured

## Environment Variables

Set the following environment variables before running:

- `COSMOSDB_ENDPOINT_URI` - Your Cosmos DB endpoint URI
- `COSMOSDB_PRIMARY_KEY` - Your Cosmos DB primary key
- `COSMOSDB_DATABASE_ID` - Database name (defaults to "OnePageAuthor")

## Usage

```bash
# Set environment variables
export COSMOSDB_ENDPOINT_URI="https://your-account.documents.azure.com:443/"
export COSMOSDB_PRIMARY_KEY="your-primary-key-here"
export COSMOSDB_DATABASE_ID="OnePageAuthor"

# Run the seeder
cd SeedExperiments
dotnet run
```

## Sample Experiments

The seeder creates the following sample experiments:

### Landing Page Experiments

1. **Hero Button Color Test**
   - Control: Blue button (#007bff)
   - Variant A: Green button (#28a745)
   - Traffic: 50/50 split

2. **Hero Headline Test**
   - Control: "Create Your Author Page"
   - Variant A: "Build Your Author Brand"
   - Variant B: "Start Your Author Journey"
   - Traffic: 33/33/34 split

### Pricing Page Experiments

1. **Pricing Card Design Test**
   - Control: Traditional card design
   - Variant A: Modern card with "Most Popular" badge
   - Traffic: 50/50 split

2. **Pricing CTA Button Text Test**
   - Control: "Subscribe Now"
   - Variant A: "Start Free Trial"
   - Variant B: "Get Started Today"
   - Traffic: 40/30/30 split

## Testing the API

After seeding, test the API endpoints:

```bash
# Get experiments for landing page
curl "http://localhost:7071/api/experiments?page=landing"

# Get experiments for pricing page with user ID
curl "http://localhost:7071/api/experiments?page=pricing&userId=test-user-123"

# Same user ID will always get the same variants (consistent bucketing)
curl "http://localhost:7071/api/experiments?page=landing&userId=test-user-123"
curl "http://localhost:7071/api/experiments?page=landing&userId=test-user-123"
```

## Notes

- The seeder is idempotent - running it multiple times will attempt to create the same experiments
- If experiments already exist, you'll see error messages (this is expected)
- Each user/session is consistently assigned to the same variants using deterministic hashing
- Traffic percentages should add up to 100 for proper distribution
