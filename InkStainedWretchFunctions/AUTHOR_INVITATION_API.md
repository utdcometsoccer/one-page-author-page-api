# Author Invitation API Endpoints

## Overview

The Author Invitation API allows authenticated administrators to manage invitations for authors to create accounts linked to their domains. The API now supports multiple domains per invitation, updating pending invitations, and resending invitation emails.

## Authentication

All endpoints require JWT authentication. Include a valid bearer token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

---

## Endpoints

### 1. Create Author Invitation

**POST** `/api/author-invitations`

Creates a new author invitation with one or more domains.

#### Request Headers

- `Content-Type: application/json`
- `Authorization: Bearer <token>`

#### Request Body

```json
{
  "emailAddress": "author@example.com",
  "domainNames": ["example.com", "author-site.com"],
  "notes": "Optional notes about the invitation"
}
```

**Backward Compatible (Single Domain):**
```json
{
  "emailAddress": "author@example.com",
  "domainName": "example.com",
  "notes": "Optional notes"
}
```

#### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| emailAddress | string | Yes | Valid email address of the author to invite |
| domainNames | array[string] | Yes* | Array of domain names to link to the author's account |
| domainName | string | Yes* | Single domain name (backward compatibility) |
| notes | string | No | Optional notes about the invitation |

\* Either `domainNames` or `domainName` must be provided

#### Response (201 Created)

```json
{
  "id": "abc-123-def-456",
  "emailAddress": "author@example.com",
  "domainName": "example.com",
  "domainNames": ["example.com", "author-site.com"],
  "status": "Pending",
  "createdAt": "2024-01-01T00:00:00Z",
  "expiresAt": "2024-01-31T00:00:00Z",
  "notes": "Optional notes about the invitation",
  "emailSent": true,
  "lastEmailSentAt": "2024-01-01T00:00:00Z"
}
```

#### Error Responses

- **400 Bad Request**: Invalid request body, email format, or domain format
- **401 Unauthorized**: Missing or invalid JWT token
- **409 Conflict**: Invitation already exists for this email address
- **500 Internal Server Error**: Unexpected error

#### Example

```bash
curl -X POST https://your-function-app.azurewebsites.net/api/author-invitations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "emailAddress": "author@example.com",
    "domainNames": ["example.com", "author-blog.com"],
    "notes": "Premium author invitation"
  }'
```

---

### 2. List Author Invitations

**GET** `/api/author-invitations`

Lists all pending author invitations.

#### Request Headers

- `Authorization: Bearer <token>`

#### Response (200 OK)

```json
[
  {
    "id": "abc-123",
    "emailAddress": "author1@example.com",
    "domainName": "example.com",
    "domainNames": ["example.com", "author-site.com"],
    "status": "Pending",
    "createdAt": "2024-01-01T00:00:00Z",
    "expiresAt": "2024-01-31T00:00:00Z",
    "lastEmailSentAt": "2024-01-01T00:00:00Z",
    "lastUpdatedAt": "2024-01-05T00:00:00Z",
    "notes": "Notes"
  },
  {
    "id": "def-456",
    "emailAddress": "author2@example.com",
    "domainName": "another.com",
    "domainNames": ["another.com"],
    "status": "Pending",
    "createdAt": "2024-01-02T00:00:00Z",
    "expiresAt": "2024-02-01T00:00:00Z"
  }
]
```

#### Example

```bash
curl -X GET https://your-function-app.azurewebsites.net/api/author-invitations \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 3. Get Author Invitation

**GET** `/api/author-invitations/{id}`

Gets details of a specific author invitation.

#### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| id | string | The invitation ID |

#### Request Headers

- `Authorization: Bearer <token>`

#### Response (200 OK)

```json
{
  "id": "abc-123-def-456",
  "emailAddress": "author@example.com",
  "domainName": "example.com",
  "domainNames": ["example.com", "author-site.com"],
  "status": "Pending",
  "createdAt": "2024-01-01T00:00:00Z",
  "expiresAt": "2024-01-31T00:00:00Z",
  "lastEmailSentAt": "2024-01-01T00:00:00Z",
  "lastUpdatedAt": "2024-01-05T00:00:00Z",
  "notes": "Optional notes"
}
```

#### Error Responses

- **404 Not Found**: Invitation with the specified ID does not exist
- **401 Unauthorized**: Missing or invalid JWT token

#### Example

```bash
curl -X GET https://your-function-app.azurewebsites.net/api/author-invitations/abc-123-def-456 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

### 4. Update Author Invitation

**PUT** `/api/author-invitations/{id}`

Updates an existing pending author invitation. Only pending invitations can be updated.

#### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| id | string | The invitation ID |

#### Request Headers

- `Content-Type: application/json`
- `Authorization: Bearer <token>`

#### Request Body

```json
{
  "domainNames": ["example.com", "newdomain.com", "thirddomain.com"],
  "notes": "Updated notes",
  "expiresAt": "2024-02-15T00:00:00Z"
}
```

#### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| domainNames | array[string] | No | Updated list of domain names |
| notes | string | No | Updated notes |
| expiresAt | DateTime | No | Updated expiration date |

