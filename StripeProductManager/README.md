# Ink Stained Wretch - Stripe Product Manager

This console application creates or updates Stripe products for the Ink Stained Wretch author management platform.

## Products Created

### 1. Annual Subscription ($59/year)
- **Name**: "Ink Stained Wretch - Annual Subscription"
- **Price**: $59.00 USD
- **Billing**: Annually (every 1 year)
- **Nickname**: "1 year subscription"

### 2. 2-Year Subscription ($118/2 years)
- **Name**: "Ink Stained Wretch - 2-Year Subscription"  
- **Price**: $118.00 USD
- **Billing**: Every 2 years
- **Nickname**: "2 year subscription"

### 3. 3-Year Subscription ($149/3 years)
- **Name**: "Ink Stained Wretch - 3-Year Subscription"
- **Price**: $149.00 USD  
- **Billing**: Every 3 years
- **Nickname**: "3 year subscription"

## Features Included

All products include comprehensive features from the Ink Stained Wretch platform:

### Core Features
- Microsoft Entra ID Integration & SSO
- Multi-format image upload & Azure Blob Storage
- Custom domain registration & DNS management
- Stripe payment integration & recurring billing
- Penguin Random House & Amazon API integration

### Multi-Language Support
- English (EN), Spanish (ES), French (FR)
- Arabic (AR) with RTL support
- Chinese Simplified (ZH-CN) & Traditional (ZH-TW)

### Technical Infrastructure
- Azure Functions serverless architecture
- Azure Cosmos DB for data persistence
- Global CDN with Azure Front Door
- Enterprise security & compliance
- Real-time analytics & monitoring

## Setup Instructions

### 1. Configure Stripe API Key

**Option A: User Secrets (Recommended)**
```bash
cd StripeProductManager
dotnet user-secrets set "Stripe:SecretKey" "sk_test_your_actual_stripe_secret_key"
```

**Option B: Environment Variable**
```bash
set STRIPE__SECRETKEY=sk_test_your_actual_stripe_secret_key
```

**Option C: Update appsettings.json**
```json
{
  "Stripe": {
    "SecretKey": "sk_test_your_actual_stripe_secret_key"
  }
}
```

### 2. Build and Run

```bash
cd StripeProductManager
dotnet build
dotnet run
```

## What the Application Does

1. **Connects to Stripe** using your API key
2. **Checks for existing products** by name to avoid duplicates
3. **Creates or updates products** with comprehensive metadata
4. **Creates or updates prices** with correct billing intervals
5. **Adds extensive metadata** including:
   - Platform features (50+ features from your documentation)
   - Culture/language support information
   - Technical architecture details
   - Billing and subscription information

## Metadata Structure

### Product Metadata
- **Features**: All major platform capabilities
- **Culture Support**: Multi-language and localization data
- **Technical Info**: Cloud provider, architecture, version
- **Business Info**: Plan type, creation date, total features

### Price Metadata
- **Billing Info**: Cycle, renewal period, currency
- **Policies**: Cancellation, refund, auto-renewal
- **Timestamps**: Created and updated dates

## Security Notes

- Never commit actual Stripe API keys to source control
- Use user secrets or environment variables for production
- The placeholder key in appsettings.json should be replaced
- Test with Stripe test keys (sk_test_) before using live keys

## Error Handling

The application includes comprehensive error handling:
- Logs all operations for debugging
- Continues processing if individual products fail
- Provides detailed console output
- Graceful handling of Stripe API errors

## Output Example

```
🚀 Ink Stained Wretch - Stripe Product Manager
===============================================

🔄 Processing: Ink Stained Wretch - Annual Subscription
✅ Product created/updated: prod_abc123
✅ Price created/updated: price_def456 (Nickname: 1 year subscription)

🔄 Processing: Ink Stained Wretch - 2-Year Subscription  
✅ Product created/updated: prod_ghi789
✅ Price created/updated: price_jkl012 (Nickname: 2 year subscription)

🔄 Processing: Ink Stained Wretch - 3-Year Subscription
✅ Product created/updated: prod_mno345
✅ Price created/updated: price_pqr678 (Nickname: 3 year subscription)

✅ All products and prices have been successfully created/updated!
```

## Integration with Your Platform

Once created, these products can be used with your existing `SubscriptionPlanService`:

```csharp
// The service will automatically retrieve features from Stripe metadata
var subscriptionPlan = await subscriptionPlanService.MapToSubscriptionPlanAsync(priceDto);

// Features will be populated from the metadata
Console.WriteLine($"Plan: {subscriptionPlan.Label}");
Console.WriteLine($"Features: {string.Join(", ", subscriptionPlan.Features)}");
```

This ensures your subscription plans have rich feature information directly from Stripe, making them consistent across your entire platform.