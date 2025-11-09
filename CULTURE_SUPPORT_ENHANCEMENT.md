# Culture Support Enhancement for GetStripePriceInformation

## Overview

Enhanced the `GetStripePriceInformation` Azure Function to support culture-specific subscription plans while providing fallback to default culture when none is specified.

## Changes Made

### 1. Enhanced Request Model

**File**: `OnePageAuthorLib\entities\Stripe\PriceDTOs.cs`

Added `Culture` property to `PriceListRequest`:

```csharp
public class PriceListRequest
{
    // ... existing properties ...
    public string Culture { get; set; } = string.Empty; // New culture property
}

```

### 2. Updated Service Interfaces

**File**: `OnePageAuthorLib\interfaces\Stripe\ISubscriptionPlanService.cs`

Added culture parameter to interface methods:

```csharp
Task<SubscriptionPlan> MapToSubscriptionPlanAsync(PriceDto priceDto, string? culture = null);
Task<List<SubscriptionPlan>> MapToSubscriptionPlansAsync(IEnumerable<PriceDto> priceDtos, string? culture = null);

```

**File**: `OnePageAuthorLib\interfaces\Stripe\IPriceServiceWrapper.cs`

Added culture support to wrapper interface:

```csharp
Task<SubscriptionPlan?> GetPriceByIdAsync(string priceId, string? culture = null);

```

### 3. Enhanced Service Implementation

**File**: `OnePageAuthorLib\api\Stripe\SubscriptionPlanService.cs`

#### New Localization Methods

1. **`GetLocalizedContentAsync`**: Retrieves culture-specific content from Stripe metadata
2. **`NormalizeCultureCode`**: Normalizes culture codes to consistent format
3. **`GetCultureMetadataKey`**: Finds metadata keys for specific cultures
4. **`GetLocalizedDescriptionFromMetadata`**: Gets localized descriptions (placeholder for future enhancement)

#### Culture Mapping

- `en` or `english` → `en-US`
- `es` or `spanish` → `es-US`
- `fr` or `french` → `fr-CA`
- Plus explicit mappings for `en-CA`, `es-MX`, `fr-FR`, etc.

### 4. Updated Function Implementation

**File**: `InkStainedWretchStripe\GetStripePriceInformation.cs`

Enhanced to set default culture and log culture usage:

```csharp
// Set default culture if not provided
if (string.IsNullOrEmpty(request.Culture))
{
    request.Culture = "en-US"; // Default culture
    _logger.LogDebug("No culture specified in request, using default culture: en-US");
}
else
{
    _logger.LogInformation("Processing price request for culture: {Culture}", request.Culture);
}

```

## Usage Examples

### API Request with Culture

```json
{
  "active": true,
  "limit": 10,
  "culture": "es-US"
}

```

### API Request without Culture (uses default)

```json
{
  "active": true,
  "limit": 10
}

```

## How Culture Localization Works

1. **Request Processing**: Function checks if `Culture` is provided in request
2. **Default Fallback**: If no culture specified, defaults to `en-US`
3. **Culture Normalization**: Culture codes are normalized (e.g., "es" → "es-US")
4. **Metadata Lookup**: Service searches Stripe product metadata for culture-specific content
5. **Localized Content**: Returns localized names, nicknames, and descriptions if available
6. **Graceful Fallback**: If localized content not found, returns original content

## Stripe Metadata Integration

The system looks for culture-specific metadata in Stripe products created by the `StripeProductManager`:

```text
culture_01_code: "en-US"
culture_01_localized_name: "Ink Stained Wretch - Annual Subscription"
culture_01_localized_nickname: "1 year subscription"

culture_02_code: "es-US"
culture_02_localized_name: "Escritor Manchado de Tinta - Suscripción Anual"
culture_02_localized_nickname: "suscripción de 1 año"

```

## Supported Cultures

Default configuration supports:

- **English**: `en-US` (default), `en-CA`
- **Spanish**: `es-US`, `es-MX`
- **French**: `fr-CA`, `fr-FR`

## Error Handling

- Invalid/unrecognized cultures fall back to original content
- Stripe API errors during metadata retrieval are logged and handled gracefully
- Missing localized content falls back to default product information
- All operations maintain backward compatibility

## Benefits

1. **Internationalization**: Full support for multiple languages/cultures
2. **Backward Compatibility**: Existing API calls work unchanged
3. **Flexible Fallback**: Always returns content even if localization fails
4. **Rich Logging**: Comprehensive logging for debugging and monitoring
5. **Culture Normalization**: Handles various culture code formats
6. **Integration Ready**: Works seamlessly with StripeProductManager culture metadata

## Future Enhancements

- Add more supported cultures
- Implement culture-specific pricing
- Add timezone-aware billing cycles
- Support for RTL (Right-to-Left) languages
- Dynamic culture loading from external resources

## Testing Recommendations

1. Test with supported cultures (`en-US`, `es-US`, `fr-CA`)
2. Test with unsupported cultures (should fallback gracefully)
3. Test with empty/null culture (should use `en-US` default)
4. Test with malformed culture codes
5. Verify localized content matches Stripe metadata
6. Test error scenarios (network issues, missing products)

This enhancement ensures your API can serve localized subscription plan information while maintaining full backward compatibility.
