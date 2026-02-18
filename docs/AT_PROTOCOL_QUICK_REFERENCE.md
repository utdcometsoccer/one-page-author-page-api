# AT Protocol Quick Reference

**For OnePageAuthor Platform Developers**

## What is AT Protocol?

The **AT Protocol** (Authenticated Transfer Protocol) is a decentralized social networking protocol used by Bluesky. It enables:

- **Portable Identity**: Authors own their identity (DID) independent of platforms
- **Federated Network**: Multiple servers can interoperate
- **Content Ownership**: Authors control their data and can migrate between servers
- **Open Standards**: Anyone can build AT Protocol-compatible apps

## Key Concepts

### Decentralized Identifier (DID)

A globally unique, portable identifier for each user.

```
Format: did:plc:xyz123abc456...
Example: did:plc:z72i7hdynmk6r22z27h6tvur
```

### Personal Data Server (PDS)

The server that hosts a user's data repository (posts, profile, follows).

```
Default: https://bsky.social
Custom: https://pds.example.com
```

### Lexicon

Schema system for data interchange. Common schemas:

- `app.bsky.feed.post` - Social media post
- `app.bsky.actor.profile` - User profile
- `com.atproto.repo.createRecord` - Create a record

### Repository

A signed, versioned collection of a user's data.

## API Endpoints (Proposed)

### Session Management

```http
# Create Session (Login)
POST /api/atprotocol/session
Authorization: Bearer <entra-jwt>
{
  "identifier": "author.bsky.social",
  "password": "app-password"
}

# Refresh Session
POST /api/atprotocol/session/refresh
Authorization: Bearer <entra-jwt>
{
  "refreshToken": "..."
}

# Get Session Status
GET /api/atprotocol/session
Authorization: Bearer <entra-jwt>

# Disconnect
DELETE /api/atprotocol/session
Authorization: Bearer <entra-jwt>
```

### Profile Management

```http
# Get Profile
GET /api/atprotocol/profile
Authorization: Bearer <entra-jwt>

# Update Profile
PUT /api/atprotocol/profile
Authorization: Bearer <entra-jwt>
{
  "displayName": "John Smith",
  "description": "Author of sci-fi novels",
  "avatar": "base64-image-data"
}
```

### Post Creation

```http
# Create Post
POST /api/atprotocol/posts
Authorization: Bearer <entra-jwt>
{
  "text": "Just published a new chapter!",
  "embed": {
    "type": "external",
    "uri": "https://example.com/chapter-5",
    "title": "Chapter 5",
    "description": "An exciting chapter..."
  },
  "tags": ["writing", "scifi"],
  "languages": ["en"]
}

# Get Post
GET /api/atprotocol/posts/{uri}
Authorization: Bearer <entra-jwt>

# Delete Post
DELETE /api/atprotocol/posts/{uri}
Authorization: Bearer <entra-jwt>
```

## Data Models

### ATProtocolCredential (Cosmos DB Entity)

```csharp
public class ATProtocolCredential
{
    public string id { get; set; }
    public string AuthorID { get; set; }              // Partition key
    public string DID { get; set; }
    public string Handle { get; set; }
    public string PDSEndpoint { get; set; }
    public string AccessToken { get; set; }           // Encrypted
    public string RefreshToken { get; set; }          // Encrypted
    public DateTime TokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public bool IsActive { get; set; }
}
```

**Container**: `ATProtocolCredentials`  
**Partition Key**: `/AuthorID`

### Social Entity (Extended)

```csharp
public class Social
{
    // Existing fields
    public string id { get; set; }
    public string AuthorID { get; set; }
    public string Name { get; set; }
    public Uri URL { get; set; }
    
    // New AT Protocol fields
    public string? DID { get; set; }
    public string? ATProtocolHandle { get; set; }
    public bool IsATProtocolEnabled { get; set; }
}
```

## Service Layer

### IATProtocolService Interface

```csharp
public interface IATProtocolService
{
    // Session
    Task<ATProtocolSession> CreateSessionAsync(string identifier, string password);
    Task<ATProtocolSession> RefreshSessionAsync(string refreshToken);
    
    // Profile
    Task<ATProtocolProfile> GetProfileAsync(string did);
    Task<ATProtocolProfile> UpdateProfileAsync(string did, ATProtocolProfileUpdate update);
    
    // Posts
    Task<ATProtocolPost> CreatePostAsync(string did, string text, ATProtocolPostOptions? options = null);
    Task<ATProtocolPost> GetPostAsync(string uri);
    Task DeletePostAsync(string uri);
    
    // Identity
    Task<string> ResolveDIDAsync(string handle);
}
```

### Usage Example

