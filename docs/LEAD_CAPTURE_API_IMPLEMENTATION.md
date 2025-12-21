# Lead Capture API - Implementation Summary

## Overview
This document summarizes the implementation of the Lead Capture API, a public endpoint for capturing email leads from landing pages, blogs, and other marketing sources.

## Endpoint
**POST** `/api/leads` - Public endpoint (no authentication required)

## Request Format
```json
{
  "email": "user@example.com",
  "firstName": "John",
  "source": "landing_page",
  "leadMagnet": "author-success-kit",
  "utmSource": "google",
  "utmMedium": "cpc",
  "utmCampaign": "spring-2024",
  "referrer": "https://google.com",
  "locale": "en-US",
  "consentGiven": true
}
```

### Valid Sources
- `landing_page`
- `blog`
- `exit_intent`
- `newsletter`

## Response Format
```json
{
  "id": "lead-123",
  "status": "created",
  "message": "Lead successfully created"
}
```

Status can be:
- `created` - New lead was created (HTTP 201)
- `existing` - Email already registered (HTTP 200)

## Error Codes
- **400 Bad Request** - Invalid email format, missing required fields, or invalid source
- **429 Too Many Requests** - Rate limit exceeded (10 requests per IP per minute)
- **500 Internal Server Error** - Server-side error

## Implementation Details

### Entity Model (`Lead.cs`)
- Comprehensive lead tracking with all required fields
- Email normalization to lowercase for consistency
- Email domain extraction for efficient partitioning
- Consent tracking for GDPR compliance
- IP address capture for rate limiting and analytics
- Timestamps for creation and updates
- Email service status tracking

### Repository (`LeadRepository.cs`)
- Implements `ILeadRepository` interface
- Cosmos DB storage with `emailDomain` partition key
- Duplicate detection based on email address
- Query capabilities by source with date range filtering
- Thread-safe operations

### Service (`LeadService.cs`)
- Implements `ILeadService` interface
- Email validation using multiple methods:
  - DataAnnotations EmailAddressAttribute
  - RFC-compliant regex pattern
- Source validation against allowed values
- Duplicate detection and handling
- Business logic for lead creation
- Placeholder for email service integration (Mailchimp/ConvertKit)

### Rate Limiting (`RateLimitService.cs`)
- In-memory rate limiting (10 requests per IP per minute)
- Thread-safe implementation using `ConcurrentBag` and locking
- Per-IP and per-endpoint tracking
- Configurable request limits
- IP address extraction from HTTP headers (X-Forwarded-For, X-Real-IP)
- **Note**: For production at scale, consider using Redis or Azure Cache for Redis

### Azure Function (`LeadCaptureFunction.cs`)
- Public HTTP endpoint (AuthorizationLevel.Anonymous)
- Comprehensive error handling with appropriate status codes
- Rate limit enforcement before processing
- Request validation at multiple levels
- IP address extraction for rate limiting
- Structured logging for monitoring

## Data Storage

### Cosmos DB Container
- Container Name: `Leads`
- Partition Key: `/emailDomain`
- Benefits:
  - Efficient queries by email domain
  - Good distribution across partitions
  - Supports duplicate detection

## Testing

### Test Coverage (42 tests, all passing)
1. **LeadServiceTests** (21 tests)
   - Lead creation with valid and invalid data
   - Email validation (multiple test cases)
   - Source validation
   - Duplicate detection
   - Email normalization
   - UTM parameter handling

2. **RateLimitServiceTests** (12 tests)
   - Rate limit enforcement
   - Different IPs tracked separately
   - Different endpoints tracked separately
   - Remaining requests calculation
   - Custom rate limit configuration

3. **LeadCaptureFunctionTests** (9 tests)
   - Valid request handling
   - Rate limit enforcement
   - Error handling (400, 429, 500)
   - Invalid data handling
   - JSON parsing errors

## Security Considerations

### Implemented
✅ Email validation to prevent injection attacks
✅ Input validation using DataAnnotations
✅ Rate limiting to prevent abuse
✅ GDPR compliance with consent tracking
✅ No authentication required (by design for public endpoint)
✅ Error messages don't expose internal details
✅ IP address tracking for analytics and rate limiting

### Recommendations for Production
1. **Rate Limiting**: Move to distributed cache (Redis) for multi-instance scenarios
2. **DDoS Protection**: Use Azure Front Door or Application Gateway with WAF rules
3. **Monitoring**: Set up Application Insights alerts for:
   - High rate limit violations
   - Error rates
   - Unusual traffic patterns
4. **Email Service**: Integrate with Mailchimp, ConvertKit, or similar
5. **Data Retention**: Implement policy for GDPR compliance (right to be forgotten)

## Email Service Integration (TODO)

The code includes a placeholder for email service integration. To complete:

1. Choose email service provider (Mailchimp, ConvertKit, SendGrid, etc.)
2. Add configuration for API keys
3. Implement async integration (consider using Azure Service Bus or Azure Functions with Queue trigger)
4. Update `EmailServiceStatus` field based on sync results
5. Handle webhook callbacks from email service
6. Implement retry logic for failed syncs

### Example Integration Points
```csharp
// In LeadService.CreateLeadAsync after lead creation:
// await _emailService.SyncLeadAsync(createdLead);

// Or use a queue for async processing:
// await _queueService.EnqueueLeadSyncAsync(createdLead);
```

## Configuration

### Required Environment Variables
- `COSMOSDB_ENDPOINT_URI` - Cosmos DB endpoint
- `COSMOSDB_PRIMARY_KEY` - Cosmos DB key
- `COSMOSDB_DATABASE_ID` - Database name

### Optional Configuration
- Rate limit max requests per minute (default: 10)

## Deployment Notes

1. Ensure Cosmos DB container is created (automatic via ContainerManager)
2. Configure Application Insights for monitoring
3. Set up alerts for error rates and rate limit violations
4. Consider Azure Front Door for global distribution
5. Review and adjust rate limits based on expected traffic

## Future Enhancements

1. **Email Service Integration**: Complete integration with Mailchimp/ConvertKit
2. **Lead Scoring**: Add lead quality scoring based on source, UTM parameters
3. **Lead Nurturing**: Automated email sequences based on lead magnet
4. **Analytics Dashboard**: Track lead conversion rates by source
5. **A/B Testing**: Support for campaign experiments
6. **Lead Enrichment**: Integration with data enrichment services
7. **Webhook Support**: Notify external systems of new leads
8. **Export Functionality**: CSV/Excel export for marketing teams

## API Documentation

For detailed API documentation, see the OpenAPI/Swagger specification (to be generated).

## Support

For issues or questions, contact the development team or open an issue in the repository.
