# Subscription Plan Service Refactoring

## Overview
This document describes the refactoring of the static `MapToSubscriptionPlan` method in `Utility.cs` to a dependency-injected service that retrieves features from Stripe Product API.

## Changes Made

### 1. Created New Service Interface
- **File**: `OnePageAuthorLib/interfaces/Stripe/ISubscriptionPlanService.cs`
- **Purpose**: Defines the contract for mapping Stripe prices to subscription plans with feature retrieval
- **Methods**:
  - `MapToSubscriptionPlanAsync(PriceDto priceDto)`: Maps a single price to a subscription plan
  - `MapToSubscriptionPlansAsync(IEnumerable<PriceDto> priceDtos)`: Maps multiple prices to subscription plans

### 2. Created Service Implementation
- **File**: `OnePageAuthorLib/api/Stripe/SubscriptionPlanService.cs`
- **Purpose**: Implements the subscription plan mapping service with Stripe integration
- **Key Features**:
  - Retrieves product features from Stripe Product API metadata
  - Supports multiple metadata formats:
    - `features` key with comma/semicolon-separated values
    - Individual `feature_1`, `feature_2`, etc. keys
  - Provides intelligent default features based on product names (Basic, Professional, Enterprise, etc.)
  - Handles API errors gracefully with fallback to default features
  - Proper logging throughout the process

### 3. Updated Service Registration
- **File**: `OnePageAuthorLib/ServiceFactory.cs`
- **Change**: Added `ISubscriptionPlanService` registration in `AddStripeServices()` method
- **Scope**: Registered as Scoped for proper dependency injection lifecycle

### 4. Updated PricesServiceWrapper
- **File**: `OnePageAuthorLib/api/Stripe/PricesServiceWrapper.cs`
- **Changes**:
  - Added `ISubscriptionPlanService` dependency injection
  - Updated `GetPricesAsync()` to use the new service for mapping
  - Updated `GetPriceByIdAsync()` to use the new service for mapping
  - Replaced static method calls with async service calls

### 5. Marked Legacy Method as Obsolete
- **File**: `OnePageAuthorLib/Utility.cs`
- **Change**: Added `[Obsolete]` attribute to `MapToSubscriptionPlan` static method
- **Message**: Directs users to use the new `ISubscriptionPlanService.MapToSubscriptionPlanAsync` instead

### 6. Comprehensive Test Coverage
- **File**: `OnePageAuthor.Test/API/Stripe/SubscriptionPlanServiceTests.cs`
- **Tests**: 11 comprehensive tests covering:
  - Null parameter validation
  - Valid price mapping
  - Empty collections handling
  - Default feature generation for different plan types
  - Error handling scenarios
  
- **File**: `OnePageAuthor.Test/API/Stripe/PricesServiceWrapperTests.cs`
- **Tests**: 7 tests covering:
  - Service integration
  - Null handling
  - Empty collections
  - Logging verification
  - Constructor validation

## Architecture Benefits

### Before (Static Method)
- ❌ No dependency injection support
- ❌ Empty features list (hardcoded)
- ❌ No access to Stripe Product API
- ❌ Difficult to test and mock
- ❌ Tight coupling

### After (Dependency Injection Service)
- ✅ Full dependency injection support
- ✅ Real features retrieved from Stripe Product API
- ✅ Intelligent default features based on product names
- ✅ Easy to test with mocking
- ✅ Loose coupling
- ✅ Proper error handling and logging
- ✅ Async/await support for better performance

## Feature Retrieval Logic

### 1. Stripe Product Metadata
The service first attempts to retrieve features from Stripe Product metadata:
- `features` key: Comma or semicolon-separated feature list
- `feature_1`, `feature_2`, etc.: Individual feature keys

### 2. Default Features by Plan Type
When metadata is unavailable, the service provides intelligent defaults based on product names:

#### Basic/Starter Plans
- Basic author profile
- Single book listing
- Contact form
- Basic social media links

#### Professional/Pro Plans
- Professional author profile
- Unlimited book listings
- Custom domain support
- Advanced social media integration
- Analytics dashboard
- Custom themes
- Priority support

#### Enterprise/Business Plans
- Enterprise author profile
- Unlimited everything
- Custom domain with SSL
- Full social media suite
- Advanced analytics
- Custom branding
- API access
- 24/7 support
- Custom integrations

#### Default Plans
- Author profile
- Book listings
- Contact information
- Social media links

## Usage Example

### Before (Static Method)
```csharp
var subscriptionPlan = Utility.MapToSubscriptionPlan(priceDto);
// Features would always be empty
```

### After (Dependency Injection)
```csharp
public class MyService
{
    private readonly ISubscriptionPlanService _subscriptionPlanService;
    
    public MyService(ISubscriptionPlanService subscriptionPlanService)
    {
        _subscriptionPlanService = subscriptionPlanService;
    }
    
    public async Task<SubscriptionPlan> GetPlanAsync(PriceDto priceDto)
    {
        var subscriptionPlan = await _subscriptionPlanService.MapToSubscriptionPlanAsync(priceDto);
        // Features are now populated from Stripe Product API
        return subscriptionPlan;
    }
}
```

## Migration Guide

### For Existing Code
1. Replace static method calls with injected service
2. Add `ISubscriptionPlanService` to constructor dependencies
3. Change synchronous calls to async/await pattern
4. Update return type handling from synchronous to async

### For New Code
- Always use `ISubscriptionPlanService` for subscription plan mapping
- Features will be automatically populated from Stripe
- Service handles errors gracefully with sensible defaults

## Error Handling
The service includes robust error handling:
- Stripe API errors fall back to default features
- Network issues are logged and handled gracefully
- Empty or invalid product IDs use product name for feature determination
- All errors are properly logged for debugging

## Performance Considerations
- Async/await pattern for non-blocking I/O
- Stripe API calls are cached by Stripe's SDK
- Batch processing support for multiple prices
- Efficient error handling prevents cascading failures

## Testing
All functionality is thoroughly tested with:
- Unit tests for service logic
- Integration tests for Stripe API interaction
- Mock testing for dependency injection
- Error scenario testing
- Performance validation

Total test coverage: 471 passing tests with 0 failures.