```csharp
public class ExampleUsage
{
    private readonly IATProtocolService _atProtocolService;
    private readonly IATProtocolTokenService _tokenService;
    
    public async Task CreatePostExample(string authorId, string text)
    {
        // Get access token for author
        var accessToken = await _tokenService.GetAccessTokenAsync(authorId);
        
        // Create post
        var post = await _atProtocolService.CreatePostAsync(
            accessToken,
            text,
            new ATProtocolPostOptions
            {
                Embed = new ExternalEmbed
                {
                    Uri = "https://example.com/article",
                    Title = "My Article",
                    Description = "Check out my latest work"
                },
                Languages = new[] { "en" }
            }
        );
        
        Console.WriteLine($"Posted to Bluesky: {post.Uri}");
    }
}
```

## Configuration

### Environment Variables

```bash
# AT Protocol Settings
ATPROTOCOL_DEFAULT_PDS_URL=https://bsky.social
ATPROTOCOL_CLIENT_ID=onepageauthor-client
ATPROTOCOL_ENABLE_AUTO_SYNC=true
ATPROTOCOL_SYNC_INTERVAL_MINUTES=60

# Encryption (Azure Key Vault)
AZURE_KEY_VAULT_URL=https://your-vault.vault.azure.net/
ATPROTOCOL_ENCRYPTION_KEY_NAME=atprotocol-token-key
```

### local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "ATPROTOCOL_DEFAULT_PDS_URL": "https://bsky.social",
    "ATPROTOCOL_CLIENT_ID": "onepageauthor-dev",
    "ATPROTOCOL_ENABLE_AUTO_SYNC": "false",
    "AZURE_KEY_VAULT_URL": "https://dev-vault.vault.azure.net/"
  }
}
```

## Security Best Practices

### 1. Token Storage

**ALWAYS** encrypt tokens before storing in Cosmos DB:

```csharp
public async Task StoreTokenAsync(string authorId, string accessToken)
{
    // ✅ Correct: Encrypt before storage
    var encryptedToken = await _encryptionService.EncryptAsync(accessToken);
    
    var credential = new ATProtocolCredential
    {
        AuthorID = authorId,
        AccessToken = encryptedToken  // Store encrypted
    };
    
    await _repository.AddAsync(credential);
}

// ❌ Wrong: Never store plaintext tokens
var credential = new ATProtocolCredential
{
    AccessToken = accessToken  // SECURITY VIOLATION!
};
```

### 2. Password Handling

**NEVER** store passwords:

```csharp
// ✅ Correct: Use password only to create session, then discard
public async Task ConnectAccountAsync(string identifier, string password)
{
    var session = await _atProtocolService.CreateSessionAsync(identifier, password);
    // Password is out of scope here - not stored
    
    await StoreTokensAsync(session.AccessToken, session.RefreshToken);
}

// ❌ Wrong: Never store passwords
var credential = new ATProtocolCredential
{
    Password = password  // NEVER DO THIS!
};
```

### 3. Token Refresh

Automatically refresh tokens before expiration:

```csharp
public async Task<string> GetAccessTokenAsync(string authorId)
{
    var credential = await _repository.GetByAuthorIdAsync(authorId);
    
    // Refresh if expiring within 5 minutes
    if (credential.TokenExpiresAt <= DateTime.UtcNow.AddMinutes(5))
    {
        var refreshToken = await _encryptionService.DecryptAsync(credential.RefreshToken);
        var newSession = await _atProtocolService.RefreshSessionAsync(refreshToken);
        
        credential.AccessToken = await _encryptionService.EncryptAsync(newSession.AccessToken);
        credential.TokenExpiresAt = DateTime.UtcNow.AddHours(24);
        
        await _repository.UpdateAsync(credential);
    }
    
    return await _encryptionService.DecryptAsync(credential.AccessToken);
}
```

## Error Handling

### Standard Error Pattern

```csharp
public async Task<Result<T>> PerformATProtocolOperationAsync<T>(Func<Task<T>> operation)
{
    try
    {
        var result = await operation();
        return Result<T>.Success(result);
    }
    catch (ATProtocolRateLimitException ex)
    {
        _logger.LogWarning("Rate limit exceeded: {Message}", ex.Message);
        return Result<T>.Failure("Rate limit exceeded. Please try again in a few minutes.");
    }
    catch (ATProtocolAuthenticationException ex)
    {
        _logger.LogError("Authentication failed: {Message}", ex.Message);
        return Result<T>.Failure("Authentication failed. Please reconnect your account.");
    }
    catch (ATProtocolNetworkException ex)
    {
        _logger.LogError("Network error: {Message}", ex.Message);
        return Result<T>.Failure("Network error. Please check your connection.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected AT Protocol error");
        return Result<T>.Failure("An unexpected error occurred.");
    }
}
```

### Custom Exceptions

```csharp
public class ATProtocolException : Exception
{
    public ATProtocolException(string message) : base(message) { }
}

public class ATProtocolAuthenticationException : ATProtocolException
{
    public ATProtocolAuthenticationException(string message) : base(message) { }
}

public class ATProtocolRateLimitException : ATProtocolException
{
    public int RetryAfterSeconds { get; }
    
