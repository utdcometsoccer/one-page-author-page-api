# InkStainedWretchStripe

[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
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

### Configuration Values

These settings are read from user secrets, environment variables, or local.settings.json:

| Setting | Description | Required |
|---------|-------------|----------|
| `STRIPE_API_KEY` | Stripe API secret key | Yes |
| `STRIPE_WEBHOOK_SECRET` | Stripe webhook signing secret | Yes (for webhooks) |
| `COSMOSDB_PRIMARY_KEY` | Cosmos DB access key | Yes |
| `AAD_TENANT_ID` | Azure AD tenant ID | Yes |
| `AAD_CLIENT_ID` | Azure AD application client ID | Yes |
| `AAD_AUDIENCE` | Azure AD API audience | Yes |

### Where to Find Your Values

**Stripe API Key:**
1. Go to [Stripe Dashboard](https://dashboard.stripe.com/)
2. Navigate to Developers > API Keys
3. Copy the "Secret key" (starts with `sk_test_` for test mode)

**Stripe Webhook Secret:**
1. Go to [Stripe Dashboard](https://dashboard.stripe.com/)
2. Navigate to Developers > Webhooks
3. Create or select a webhook endpoint
4. Copy the "Signing secret" (starts with `whsec_`)

**Cosmos DB Primary Key:**
1. Go to [Azure Portal](https://portal.azure.com/)
2. Navigate to your Cosmos DB account
3. Go to Settings > Keys
4. Copy the "Primary Key"

**Azure AD Values:**
1. Go to [Azure Portal](https://portal.azure.com/)
2. Navigate to Azure Active Directory > App registrations
3. Select your app registration
4. Copy the "Application (client) ID" and "Directory (tenant) ID"

### Example local.settings.json (Alternative Configuration Method)

⚠️ **WARNING**: Do not commit secrets to source control!

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "STRIPE_API_KEY": "sk_test_***",
    "STRIPE_WEBHOOK_SECRET": "whsec_***",
    "COSMOSDB_PRIMARY_KEY": "***",
    "AAD_TENANT_ID": "***",
    "AAD_CLIENT_ID": "***",
    "AAD_AUDIENCE": "***"
  }
}
```

Add library/service configuration via `OnePageAuthorLib` as needed.

## Deployment

### Production Deployment

For production deployments:

1. Deploy as an Azure Functions app (v4, dotnet-isolated)
2. Configure settings as Azure App Settings (not in local.settings.json):
   - `STRIPE_API_KEY`
   - `STRIPE_WEBHOOK_SECRET`
   - `COSMOSDB_PRIMARY_KEY`
   - `AAD_TENANT_ID`
   - `AAD_CLIENT_ID`
   - `AAD_AUDIENCE`

3. Optionally use Azure Key Vault for enhanced security:
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

Assuming the Functions host is running locally at https://localhost:7292.

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
