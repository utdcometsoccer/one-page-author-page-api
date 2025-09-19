# InkStainedWretchStripe

Azure Functions (Isolated Worker, .NET 9) exposing Stripe-backed endpoints for checkout, customers, subscriptions, price info, and webhooks.

## Overview
This project provides HTTP-triggered Azure Functions to integrate with Stripe using services defined in `OnePageAuthorLib`.

Key functions and routes:
- POST /api/CreateStripeCheckoutSession — create a checkout session
- GET /api/GetStripeCheckoutSession/{sessionId} — retrieve a checkout session
- POST /api/CreateStripeCustomer — create a Stripe customer
- POST /api/CreateSubscription — create a subscription
- POST /api/CancelSubscription — cancel a subscription
- POST /api/UpdateSubscription — update a subscription
- GET  /api/ListSubscription/{customerId} — list subscriptions for a customer
- POST /api/WebHook — Stripe webhook receiver
- GET  /api/GetStripePriceInformation/{priceId} — retrieve price details

## Quickstart
```pwsh
dotnet build InkStainedWretchStripe.csproj
func start
```

## Configuration
These settings are read from environment variables or local.settings.json:
- STRIPE_API_KEY

Add library/service configuration via `OnePageAuthorLib` as needed.

Example local.settings.json (do not commit secrets):
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "STRIPE_API_KEY": "sk_test_***"
  }
}

## Deployment
- Deploy as an Azure Functions app (v4, dotnet-isolated). Configure STRIPE_API_KEY as an app setting.

## Notes
- Program.cs wires Application Insights, Function middleware, and registers Stripe-related services via `.AddStripeServices()`.
- Handlers live in this folder as C# files named by operation (e.g., CreateStripeCheckoutSession.cs).