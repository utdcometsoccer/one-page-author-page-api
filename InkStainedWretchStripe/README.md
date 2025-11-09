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
These settings are read from environment variables or local.settings.json:
- STRIPE_API_KEY
- STRIPE_WEBHOOK_SECRET (required for /api/WebHook signature verification)

Add library/service configuration via `OnePageAuthorLib` as needed.

Example local.settings.json (do not commit secrets):
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "STRIPE_API_KEY": "sk_test_***",
    "STRIPE_WEBHOOK_SECRET": "whsec_***"
  }
}

## Deployment
- Deploy as an Azure Functions app (v4, dotnet-isolated). Configure STRIPE_API_KEY as an app setting.

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
