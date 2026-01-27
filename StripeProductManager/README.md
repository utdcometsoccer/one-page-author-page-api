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

## ⚙️ Configuration

### Required Settings

| Variable | Description | Where to Find | Why It's Needed |
|----------|-------------|---------------|-----------------|
| `Stripe:SecretKey` | Stripe API secret key | [Stripe Dashboard](https://dashboard.stripe.com) → Developers → API keys → Secret key | Authenticate API calls to create products and prices |

### Why This Setting Is Needed

**`Stripe:SecretKey`**

- **Purpose**: Authenticates all Stripe API operations including creating products, prices, and updating metadata
- **Key Types**:
  - Test keys (`sk_test_...`): Use for development and testing - creates products in test mode
  - Live keys (`sk_live_...`): Use for production - creates real products visible to customers
- **Security**: Never commit this key to source control or share it publicly

### How to Obtain Your Stripe API Key

1. Sign up or log in at [Stripe Dashboard](https://dashboard.stripe.com)
2. Navigate to **Developers** → **API keys**
3. For development: Copy the **Secret key** in test mode (toggle "Test mode" in the header)
4. For production: Copy the **Secret key** in live mode
5. **Important**: The secret key is shown only once for live mode - save it securely

### Setup Instructions

#### Option A: User Secrets (Recommended for Development)

```bash
cd StripeProductManager
dotnet user-secrets init
dotnet user-secrets set "Stripe:SecretKey" "sk_test_your_actual_stripe_secret_key"

# Verify
dotnet user-secrets list
```

#### Option B: Environment Variable

```bash
# Windows (PowerShell)
$env:STRIPE__SECRETKEY = "sk_test_your_actual_stripe_secret_key"

# Windows (Command Prompt)
set STRIPE__SECRETKEY=sk_test_your_actual_stripe_secret_key

# Linux/Mac
export STRIPE__SECRETKEY=sk_test_your_actual_stripe_secret_key

```

#### Option C: Update appsettings.json

⚠️ **Warning**: Only use for non-sensitive test keys. Never commit production keys.

```json
{
  "Stripe": {
    "SecretKey": "sk_test_your_actual_stripe_secret_key"
  }
}

```

#### Option D: Environment-Specific Configuration

Create environment-specific configuration files:

```bash
# For development
cp appsettings.json appsettings.Development.json

# For production
cp appsettings.json appsettings.Production.json

```

Then customize pricing for each environment. The application automatically loads the appropriate file based on `ASPNETCORE_ENVIRONMENT`.

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
   📍 Cultures: en-US (primary), es-US, fr-CA, en-CA, es-MX, fr-FR
   🌐 Localized versions: 3 languages
✅ Product created/updated: prod_abc123
✅ Price created/updated: price_def456 (Nickname: 1 year subscription)

🔄 Processing: Ink Stained Wretch - 2-Year Subscription
   📍 Cultures: en-US (primary), es-US, fr-CA, en-CA, es-MX, fr-FR
   🌐 Localized versions: 3 languages
✅ Product created/updated: prod_ghi789
✅ Price created/updated: price_jkl012 (Nickname: 2 year subscription)

🔄 Processing: Ink Stained Wretch - 3-Year Subscription
   📍 Cultures: en-US (primary), es-US, fr-CA, en-CA, es-MX, fr-FR
   🌐 Localized versions: 3 languages
✅ Product created/updated: prod_mno345
✅ Price created/updated: price_pqr678 (Nickname: 3 year subscription)

✅ All products and prices have been successfully created/updated!
✅ Culture information added to all products!

```

## Culture and Internationalization Support

### Supported Languages

The application includes comprehensive culture information for internationalization:

#### English Variants

- **en-US**: English (United States) - Primary
- **en-CA**: English (Canada)

#### Spanish Variants

- **es-US**: Spanish (United States)
- **es-MX**: Spanish (Mexico)

#### French Variants

- **fr-CA**: French (Canada)
- **fr-FR**: French (France)

### Culture-Specific Features

Each product includes localized names, descriptions, and pricing nicknames:

**Example - Annual Subscription:**

- **English (en-US)**: "Ink Stained Wretch - Annual Subscription" / "1 year subscription"
- **Spanish (es-US)**: "Escritor Manchado de Tinta - Suscripción Anual" / "suscripción de 1 año"
- **French (fr-CA)**: "Écrivain Taché d'Encre - Abonnement Annuel" / "abonnement de 1 an"

### Enhanced Stripe Metadata

Products include rich culture metadata:

- `supported_cultures`: Comma-separated list of supported culture codes
- `primary_language`: Primary culture code
- `multi_language`: Boolean indicating multi-language support
- `culture_count`: Number of supported cultures
- `localized_versions`: Number of cultures with localized content
- `culture_XX_code`: Individual culture codes
- `culture_XX_name`: Culture display names
- `culture_XX_localized_name`: Product name in that culture
- `culture_XX_localized_nickname`: Price nickname in that culture

### Configuration Example

```json
{
  "Name": "Ink Stained Wretch - Annual Subscription",
  "SupportedCultures": ["en-US", "en-CA", "es-US", "es-MX", "fr-CA", "fr-FR"],
  "PrimaryCulture": "en-US",
  "CultureSpecificInfo": {
    "en-US": {
      "LocalizedName": "Ink Stained Wretch - Annual Subscription",
      "LocalizedDescription": "Complete author management platform...",
      "LocalizedNickname": "1 year subscription"
    },
    "es-US": {
      "LocalizedName": "Escritor Manchado de Tinta - Suscripción Anual",
      "LocalizedDescription": "Plataforma completa de gestión de autores...",
      "LocalizedNickname": "suscripción de 1 año"
    },
    "fr-CA": {
      "LocalizedName": "Écrivain Taché d'Encre - Abonnement Annuel",
      "LocalizedDescription": "Plateforme complète de gestion d'auteurs...",
      "LocalizedNickname": "abonnement de 1 an"
    }
  }
}

```

### Managing Cultures

**Adding New Cultures:**

1. Add culture code to `SupportedCultures` array
2. Add entry to `CultureSpecificInfo` with localized content
3. Run the application to update Stripe products

**Changing Primary Culture:**

1. Update `PrimaryCulture` field in configuration
2. Ensure the primary culture exists in `SupportedCultures`
3. Re-run the application to update metadata

**Removing Cultures:**

1. Remove culture code from `SupportedCultures`
2. Remove corresponding entry from `CultureSpecificInfo`
3. Re-run to clean up Stripe metadata

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

## Customizing Products

### Product & Pricing Configuration

The application reads product definitions and pricing from `appsettings.json`. You can customize products, descriptions, and prices by editing the configuration:

```json
{
  "Stripe": {
    "Products": [
      {
        "Name": "Custom Plan - Monthly",
        "Description": "Monthly subscription with all features",
        "PriceInCents": 999,
        "PriceNickname": "monthly plan",
        "IntervalCount": 1,
        "PlanType": "monthly"
      },
      {
        "Name": "Custom Plan - Annual",
        "Description": "Annual subscription with discount",
        "PriceInCents": 9900,
        "PriceNickname": "annual plan",
        "IntervalCount": 1,
        "PlanType": "annual"
      }
    ]
  }
}

```

### Customization Options

To change pricing, cultures, or add new products:

1. **Modify existing products**: Edit the `PriceInCents` value (e.g., 5900 = $59.00)
2. **Add new products**: Add new entries to the `Products` array
3. **Change billing cycles**: Modify `IntervalCount` (1 = yearly, 2 = every 2 years, etc.)
4. **Update descriptions**: Edit the `Description` field for each product
5. **Add cultures**: Modify `SupportedCultures` array and add entries to `CultureSpecificInfo`
6. **Change primary language**: Update `PrimaryCulture` field

## Verifying in Stripe Dashboard

1. Log into your [Stripe Dashboard](https://dashboard.stripe.com/)
2. Go to **Products** → **Products**
3. You should see your "Ink Stained Wretch" products
4. Click on any product to see the rich metadata
5. Go to **Products** → **Prices** to see the pricing configurations

## Troubleshooting

### Error: "Stripe:SecretKey is required"

- Make sure you've set your Stripe API key using one of the methods above
- Verify the key starts with `sk_test_` for test mode or `sk_live_` for live mode

### Error: "Invalid API Key"

- Double-check your Stripe API key is correct
- Ensure you're using the correct key for your Stripe account
- Make sure the key hasn't expired or been revoked

### Error: Network/Connection Issues

- Check your internet connection
- Verify you can access <https://api.stripe.com>
- Check if you're behind a corporate firewall

### Warning: "Stripe.net version resolved to newer version"

- This is harmless - NuGet automatically resolved to a newer compatible version
- The application will work correctly with Stripe.net 47.0.0

## Next Steps

After running the application:

1. **Test Integration**: Verify your `SubscriptionPlanService` picks up the new features
2. **Update UI**: Your subscription selection UI now has rich feature data
3. **Customize Features**: Edit the metadata in Stripe Dashboard to add/remove features
4. **Go Live**: When ready, switch to live Stripe keys and run again
