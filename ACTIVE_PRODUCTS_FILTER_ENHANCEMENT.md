# Active Products Filter Enhancement

## Overview

Enhanced the `GetStripePriceInformation` Azure Function to automatically filter out inactive products by ensuring the `Active` property is always set to `true` in the request.

## Changes Made

### Enhanced Function Logic

**File**: `InkStainedWretchStripe\GetStripePriceInformation.cs`

Added automatic filtering for active products:

```csharp
// Ensure we only return active products
if (request.Active != true)
{
    request.Active = true;
    _logger.LogDebug("Setting Active filter to true to exclude inactive products");
}

```

## How It Works

### Before Enhancement

- The function would pass through whatever `Active` value was provided in the request
- If `Active` was

ull` or `false`, inactive products could be returned

- Callers needed to remember to explicitly set `Active = true`

### After Enhancement

- The function **automatically ensures** `Active = true` is set
- Inactive products are **always filtered out** regardless of the input
- Provides consistent behavior and prevents accidental inclusion of inactive products
- Logs when the filter is automatically applied

## API Behavior

### Request Examples

**Request with Active explicitly set to true** (no change):

```json
{
  "active": true,
  "limit": 10,
  "culture": "en-US"
}

```

→ Behavior unchanged, active filter already applied

**Request with Active set to false** (now changed to true):

```json
{
  "active": false,
  "limit": 10,
  "culture": "en-US"
}

```

→ Automatically changed to `active: true` and logged

**Request without Active property** (now defaults to true):

```json
{
  "limit": 10,
  "culture": "en-US"
}

```

→ Automatically set to `active: true` and logged

## Integration with Existing Services

The enhancement works seamlessly with the existing service architecture:

1. **Function Level**: `GetStripePriceInformation` ensures `Active = true`
2. **Service Level**: `PricesService` passes the filter to Stripe API
3. **Stripe Level**: Stripe API returns only active prices
4. **Wrapper Level**: `PricesServiceWrapper` processes active prices only

## Benefits

1. **Data Consistency**: Ensures only active products are returned
2. **Security**: Prevents accidental exposure of inactive/disabled products
3. **User Experience**: Users only see available products they can purchase
4. **Backwards Compatible**: Existing API calls continue to work
5. **Logging**: Clear visibility when filter is automatically applied
6. **Error Prevention**: Eliminates possibility of including inactive products

## Logging

The function now logs when the active filter is automatically applied:

```text
[DEBUG] Setting Active filter to true to exclude inactive products

```

This helps with troubleshooting and monitoring to understand when the automatic filter is being applied.

## Use Cases

This enhancement is particularly valuable for:

- **Product Management**: When products are temporarily disabled in Stripe
- **A/B Testing**: When certain pricing plans are deactivated
- **Maintenance**: When products are marked inactive during updates
- **Compliance**: Ensuring only approved/active products are offered
- **User Interface**: Preventing display of unavailable subscription options

## Technical Details

### Filter Priority

The function checks `if (request.Active != true)` which means:

- `Active = true` → No change (already correct)
- `Active = false` → Changed to `true` (filtered)
- `Active = null` → Changed to `true` (filtered)

### Service Chain

1. `GetStripePriceInformation` → Sets `Active = true`
2. `PricesServiceWrapper` → Passes request through
3. `PricesService` → Maps to Stripe API options
4. `Stripe API` → Returns only active prices
5. Response chain processes active prices only

## Testing Recommendations

To verify the enhancement works correctly:

1. **Test with Active = true**: Should work as before
2. **Test with Active = false**: Should return active prices only
3. **Test with Active = null**: Should return active prices only
4. **Test with no Active property**: Should return active prices only
5. **Check logs**: Verify debug messages appear when filter is applied
6. **Verify results**: Confirm no inactive products in response

This enhancement ensures robust, consistent filtering of inactive products while maintaining full backward compatibility.