#### Response (200 OK)

```json
{
  "id": "abc-123-def-456",
  "emailAddress": "author@example.com",
  "domainName": "example.com",
  "domainNames": ["example.com", "newdomain.com", "thirddomain.com"],
  "status": "Pending",
  "createdAt": "2024-01-01T00:00:00Z",
  "expiresAt": "2024-02-15T00:00:00Z",
  "lastUpdatedAt": "2024-01-10T00:00:00Z",
  "notes": "Updated notes"
}
```

#### Error Responses

- **400 Bad Request**: Invalid request body, domain format, or invitation status is not "Pending"
- **404 Not Found**: Invitation with the specified ID does not exist
- **401 Unauthorized**: Missing or invalid JWT token

#### Example

```bash
curl -X PUT https://your-function-app.azurewebsites.net/api/author-invitations/abc-123-def-456 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "domainNames": ["example.com", "newdomain.com"],
    "notes": "Updated invitation with additional domain"
  }'
```

---

### 5. Resend Author Invitation

**POST** `/api/author-invitations/{id}/resend`

Resends the invitation email for a pending invitation.

#### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| id | string | The invitation ID |

#### Request Headers

- `Authorization: Bearer <token>`

#### Response (200 OK)

```json
{
  "id": "abc-123-def-456",
  "emailAddress": "author@example.com",
  "emailSent": true,
  "lastEmailSentAt": "2024-01-10T00:00:00Z"
}
```

#### Error Responses

- **400 Bad Request**: Invitation status is not "Pending"
- **404 Not Found**: Invitation with the specified ID does not exist
- **500 Internal Server Error**: Failed to send email
- **503 Service Unavailable**: Email service is not configured
- **401 Unauthorized**: Missing or invalid JWT token

#### Example

```bash
curl -X POST https://your-function-app.azurewebsites.net/api/author-invitations/abc-123-def-456/resend \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Common Response Fields

| Field | Type | Description |
|-------|------|-------------|
| id | string | Unique identifier for the invitation |
| emailAddress | string | Email address of the invited author |
| domainName | string | Primary domain name (first in list, for backward compatibility) |
| domainNames | array[string] | List of domain names linked to the invitation |
| status | string | Current status: "Pending", "Accepted", "Expired", "Revoked" |
| createdAt | DateTime | When the invitation was created (UTC) |
| expiresAt | DateTime | When the invitation expires (UTC, default 30 days from creation) |
| lastEmailSentAt | DateTime | When the invitation email was last sent (UTC) |
| lastUpdatedAt | DateTime | When the invitation was last updated (UTC) |
| notes | string | Optional notes about the invitation |
| emailSent | boolean | Whether the invitation email was sent successfully (create/resend operations) |

---

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

---

## Validation Rules

### Email Validation

- Must be a valid email address format
- Validated using `System.Net.Mail.MailAddress`

### Domain Validation

- Must be a valid DNS hostname
- Must contain at least one dot
- Cannot be an IP address (IPv4 or IPv6)
- Cannot be "localhost"
- Each label must be 1-63 characters
- Labels cannot start or end with hyphens
- Total length must not exceed 253 characters
- Examples of valid domains: `example.com`, `subdomain.example.com`, `author-site.io`

---

## Status Workflow

```
Pending → Accepted (when author accepts)
Pending → Expired (after expiration date)
Pending → Revoked (manually cancelled by admin)
```

Only **Pending** invitations can be:
- Updated (PUT /api/author-invitations/{id})
- Resent (POST /api/author-invitations/{id}/resend)

---

## Console Application Usage

The `AuthorInvitationTool` console application has been updated to support all new features:

```bash
# Create invitation with multiple domains
AuthorInvitationTool create author@example.com example.com author-site.com --notes "Premium author"

# Create invitation with single domain (backward compatible)
AuthorInvitationTool create author@example.com example.com

# List all pending invitations
AuthorInvitationTool list

# Get details of specific invitation
AuthorInvitationTool get abc-123-def-456

# Update invitation domains
AuthorInvitationTool update abc-123-def-456 --domains example.com newdomain.com thirddomain.com

# Update invitation notes
AuthorInvitationTool update abc-123-def-456 --notes "Updated notes for this author"

# Resend invitation email
AuthorInvitationTool resend abc-123-def-456
```

---

## Notes

- Invitations expire 30 days after creation by default (can be updated)
- All timestamps are in UTC
- The endpoint follows the same authentication and authorization patterns as other InkStainedWretchFunctions endpoints
- Backward compatibility is maintained: existing code using single domain will continue to work
- Multiple domains are stored in the `domainNames` array; the `domainName` field is maintained for backward compatibility and contains the first domain

---

## Related

- Entity: `AuthorInvitation` in `OnePageAuthorLib/entities/`
- Repository: `IAuthorInvitationRepository` in `OnePageAuthorLib/nosql/`
- Email Service: `IEmailService` in `OnePageAuthorLib/services/`
- Console Application: `AuthorInvitationTool/`
- Tests: `OnePageAuthor.Test/InkStainedWretchFunctions/AuthorInvitationFunctionTests.cs`
