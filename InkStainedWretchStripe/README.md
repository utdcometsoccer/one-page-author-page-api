# InkStainedWretchStripe

[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-orange.svg)](https://docs.microsoft.com/en-us/azure/azure-functions/)
[![Stripe](https://img.shields.io/badge/Stripe-API-blueviolet.svg)](https://stripe.com/docs)

Azure Functions app providing Stripe payment processing integration for subscription management, checkout sessions, and webhook handling.

## Overview

This project provides HTTP-triggered Azure Functions to integrate with Stripe using services defined in `OnePageAuthorLib`.

Key functions and routes:

- POST /api/CreateStripeCheckoutSession — create a checkout session
- GET  /api/GetStripeCheckoutSession/{sessionId} — retrieve a checkout session
- POST /api/CreateStripeCustomer — create a Stripe customer
- POST /api/CreateSubscription — create a subscription
- POST /api/CancelSubscription/{subscriptionId} — cancel a subscription
- POST /api/UpdateSubscription/{subscriptionId} — update a subscription
- GET  /api/ListSubscription/{customerId} — list subscriptions for a customer (supports query params)
- POST /api/InvoicePreview — preview upcoming invoice for a customer/subscription change
- POST /api/WebHook — Stripe webhook receiver (Stripe sends events here)
- POST /api/GetStripePriceInformation — retrieve filtered prices (body-driven)

## Quickstart

```pwsh
dotnet build InkStainedWretchStripe.csproj
func start

```

## Configuration

### User Secrets Setup (Recommended for Development)

**Important**: This project requires sensitive configuration values that should NOT be stored in source control.

#### Initialize User Secrets

```bash
cd InkStainedWretchStripe
dotnet user-secrets init

```

#### Add Required Secrets

Replace the placeholder values with your actual credentials:

```bash
# Stripe API Key (Get from Stripe Dashboard)
dotnet user-secrets set "STRIPE_API_KEY" "sk_test_YOUR_ACTUAL_STRIPE_KEY"

# Stripe Webhook Secret (Get from Stripe Dashboard)
dotnet user-secrets set "STRIPE_WEBHOOK_SECRET" "whsec_YOUR_ACTUAL_WEBHOOK_SECRET"

# Cosmos DB Primary Key (Get from Azure Portal)
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "YOUR_ACTUAL_COSMOS_KEY"

# Azure AD Tenant ID (Get from Azure Portal)
dotnet user-secrets set "AAD_TENANT_ID" "YOUR_ACTUAL_TENANT_ID"

# Azure AD Client ID (Get from Azure Portal)
dotnet user-secrets set "AAD_CLIENT_ID" "YOUR_ACTUAL_CLIENT_ID"

# Azure AD Audience (Usually same as Client ID)
dotnet user-secrets set "AAD_AUDIENCE" "YOUR_ACTUAL_CLIENT_ID"

```

#### Verify Setup

```bash
dotnet user-secrets list

```

You should see your secrets listed (values will be hidden for security).

### Environment Variables

The following environment variables are required for the application to run:

| Variable | Description | Required | Where to Find |
|----------|-------------|----------|---------------|
| `STRIPE_API_KEY` | Stripe API secret key | Yes | Stripe Dashboard → Developers → API keys |
| `STRIPE_WEBHOOK_SECRET` | Stripe webhook endpoint secret | Yes (webhooks) | Stripe Dashboard → Developers → Webhooks → [Your endpoint] → Signing secret |
| `COSMOSDB_ENDPOINT_URI` | Cosmos DB account endpoint | Yes | Azure Portal → Cosmos DB account → Keys → URI |
| `COSMOSDB_PRIMARY_KEY` | Cosmos DB primary access key | Yes | Azure Portal → Cosmos DB account → Keys → Primary Key |
| `COSMOSDB_DATABASE_ID` | Cosmos DB database name | Yes | Your database name (e.g., "OnePageAuthor") |
| `AAD_TENANT_ID` | Azure Active Directory tenant ID | Yes | Azure Portal → Azure Active Directory → Properties → Directory ID |
| `AAD_CLIENT_ID` | Azure AD application client ID | Yes | Azure Portal → Azure Active Directory → App registrations → [Your app] → Application ID |
| `AAD_AUDIENCE` | Azure AD API audience/scope | Yes | Usually same as Client ID |

### Configuration Sources Priority

The application reads configuration in this order (later sources override earlier ones):

1. **appsettings.json** (default values)
2. **Environment Variables** (production)
3. **User Secrets** (development) 
4. **local.settings.json** (⚠️ **NOT RECOMMENDED** - contains exposed credentials)

### Security Requirements

**⚠️ IMPORTANT SECURITY NOTICE:**
- **DO NOT** use `local.settings.json` for storing credentials
- **DO NOT** commit secrets to version control
- **ALWAYS** use User Secrets for local development
- **ALWAYS** use Environment Variables or Azure Key Vault for production

**Step 1:** Set up User Secrets (one-time setup)

```bash
cd InkStainedWretchStripe
dotnet user-secrets init
```

**Step 2:** Add your secrets (replace with actual values)

```bash
# Stripe Configuration
dotnet user-secrets set "STRIPE_API_KEY" "sk_test_YOUR_ACTUAL_STRIPE_KEY"
dotnet user-secrets set "STRIPE_WEBHOOK_SECRET" "whsec_YOUR_ACTUAL_WEBHOOK_SECRET"

# Cosmos DB Configuration  
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "YOUR_ACTUAL_COSMOS_KEY"
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthor"

# Azure AD Configuration
dotnet user-secrets set "AAD_TENANT_ID" "YOUR_ACTUAL_TENANT_ID"
dotnet user-secrets set "AAD_CLIENT_ID" "YOUR_ACTUAL_CLIENT_ID"
dotnet user-secrets set "AAD_AUDIENCE" "YOUR_ACTUAL_CLIENT_ID"
```

**Step 3:** Verify Setup

```bash
dotnet user-secrets list
```

## Deployment

### Production Deployment

For Azure deployments:

1. **Deploy as Azure Functions v4** (.NET 10.0, isolated worker)
2. **Configure Application Settings** in Azure Portal (Function App → Configuration):
   - `STRIPE_API_KEY` (use production `sk_live_` key)
   - `STRIPE_WEBHOOK_SECRET`
   - `COSMOSDB_ENDPOINT_URI`
   - `COSMOSDB_PRIMARY_KEY`
   - `COSMOSDB_DATABASE_ID`
   - `AAD_TENANT_ID`
   - `AAD_CLIENT_ID`
   - `AAD_AUDIENCE`

3. **Enhanced Security**: Use Azure Key Vault integration


   ```bash

   az keyvault secret set --vault-name <vault-name> --name STRIPE-API-KEY --value <key>

   ```

### Security Best Practices

- ✅ **Never commit secrets to source control**
- ✅ **Use user secrets for local development**
- ✅ **Use environment variables or Azure App Settings for production**
- ✅ **Use Azure Key Vault for sensitive production data**
- ✅ **Rotate keys regularly**
- ✅ **Use test keys (`sk_test_`) for development**
- ✅ **Use live keys (`sk_live_`) only in production**
- ❌ **Don't share secrets via email or chat**
- ❌ **Don't hardcode secrets in your application**

### Webhook Configuration

For production webhook handling:

1. In Stripe Dashboard, add webhook endpoint:

   - URL: `https://<your-function-app>.azurewebsites.net/api/WebHook`
   - Events to send: Select the events you want to handle (e.g., `invoice.payment_succeeded`)

2. Copy the webhook signing secret
3. Add as app setting: `STRIPE_WEBHOOK_SECRET=whsec_***`

## Troubleshooting

### "Configuration value not found" errors


- Make sure you've run `dotnet user-secrets init`
- Verify secrets are set with `dotnet user-secrets list`
- Check that you're in the correct project directory

### Authentication errors


- Verify your Azure AD configuration matches your app registration
- Check that the tenant ID and client ID are correct
- Ensure your app registration has the necessary permissions

### Stripe errors


- Verify you're using the correct API key for your environment (test vs live)
- Check that your Stripe account is active and in good standing
- Ensure webhook secret matches the endpoint configuration

### Webhook signature validation failures


- Verify `STRIPE_WEBHOOK_SECRET` is set correctly
- Check system clock synchronization (5-minute tolerance window)
- Ensure the webhook endpoint URL matches Stripe configuration
- Test with Stripe CLI: `stripe listen --forward-to localhost:7292/api/WebHook`

## Notes


- Program.cs wires Application Insights, Function middleware, and registers Stripe-related services via `.AddStripeServices()`.
- Handlers live in this folder as C# files named by operation (e.g., CreateStripeCheckoutSession.cs).

## Examples

Assuming the Functions host is running locally at <https://localhost:7292.>

Create customer

```pwsh
$body = @{ Email = "user@example.com"; Name = "Jane Doe" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "https://localhost:7292/api/CreateStripeCustomer" -ContentType "application/json" -Body $body

```

Create subscription

```pwsh
$body = @{ PriceId = "price_123"; CustomerId = "cus_123" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "https://localhost:7292/api/CreateSubscription" -ContentType "application/json" -Body $body

```

Cancel subscription (optional body supports InvoiceNow/Prorate)

```pwsh
$body = @{ InvoiceNow = $true; Prorate = $false } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "https://localhost:7292/api/CancelSubscription/sub_123" -ContentType "application/json" -Body $body

```

List subscriptions for a customer with filters

```pwsh
Invoke-RestMethod -Method Get -Uri "https://localhost:7292/api/ListSubscription/cus_123?status=active&limit=10"

```

Create checkout session

```pwsh
$body = @{ SuccessUrl = "https://example.com/success"; CancelUrl = "https://example.com/cancel"; PriceId = "price_123" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "https://localhost:7292/api/CreateStripeCheckoutSession" -ContentType "application/json" -Body $body

```

Get price information (filtered list)

```pwsh
$body = @{ Active = $true; Limit = 20; Currency = "usd"; IncludeProductDetails = $true } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "https://localhost:7292/api/GetStripePriceInformation" -ContentType "application/json" -Body $body

```

Update subscription (change item price and qty with proration)

```pwsh
$body = @{ SubscriptionItemId = "si_123"; PriceId = "price_456"; Quantity = 2; ProrationBehavior = "create_prorations" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "https://localhost:7292/api/UpdateSubscription/sub_123" -ContentType "application/json" -Body $body

```

Invoice preview (upcoming invoice) for a change

```pwsh
$body = @{ CustomerId = "cus_123"; SubscriptionId = "sub_123"; PriceId = "price_456"; Quantity = 2; ProrationBehavior = "create_prorations" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "https://localhost:7292/api/InvoicePreview" -ContentType "application/json" -Body $body

```

Webhook receiver (manual test)

```pwsh
$payload = '{"type":"invoice.payment_succeeded"}'
Invoke-RestMethod -Method Post -Uri "https://localhost:7292/api/WebHook" -ContentType "application/json" -Body $payload -Headers @{ "Stripe-Signature" = "t=TIMESTAMP,v1=SIGNATURE" }

Notes:
- Webhook signature is validated using HMAC-SHA256 with STRIPE_WEBHOOK_SECRET.
- A 5-minute tolerance window is enforced on the timestamp embedded in the Stripe-Signature header.
- For invoice events, the handler also extracts the first line item's price.id when present and includes it in the response payload.

InvoicePreview response lines include `PriceId` when Stripe returns `lines.data[*].price.id` in the REST response.

```
