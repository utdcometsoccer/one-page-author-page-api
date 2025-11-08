# How to Update Stripe Price Nickname

## Option 1: Stripe Dashboard (Recommended for manual updates)

1. Go to [Stripe Dashboard](https://dashboard.stripe.com/)
2. Navigate to **Products** → **Prices**
3. Find your price and click on it
4. Edit the **Nickname** field
5. Save changes

## Option 2: Stripe API (Programmatic updates)

### Using Stripe .NET SDK

```csharp
using Stripe;

public async Task<Price> UpdatePriceNicknameAsync(string priceId, string newNickname)
{
    var service = new PriceService();
    var options = new PriceUpdateOptions
    {
        Nickname = newNickname
    };
    
    return await service.UpdateAsync(priceId, options);
}
```

### Example Usage

```csharp
// Update a price nickname
var updatedPrice = await UpdatePriceNicknameAsync("price_1234567890", "Pro Monthly Plan");
```

## Option 3: Add to Your Service

You could add a method to your existing `SubscriptionPlanService` or create a new service:

```csharp
public interface IPriceManagementService
{
    Task<bool> UpdatePriceNicknameAsync(string priceId, string newNickname);
}

public class PriceManagementService : IPriceManagementService
{
    private readonly ILogger<PriceManagementService> _logger;

    public PriceManagementService(ILogger<PriceManagementService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> UpdatePriceNicknameAsync(string priceId, string newNickname)
    {
        try
        {
            _logger.LogInformation("Updating nickname for price {PriceId} to {NewNickname}", priceId, newNickname);

            var service = new PriceService();
            var options = new PriceUpdateOptions
            {
                Nickname = newNickname
            };

            var updatedPrice = await service.UpdateAsync(priceId, options);
            
            _logger.LogInformation("Successfully updated price nickname. Price ID: {PriceId}, New Nickname: {Nickname}", 
                updatedPrice.Id, updatedPrice.Nickname);
                
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error while updating price nickname for {PriceId}: {Message}", 
                priceId, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating price nickname for {PriceId}", priceId);
            return false;
        }
    }
}
```

## Option 4: Azure Function Endpoint

Create an Azure Function to update price nicknames:

```csharp
[Function("UpdatePriceNickname")]
public async Task<IActionResult> UpdatePriceNickname(
    [HttpTrigger(AuthorizationLevel.Function, "put", Route = "prices/{priceId}/nickname")] HttpRequest req,
    string priceId,
    ILogger log)
{
    try
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updateRequest = JsonSerializer.Deserialize<UpdateNicknameRequest>(requestBody);

        if (string.IsNullOrWhiteSpace(updateRequest?.Nickname))
        {
            return new BadRequestObjectResult("Nickname is required");
        }

        var service = new PriceService();
        var options = new PriceUpdateOptions
        {
            Nickname = updateRequest.Nickname
        };

        var updatedPrice = await service.UpdateAsync(priceId, options);

        return new OkObjectResult(new 
        { 
            PriceId = updatedPrice.Id, 
            Nickname = updatedPrice.Nickname,
            Message = "Price nickname updated successfully" 
        });
    }
    catch (StripeException ex)
    {
        log.LogError(ex, "Stripe error updating price {PriceId}", priceId);
        return new BadRequestObjectResult($"Stripe error: {ex.Message}");
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error updating price {PriceId}", priceId);
        return new StatusCodeResult(500);
    }
}

public class UpdateNicknameRequest
{
    public string Nickname { get; set; } = string.Empty;
}
```

## Important Notes

1. **Price IDs are immutable** - You can only update the nickname, not create a new price with a different ID
2. **Nicknames are optional** - Prices can exist without nicknames
3. **Your SubscriptionPlanService will automatically use updated nicknames** - The next time it fetches price data, it will get the updated nickname
4. **Caching considerations** - If you're caching price data, you may need to invalidate the cache after updating nicknames

## Testing the Update

After updating a price nickname, you can test that your `SubscriptionPlanService` picks up the change:

```csharp
[Fact]
public async Task MapToSubscriptionPlanAsync_UsesUpdatedNickname()
{
    // Arrange - simulate a PriceDto with an updated nickname
    var priceDto = new PriceDto
    {
        Id = "price_test",
        ProductId = "prod_test",
        ProductName = "Professional Plan",
        Nickname = "Updated Pro Plan", // This would be the new nickname from Stripe
        UnitAmount = 1999,
        Currency = "usd",
        Active = true,
        CreatedDate = DateTime.UtcNow
    };

    // Act
    var result = await _service.MapToSubscriptionPlanAsync(priceDto);

    // Assert
    Assert.Equal("Updated Pro Plan", result.Label); // Should use the updated nickname
}
```

The recommended approach depends on your use case:
- **Manual updates**: Use Stripe Dashboard
- **Programmatic updates**: Use Stripe API with proper error handling
- **Bulk updates**: Create a management service or Azure Function