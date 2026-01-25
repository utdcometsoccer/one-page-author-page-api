# Author Invitation API Endpoint

## Overview

The Author Invitation endpoint allows authenticated administrators to send invitations to authors to create accounts linked to their domains.

## Endpoint

**POST** `/api/author-invitations`

## Authentication

This endpoint requires JWT authentication. Include a valid bearer token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Request

### Headers
- `Content-Type: application/json`
- `Authorization: Bearer <token>`

### Body

```json
{
  "emailAddress": "author@example.com",
  "domainName": "example.com",
  "notes": "Optional notes about the invitation"
}
```

### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| emailAddress | string | Yes | Valid email address of the author to invite |
| domainName | string | Yes | Valid domain name to link to the author's account |
| notes | string | No | Optional notes about the invitation |

## Response

### Success Response (201 Created)

```json
{
  "id": "abc-123-def-456",
  "emailAddress": "author@example.com",
  "domainName": "example.com",
  "status": "Pending",
  "createdAt": "2024-01-01T00:00:00Z",
  "expiresAt": "2024-01-31T00:00:00Z",
  "notes": "Optional notes about the invitation",
  "emailSent": true
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| id | string | Unique identifier for the invitation |
| emailAddress | string | Email address of the invited author |
| domainName | string | Domain name linked to the invitation |
| status | string | Current status of the invitation (e.g., "Pending") |
| createdAt | DateTime | When the invitation was created (UTC) |
| expiresAt | DateTime | When the invitation expires (UTC, 30 days from creation) |
| notes | string | Notes about the invitation (if provided) |
| emailSent | boolean | Whether the invitation email was sent successfully |

### Error Responses

#### 400 Bad Request

Returned when:
- Request body is invalid or missing
- Email address is missing or invalid format
- Domain name is missing or invalid format

```json
{
  "error": "Invalid email address format: invalid-email"
}
```

#### 401 Unauthorized

Returned when the JWT token is missing or invalid.

#### 500 Internal Server Error

Returned when an unexpected error occurs during processing.

## Examples

### cURL Example

```bash
curl -X POST https://your-function-app.azurewebsites.net/api/author-invitations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "emailAddress": "author@example.com",
    "domainName": "example.com",
    "notes": "Welcome to One Page Author!"
  }'
```

### PowerShell Example

```powershell
$headers = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer YOUR_JWT_TOKEN"
}

$body = @{
    emailAddress = "author@example.com"
    domainName = "example.com"
    notes = "Welcome to One Page Author!"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://your-function-app.azurewebsites.net/api/author-invitations" `
    -Method Post `
    -Headers $headers `
    -Body $body
```

## Configuration

### Required Environment Variables

- `COSMOSDB_ENDPOINT_URI`: Cosmos DB endpoint for storing invitations
- `COSMOSDB_PRIMARY_KEY`: Cosmos DB primary key
- `COSMOSDB_DATABASE_ID`: Database name (typically "OnePageAuthor")
- `AAD_TENANT_ID`: Azure AD tenant ID for authentication
- `AAD_AUDIENCE`: API client ID for JWT validation

### Optional Environment Variables

- `ACS_CONNECTION_STRING`: Azure Communication Services connection string for sending emails
- `ACS_SENDER_ADDRESS`: Email sender address (defaults to "DoNotReply@onepageauthor.com")

**Note**: If `ACS_CONNECTION_STRING` is not configured, invitations will be created in the database but emails will not be sent. The API will log a warning and set `emailSent: false` in the response.

## Validation Rules

### Email Validation
- Must be a valid email address format
- Validated using `System.Net.Mail.MailAddress`

### Domain Validation
- Must be a valid DNS hostname
- Validated using `Uri.CheckHostName()`
- Examples of valid domains: `example.com`, `subdomain.example.com`, `author-site.io`

## Notes

- Invitations expire 30 days after creation
- If an invitation already exists for an email address, a warning is logged but a new invitation is still created
- The endpoint follows the same authentication and authorization patterns as other InkStainedWretchFunctions endpoints
- All timestamps are in UTC

## Related

- Console Application: `AuthorInvitationTool` (legacy, replaced by this endpoint)
- Entity: `AuthorInvitation` in `OnePageAuthorLib/entities/`
- Repository: `IAuthorInvitationRepository` in `OnePageAuthorLib/nosql/`
- Email Service: `IEmailService` in `OnePageAuthorLib/services/`