    public ATProtocolRateLimitException(string message, int retryAfter) : base(message)
    {
        RetryAfterSeconds = retryAfter;
    }
}

public class ATProtocolNetworkException : ATProtocolException
{
    public ATProtocolNetworkException(string message, Exception? inner = null) 
        : base(message) { }
}
```

## Testing

### Unit Test Example

```csharp
[Fact]
public async Task CreateSession_ValidCredentials_ReturnsSession()
{
    // Arrange
    var mockHttpClient = new Mock<HttpClient>();
    var service = new ATProtocolService(mockHttpClient.Object);
    
    // Act
    var session = await service.CreateSessionAsync("author.bsky.social", "password");
    
    // Assert
    Assert.NotNull(session);
    Assert.NotEmpty(session.AccessToken);
    Assert.NotEmpty(session.DID);
}
```

### Integration Test Example

```csharp
[Fact]
public async Task CreatePost_ValidToken_PostsToBluesky()
{
    // Arrange
    var service = new ATProtocolService(_realHttpClient);
    var token = await GetTestTokenAsync();
    
    // Act
    var post = await service.CreatePostAsync(
        token,
        "Test post from OnePageAuthor integration tests",
        null
    );
    
    // Assert
    Assert.NotNull(post);
    Assert.StartsWith("at://", post.Uri);
    
    // Cleanup
    await service.DeletePostAsync(post.Uri);
}
```

## Debugging

### Enable Verbose Logging

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);  // Set to Debug for AT Protocol debugging
});
```

### Log AT Protocol Requests

```csharp
public class LoggingATProtocolService : IATProtocolService
{
    private readonly IATProtocolService _innerService;
    private readonly ILogger _logger;
    
    public async Task<ATProtocolPost> CreatePostAsync(string token, string text, ATProtocolPostOptions? options)
    {
        _logger.LogDebug("Creating AT Protocol post: {Text}", text);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _innerService.CreatePostAsync(token, text, options);
            
            _logger.LogInformation(
                "AT Protocol post created in {ElapsedMs}ms: {Uri}",
                stopwatch.ElapsedMilliseconds,
                result.Uri
            );
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create AT Protocol post after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

## Common Pitfalls

### ❌ Don't: Store Passwords

```csharp
// WRONG - Security violation
public class ATProtocolCredential
{
    public string Password { get; set; }  // NEVER store passwords!
}
```

### ❌ Don't: Forget to Encrypt Tokens

```csharp
// WRONG - Tokens must be encrypted
await _repository.AddAsync(new ATProtocolCredential
{
    AccessToken = plainTextToken  // Should be encrypted!
});
```

### ❌ Don't: Ignore Token Expiration

```csharp
// WRONG - Token may be expired
var token = credential.AccessToken;
await _atProtocolService.CreatePostAsync(token, text);  // May fail!
```

### ❌ Don't: Block on Async Calls

```csharp
// WRONG - Blocking async call
var session = _atProtocolService.CreateSessionAsync(id, pw).Result;  // Can cause deadlock!
```

### ✅ Do: Use Async/Await Properly

```csharp
// CORRECT
var session = await _atProtocolService.CreateSessionAsync(identifier, password);
```

### ✅ Do: Handle Errors Gracefully

```csharp
// CORRECT
try
{
    await _atProtocolService.CreatePostAsync(token, text);
}
catch (ATProtocolAuthenticationException)
{
    // Show user-friendly message to reconnect
    return Results.Unauthorized("Please reconnect your Bluesky account");
}
```

### ✅ Do: Use Rate Limiting

```csharp
// CORRECT
[RateLimit(RequestsPerMinute = 30)]
public async Task<IActionResult> CreatePost([FromBody] PostRequest request)
{
    // Implementation
}
```

## Resources

- **Full Documentation**: `/docs/AT_PROTOCOL_IMPLEMENTATION.md`
- **AT Protocol Docs**: https://atproto.com/
- **Bluesky API Docs**: https://docs.bsky.app/
- **GitHub Issues**: Tag AT Protocol questions with `atprotocol` label

## Frequently Asked Questions

**Q: What's the character limit for AT Protocol posts?**  
A: 300 characters (as of current spec).

**Q: How long are access tokens valid?**  
A: Typically 24 hours, but may vary by PDS.

**Q: Can users bring their own PDS?**  
A: Not in MVP, but planned for v3.0.

**Q: What happens if AT Protocol is down?**  
A: OnePageAuthor continues to function normally. AT Protocol features gracefully degrade.

**Q: How do we handle rate limiting?**  
A: Implement exponential backoff and queue posts for batch processing.

**Q: Is AT Protocol production-ready?**  
A: Bluesky is live with millions of users, but the protocol is still evolving. Monitor for breaking changes.

---

**Last Updated**: February 18, 2026  
**For Questions**: Contact the technical lead or open a GitHub discussion
