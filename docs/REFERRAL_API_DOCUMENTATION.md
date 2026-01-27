# Referral Program API Documentation

## Overview

The Referral Program API enables users to refer new leads to the platform and track their referral performance. Users receive credits for successful conversions when their referrals sign up for paid subscriptions.

## Endpoints

### Create Referral

Creates a new referral entry to track when a user refers someone new to the platform.

**Endpoint:** `POST /api/referrals`

**Authorization:** Anonymous (can be secured with JWT if needed)

**Request Body:**

```json
{
  "referrerId": "string",       // Existing user's ID
  "referredEmail": "string"     // Email of person being referred
}
```

**Success Response (200 OK):**

```json
{
  "referralCode": "ABC12345",
  "referralUrl": "https://inkstainedwretches.com/signup?ref=ABC12345"
}
```

**Error Responses:**

- `400 Bad Request` - Invalid request body, missing required fields, or invalid email format
- `409 Conflict` - The email has already been referred by this user
- `500 Internal Server Error` - Server error (e.g., unable to generate unique code)

**Example Request:**

```bash
curl -X POST https://your-domain.azurewebsites.net/api/referrals \
  -H "Content-Type: application/json" \
  -d '{
    "referrerId": "user-123",
    "referredEmail": "friend@example.com"
  }'
```

**Example Response:**

```json
{
  "referralCode": "K7Y2MNPQ",
  "referralUrl": "https://inkstainedwretches.com/signup?ref=K7Y2MNPQ"
}
```

**Validation Rules:**

- `referrerId` - Required, non-empty string
- `referredEmail` - Required, valid email format
- Email must not have been previously referred by the same user

---

### Get Referral Statistics

Retrieves statistics about a user's referral activity including total referrals, successful conversions, and credits earned.

**Endpoint:** `GET /api/referrals/{userId}`

**Authorization:** Anonymous (can be secured with JWT if needed)

**Path Parameters:**

- `userId` - The user's unique identifier

**Success Response (200 OK):**

```json
{
  "totalReferrals": 10,
  "successfulReferrals": 3,      // Referrals that converted to paid
  "pendingCredits": 3,            // Months of credit earned
  "redeemedCredits": 0            // Months of credit already used
}
```

**Error Responses:**

- `400 Bad Request` - Invalid or missing userId
- `500 Internal Server Error` - Server error

**Example Request:**

```bash
curl https://your-domain.azurewebsites.net/api/referrals/user-123
```

**Example Response:**

```json
{
  "totalReferrals": 15,
  "successfulReferrals": 5,
  "pendingCredits": 5,
  "redeemedCredits": 0
}
```

---

## Data Model

### Referral Entity

Stored in Cosmos DB with the following schema:

```json
{
  "id": "guid-string",
  "ReferrerId": "string",           // Partition key
  "ReferredEmail": "string",
  "ReferralCode": "string",         // 8-char alphanumeric (e.g., "ABC12345")
  "Status": "string",               // "Pending" | "Converted" | "Expired"
  "CreatedAt": "datetime",
  "UpdatedAt": "datetime?",
  "ReferredUserId": "string?",      // Populated when referred user signs up
  "ConvertedAt": "datetime?"        // Populated when user converts to paid
}
```

**Referral States:**

- `Pending` - Initial state when referral is created
- `Converted` - Referred user has signed up and subscribed to a paid plan
- `Expired` - Referral link expired or invalidated (future feature)

---

## Configuration

### Environment Variables

- `REFERRAL_BASE_URL` (optional) - Base URL for generating referral links
  - Default: `https://inkstainedwretches.com`
  - Example: `REFERRAL_BASE_URL=https://myapp.com`

### Cosmos DB Container

- **Container Name:** `Referrals`
- **Partition Key:** `/ReferrerId`
- **Automatic Creation:** Yes (created on first access if not exists)

---

## Business Logic

### Referral Code Generation

- 8-character alphanumeric codes (uppercase letters and digits)
- Uses cryptographically secure `RandomNumberGenerator`
- Uniqueness verified against database
- Maximum 5 retry attempts if collision occurs
- Example codes: `ABC12345`, `XYZ789WQ`, `M4N2P7K3`

### Referral URL Generation

Format: `{baseUrl}/signup?ref={referralCode}`

Examples:

- `https://inkstainedwretches.com/signup?ref=ABC12345`
- `https://myapp.com/signup?ref=XYZ789WQ`

### Credit Calculation

Currently, the system tracks:

- 1 month of credit per successful referral (when status = "Converted")
- Credits are tracked but redemption logic is not yet implemented
- Future: Separate tracking for pending vs. redeemed credits

---

## Integration Guide

### Frontend Integration

```javascript
// Create a referral
async function createReferral(referrerId, referredEmail) {
  const response = await fetch('/api/referrals', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ referrerId, referredEmail })
  });
  
  if (!response.ok) {
    const error = await response.text();
    throw new Error(error);
  }
  
  return await response.json();
}

// Get referral stats
async function getReferralStats(userId) {
  const response = await fetch(`/api/referrals/${userId}`);
  
  if (!response.ok) {
    throw new Error('Failed to fetch referral stats');
  }
  
  return await response.json();
}

// Usage
try {
  const result = await createReferral('user-123', 'friend@example.com');
  console.log('Referral code:', result.referralCode);
  console.log('Share this URL:', result.referralUrl);
  
  const stats = await getReferralStats('user-123');
  console.log('Total referrals:', stats.totalReferrals);
  console.log('Credits earned:', stats.pendingCredits);
} catch (error) {
  console.error('Error:', error.message);
}
```

---

## Future Enhancements

### Planned Features

1. **Referral Tracking** - Track when referred users click the referral link
2. **Credit Redemption** - Allow users to apply earned credits to subscriptions
3. **Expiration** - Automatic expiration of referral codes after a period
4. **Notification System** - Email notifications when referrals convert
5. **Referral History** - Detailed view of individual referral activity
6. **Tiered Rewards** - Different credit amounts based on subscription tier
7. **Referral Leaderboard** - Gamification with top referrers

### Webhook Integration

Consider adding webhook support to notify external systems when:

- A new referral is created
- A referral converts to paid
- Credits are earned or redeemed

---

## Testing

Comprehensive test coverage with 31 passing tests:

- **ReferralRepositoryTests** (17 tests) - Database operations
- **ReferralServiceTests** (11 tests) - Business logic
- **ReferralFunctionTests** (3 tests) - API endpoints

Run tests:

```bash
dotnet test --filter "FullyQualifiedName~Referral"
```

---

## Troubleshooting

### Common Issues

**Issue:** "This email has already been referred by you"

- **Cause:** Attempting to refer the same email twice
- **Solution:** Use a different email or check existing referrals

**Issue:** "Unable to generate a unique referral code"

- **Cause:** Too many collisions when generating codes (rare)
- **Solution:** System will retry automatically, if persistent check database

**Issue:** Invalid email format

- **Cause:** Email doesn't match standard email pattern
- **Solution:** Validate email on frontend before submission

---

## Support

For questions or issues with the Referral Program API:

1. Check this documentation first
2. Review test cases for usage examples
3. Check application logs for error details
4. Contact development team for assistance
