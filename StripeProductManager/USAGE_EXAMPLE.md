# Example: Running the Stripe Product Manager

## Step 1: Set up your Stripe API Key

### Option 1: Using User Secrets (Recommended)

```bash
cd StripeProductManager
dotnet user-secrets set "Stripe:SecretKey" "sk_test_51AbCdE..."
```

### Option 2: Using Environment Variable

```bash
# Windows (PowerShell)
$env:STRIPE__SECRETKEY = "sk_test_51AbCdE..."

# Windows (Command Prompt) 
set STRIPE__SECRETKEY=sk_test_51AbCdE...

# Linux/Mac
export STRIPE__SECRETKEY=sk_test_51AbCdE...
```

### Option 3: Environment-Specific Configuration Files

Create environment-specific configuration files:

```bash
# For development
cp appsettings.json appsettings.Development.json

# For production  
cp appsettings.json appsettings.Production.json
```

Then customize pricing for each environment. The application automatically loads the appropriate file based on `ASPNETCORE_ENVIRONMENT`.

## Step 2: Configure Products (Optional)

The default configuration includes three subscription plans. To customize:

```bash
# Edit the appsettings.json file
notepad appsettings.json
```

Example configuration changes:

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

## Step 3: Run the Application

```bash
cd StripeProductManager
dotnet run
```

## Expected Output

```
🚀 Ink Stained Wretch - Stripe Product Manager
===============================================

🔄 Processing: Ink Stained Wretch - Annual Subscription
   📍 Cultures: en-US (primary), es-US, fr-CA, en-CA, es-MX, fr-FR
   🌐 Localized versions: 3 languages
✅ Product created/updated: prod_P1Q2R3S4T5U6V7W8
✅ Price created/updated: price_1A2B3C4D5E6F7G8H (Nickname: 1 year subscription)

🔄 Processing: Ink Stained Wretch - 2-Year Subscription
   📍 Cultures: en-US (primary), es-US, fr-CA, en-CA, es-MX, fr-FR
   🌐 Localized versions: 3 languages
✅ Product created/updated: prod_X1Y2Z3A4B5C6D7E8
✅ Price created/updated: price_2F9G8H7I6J5K4L3M (Nickname: 2 year subscription)

🔄 Processing: Ink Stained Wretch - 3-Year Subscription
   📍 Cultures: en-US (primary), es-US, fr-CA, en-CA, es-MX, fr-FR
   🌐 Localized versions: 3 languages
✅ Product created/updated: prod_9I8J7K6L5M4N3O2P
✅ Price created/updated: price_3Q1R2S3T4U5V6W7X (Nickname: 3 year subscription)

✅ All products and prices have been successfully created/updated!
✅ Culture information added to all products!

Press any key to exit...
```

## Configuration

### Product & Pricing Configuration

The application now reads product definitions and pricing from `appsettings.json`. You can customize the products, descriptions, and prices by editing the configuration:

```json
{
  "Stripe": {
    "Products": [
      {
        "Name": "Ink Stained Wretch - Annual Subscription",
        "Description": "Complete author management platform with annual billing...",
        "PriceInCents": 5900,
        "PriceNickname": "1 year subscription",
        "IntervalCount": 1,
        "PlanType": "annual"
      }
    ]
  }
}
```

### Customizing Products

To change pricing, cultures, or add new products:

1. **Modify existing products**: Edit the `PriceInCents` value (e.g., 5900 = $59.00)
2. **Add new products**: Add new entries to the `Products` array
3. **Change billing cycles**: Modify `IntervalCount` (1 = yearly, 2 = every 2 years, etc.)
4. **Update descriptions**: Edit the `Description` field for each product
5. **Add cultures**: Modify `SupportedCultures` array and add entries to `CultureSpecificInfo`
6. **Change primary language**: Update `PrimaryCulture` field

### Culture Configuration Example

