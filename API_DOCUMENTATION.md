# Author API Documentation

## Overview
This REST API provides access to a list of author objects for a given domain. Authentication is handled via Microsoft Entra ID (Azure AD) using OAuth 2.0. Only authenticated users with the appropriate permissions can access the endpoints.

## Authentication
- **Protocol:** OAuth 2.0 (OpenID Connect)
- **Provider:** Microsoft Entra ID (Azure AD)
- **Flow:** Authorization Code or Client Credentials
- **Scopes:** `api://<your-api-client-id>/Author.Read`

## Endpoints

### GET /api/authors/{secondLevelDomain}/{topLevelDomain}
Returns a list of author objects for the specified domain.

#### Request
```http
GET /api/authors/{secondLevelDomain}/{topLevelDomain}
Authorization: Bearer <access_token>
```

#### Parameters
- `secondLevelDomain` (path): The second-level domain name (e.g., "example" from "example.com")
- `topLevelDomain` (path): The top-level domain (e.g., "com" from "example.com")

#### Response
**Success (200 OK)**
```json
[
  {
    "id": "string",
    "AuthorName": "string",
    "LanguageName": "string", 
    "RegionName": "string",
    "EmailAddress": "string",
    "WelcomeText": "string",
    "AboutText": "string", 
    "HeadShotURL": "string",
    "CopyrightText": "string",
    "TopLevelDomain": "string",
    "SecondLevelDomain": "string",
    "Articles": [
      {
        "Title": "string",
        "Date": "yyyy-MM-dd",
        "Publication": "string",
        "Url": "string"
      }
    ],
    "Books": [
      {
        "Title": "string", 
        "Description": "string",
        "Url": "string",
        "Cover": "string"
      }
    ],
    "Socials": [
      {
        "Name": "string",
        "Url": "string"
      }
    ]
  }
]
```

## Error Codes
- `401 Unauthorized`: Invalid or missing access token
- `403 Forbidden`: Insufficient permissions (missing Author.Read scope)
- `404 Not Found`: Domain not found
- `500 Internal Server Error`: Unexpected error

## Example Usage

### Using TypeScript/JavaScript
See `src/services/fetchAuthorsByDomain.ts` for a complete example including:
- Microsoft Authentication Library (MSAL) setup
- Token acquisition for server-to-server calls
- Error handling
- TypeScript interfaces

### Using cURL
```bash
curl -X GET "https://your-function-app.azurewebsites.net/api/authors/example/com" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json"
```

### Using PowerShell
```powershell
$headers = @{
    "Authorization" = "Bearer YOUR_ACCESS_TOKEN"
    "Content-Type" = "application/json"
}

$response = Invoke-RestMethod -Uri "https://your-function-app.azurewebsites.net/api/authors/example/com" -Method Get -Headers $headers
$response | ConvertTo-Json -Depth 10
```

## Security Requirements
- All requests must use HTTPS
- Access tokens must be obtained from Microsoft Entra ID
- Do not expose client secrets in client-side code
- Tokens should be stored securely and refreshed as needed

## Configuration
The API requires the following Azure AD configuration:
- **Tenant ID**: Your Azure AD tenant identifier
- **Client ID**: Application ID registered in Azure AD  
- **API Scope**: `api://your-api-client-id/Author.Read`
- **Authority**: `https://login.microsoftonline.com/{tenant-id}/v2.0`

## Rate Limiting
Standard Azure Functions rate limiting applies. Consider implementing client-side retry logic with exponential backoff.

## Support
For questions or support, contact the API administrator.
