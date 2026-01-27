# InkStainedWretchStripe

[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-orange.svg)](https://docs.microsoft.com/en-us/azure/azure-functions/)
[![Stripe](https://img.shields.io/badge/Stripe-API-blueviolet.svg)](https://stripe.com/docs)

Azure Functions app providing Stripe payment processing integration for subscription management, checkout sessions, and webhook handling.

## Overview

This project provides HTTP-triggered Azure Functions to integrate with Stripe using services defined in `OnePageAuthorLib`.

Key functions and routes:

- GET  /api/stripe/health — health check endpoint (returns Stripe mode and connection status)
- POST /api/CreateStripeCheckoutSession — create a checkout session
- GET  /api/GetStripeCheckoutSession/{sessionId} — retrieve a checkout session
- POST /api/CreateStripeCustomer — create a Stripe customer
- POST /api/CreateSubscription — create a subscription
- POST /api/CancelSubscription/{subscriptionId} — cancel a subscription
- POST /api/UpdateSubscription/{subscriptionId} — update a subscription
- GET  /api/ListSubscription/{customerId} — list subscriptions for a customer (supports query params)
- GET  /api/FindSubscription — find subscriptions by customer email and domain (requires email and domain query params)
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

# Optional: Entra ID / CIAM Authority URL (auto-constructed for standard tenants if not provided)
# dotnet user-secrets set "AAD_AUTHORITY" "https://{your-ciam-domain}.ciamlogin.com/{your-tenant}/"

# Optional: Multiple valid issuers (comma-separated)
# dotnet user-secrets set "AAD_VALID_ISSUERS" "https://inkswcustomers.ciamlogin.com/inkswcustomers.onmicrosoft.com/v2.0,https://inkswcustomers.ciamlogin.com/inkswcustomers.onmicrosoft.com/B2C_1_signup_signin/v2.0"

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
| `AAD_TENANT_ID` | Entra ID / CIAM tenant ID | Yes | Azure Portal → Microsoft Entra ID → Overview → Tenant ID |
| `AAD_CLIENT_ID` | Entra ID / CIAM application client ID | Yes | Azure Portal → Microsoft Entra ID → App registrations → [Your app] → Application (client) ID |
| `AAD_AUDIENCE` | Entra ID / CIAM API audience/scope | Yes | Usually same as Client ID |
| `AAD_AUTHORITY` | Entra ID / CIAM authority URL | No (optional) | For CIAM, set explicitly (e.g., `https://{your-ciam-domain}.ciamlogin.com/{your-tenant}/`); for standard Entra ID tenants, it is auto-constructed from tenant ID if not provided (e.g., `https://login.microsoftonline.com/{tenantId}/v2.0`) |
| `AAD_VALID_ISSUERS` | Comma-separated list of valid JWT issuers | No (optional) | Include your CIAM policy issuers such as `https://{your-ciam-domain}.ciamlogin.com/{your-tenant}/B2C_1_signup_signin/v2.0/` and any additional policies or tenants you need |

### Why These Settings Are Needed

<details>
<summary>💳 Stripe Configuration Details</summary>

**`STRIPE_API_KEY`**

- **Purpose**: Authenticates all API calls to Stripe's servers for payment processing
- **Why It's Needed**: Required for creating customers, managing subscriptions, processing payments, and retrieving price/product information
- **Key Types**:
  - Test keys (`sk_test_...`): Use for development and testing - no real charges
  - Live keys (`sk_live_...`): Use for production - processes real payments
- **How to Obtain**:
  1. Log in to [Stripe Dashboard](https://dashboard.stripe.com)
  2. Navigate to **Developers** → **API keys**
  3. Copy the **Secret key** (click "Reveal test key" for test mode)

**`STRIPE_WEBHOOK_SECRET`**

- **Purpose**: Validates that incoming webhook events genuinely originate from Stripe
- **Why It's Needed**: Prevents malicious actors from spoofing payment events and fraudulently triggering business logic (e.g., activating subscriptions without payment)
- **Security**: Uses HMAC-SHA256 signature verification with a 5-minute timestamp tolerance
- **How to Obtain**:
  1. In Stripe Dashboard, go to **Developers** → **Webhooks**
  2. Click **Add endpoint** and enter your webhook URL (e.g., `https://your-app.azurewebsites.net/api/WebHook`)
  3. Select events to receive (e.g., `invoice.payment_succeeded`, `customer.subscription.updated`)
  4. After creation, click on the endpoint and copy the **Signing secret** (starts with `whsec_`)

</details>

<details>
<summary>🗄️ Cosmos DB Configuration Details</summary>

**`COSMOSDB_ENDPOINT_URI`**

- **Purpose**: Specifies the URL of your Cosmos DB account
- **Why It's Needed**: Required to establish the database connection for storing user profiles, subscription data, and customer information
- **Format**: `https://your-account-name.documents.azure.com:443/`
- **How to Obtain**:
  1. Go to [Azure Portal](https://portal.azure.com)
  2. Navigate to your Cosmos DB account
  3. Click **Keys** in the left menu
  4. Copy the **URI** value

**`COSMOSDB_PRIMARY_KEY`**

- **Purpose**: Authentication key for Cosmos DB access
- **Why It's Needed**: Grants read/write permissions to the database. Without this, the application cannot store or retrieve data.
- **Security**: This is a sensitive credential - never commit to source control
- **How to Obtain**: In the same **Keys** section, copy the **Primary Key**

**`COSMOSDB_DATABASE_ID`**

- **Purpose**: Identifies the specific database within your Cosmos DB account
- **Why It's Needed**: An account can contain multiple databases; this tells the app which one holds the application data
- **Value**: Typically "OnePageAuthor" or "OnePageAuthorDb"

</details>

<details>
<summary>🔐 Azure AD (Entra ID) Configuration Details</summary>

**`AAD_TENANT_ID`**

- **Purpose**: Identifies your Azure AD tenant (organization)
- **Why It's Needed**: Used during JWT token validation to ensure tokens were issued by your tenant. Tokens from other tenants will be rejected.
- **Format**: GUID (e.g., `12345678-1234-1234-1234-123456789abc`)
- **How to Obtain**:
  1. Go to [Azure Portal](https://portal.azure.com)
  2. Navigate to **Microsoft Entra ID** (formerly Azure Active Directory)
  3. Copy the **Tenant ID** from the Overview page

**`AAD_CLIENT_ID` / `AAD_AUDIENCE`**

- **Purpose**: Identifies your API application and validates token audience claims
- **Why It's Needed**: Ensures tokens were specifically issued for your API, not another application. Provides an additional layer of security.
- **Note**: These are typically the same value for API applications
- **How to Obtain**:
  1. In Microsoft Entra ID, go to **App registrations**
  2. Select your API application
  3. Copy the **Application (client) ID**

</details>

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

# Optional: Azure AD Authority URL (auto-constructed if not provided)
# dotnet user-secrets set "AAD_AUTHORITY" "https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0"

# Optional: Multiple valid issuers (comma-separated)
# dotnet user-secrets set "AAD_VALID_ISSUERS" "https://login.microsoftonline.com/TENANT1/v2.0,https://login.microsoftonline.com/TENANT2/v2.0"
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
   - `AAD_AUTHORITY` (optional - auto-constructed from tenant ID if not provided)
   - `AAD_VALID_ISSUERS` (optional - for multi-tenant scenarios)

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

### Health Check

Check Stripe configuration and connection status (no authentication required):

```pwsh
Invoke-RestMethod -Method Get -Uri "https://localhost:7292/api/stripe/health"

# Response example (test mode):
# {
#   "stripeMode": "test",
#   "stripeConnected": true,
#   "version": "1.0.0"
# }
```

This endpoint is useful for:

- Verifying Stripe configuration before making API calls
- Frontend validation to detect test/live mode mismatches
- Health monitoring and diagnostics
- No authentication required (anonymous access)

### Create customer

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

Find subscriptions by customer email and domain

```pwsh
Invoke-RestMethod -Method Get -Uri "https://localhost:7292/api/FindSubscription?email=user@example.com&domain=example.com"

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