```json
{
  "SupportedCultures": ["en-US", "es-US", "fr-CA", "de-DE"],
  "PrimaryCulture": "en-US",
  "CultureSpecificInfo": {
    "es-US": {
      "LocalizedName": "Mi Producto - Suscripción Anual",
      "LocalizedDescription": "Descripción del producto en español",
      "LocalizedNickname": "suscripción anual"
    },
    "fr-CA": {
      "LocalizedName": "Mon Produit - Abonnement Annuel",
      "LocalizedDescription": "Description du produit en français",
      "LocalizedNickname": "abonnement annuel"
    }
  }
}
```

## What Gets Created in Stripe

### Products (configurable)

1. **Ink Stained Wretch - Annual Subscription**
   - Price: $59.00/year (configurable via `PriceInCents: 5900`)
   - Description: Complete author management platform with annual billing
   - Metadata: 50+ platform features, culture support, technical info

2. **Ink Stained Wretch - 2-Year Subscription**
   - Price: $118.00/2 years (configurable via `PriceInCents: 11800`)
   - Description: Best value for committed authors with all premium features
   - Metadata: Same comprehensive feature set

3. **Ink Stained Wretch - 3-Year Subscription**
   - Price: $149.00/3 years (configurable via `PriceInCents: 14900`)
   - Description: Maximum savings for long-term commitment
   - Metadata: Same comprehensive feature set

## Product Metadata Examples

Each product includes extensive metadata with culture-specific information:

```json
{
  "plan_type": "annual",
  "platform": "ink_stained_wretch",
  "version": "1.0",
  "total_features": "65",
  "supported_cultures": "en-US,en-CA,es-US,es-MX,fr-CA,fr-FR",
  "primary_language": "en-US",
  "multi_language": "true",
  "culture_count": "6",
  "localized_versions": "3",
  "cloud_provider": "azure",
  "architecture": "serverless",
  "feature_01": "Microsoft Entra ID Integration",
  "feature_02": "Single Sign-On (SSO)",
  "feature_03": "Multi-Format Image Upload",
  "culture_01_code": "en-US",
  "culture_01_name": "English (United States)",
  "culture_01_localized_name": "Ink Stained Wretch - Annual Subscription",
  "culture_01_localized_nickname": "1 year subscription",
  "culture_02_code": "es-US",
  "culture_02_name": "Spanish (United States)",
  "culture_02_localized_name": "Escritor Manchado de Tinta - Suscripc...",
  "culture_02_localized_nickname": "suscripción de 1 año",
  "culture_03_code": "fr-CA",
  "culture_03_name": "French (Canada)",
  "culture_03_localized_name": "Écrivain Taché d'Encre - Abonnement...",
  "culture_03_localized_nickname": "abonnement de 1 an"
}
```

### Culture-Specific Features

- **Multi-language support**: Products include localized names and descriptions
- **Culture metadata**: Each supported culture has code, name, and localized content
- **Primary language**: Configurable primary culture for fallback scenarios
- **Localized nicknames**: Stripe price nicknames in multiple languages
- **Character limits**: Long names are automatically truncated to fit Stripe's metadata limits

## Integration with Your Platform

Once created, your `SubscriptionPlanService` will automatically use these rich metadata:

```csharp
// Your existing service will now get features from Stripe metadata
var subscriptionPlan = await subscriptionPlanService.MapToSubscriptionPlanAsync(priceDto);

Console.WriteLine($"Plan: {subscriptionPlan.Label}");
Console.WriteLine($"Features count: {subscriptionPlan.Features.Count}");
Console.WriteLine($"Features: {string.Join(", ", subscriptionPlan.Features.Take(5))}...");

// Output:
// Plan: 1 year subscription
// Features count: 65
// Features: Microsoft Entra ID Integration, Single Sign-On (SSO), Multi-Format Image Upload, Azure Blob Storage Integration, Tier-Based Storage Plans...
```

## Verifying in Stripe Dashboard

1. Log into your [Stripe Dashboard](https://dashboard.stripe.com/)
2. Go to **Products** → **Products**
3. You should see your three "Ink Stained Wretch" products
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
- Verify you can access https://api.stripe.com
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

## Security Reminder

- Never commit real API keys to source control
- Use test keys (`sk_test_`) for development
- Use live keys (`sk_live_`) only in production
- Rotate keys regularly for security
