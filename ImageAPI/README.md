# ImageAPI

[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-orange.svg)](https://docs.microsoft.com/en-us/azure/azure-functions/)

Azure Functions app for image management operations including upload, retrieval, and deletion with subscription tier validation.

## 🚀 Overview

The ImageAPI provides a comprehensive image management system for the OnePageAuthor platform. It handles user image uploads to Azure Blob Storage with subscription tier-based validation, retrieval of user images, and secure deletion operations.

## 🏗️ Architecture

- **Runtime**: Azure Functions v4 (.NET 10)
- **Storage**: Azure Blob Storage
- **Authentication**: JWT Bearer tokens
- **Authorization**: User-based access control
- **Validation**: Subscription tier limits enforcement

## 📋 Features

### Subscription Tier Support


- **Starter (Free)**: 5MB max file size, 20 files max, 5GB storage
- **Pro ($9.99/month)**: 10MB max file size, 500 files max, 250GB storage
- **Elite ($19.99/month)**: 25MB max file size, 2000 files max, 2TB storage

### Security Features


- JWT token authentication on all endpoints
- User ownership validation for image operations
- Secure file upload with content type validation
- CORS support for web applications

## 🔌 API Endpoints

### Overview

| Method | Endpoint | Description | Authentication |
|--------|----------|-------------|----------------|
| `POST` | `/api/images/upload` | Upload image file | Required |
| `GET` | `/api/images/user` | Get user's images | Required |
| `DELETE` | `/api/images/{imageId}` | Delete image | Required |
| `GET` | `/api/whoami` | Get user info | Required |

### 1. Upload Image

**Endpoint:** `POST /api/images/upload`

**Description:** Upload an image file to Azure Blob Storage with subscription tier validation.

**Headers:**

- `Authorization: Bearer <token>` (required)

**Body:**

- `file`: Image file (multipart/form-data)

**Limits:**
File size and count limits depend on subscription tier:

- **Starter (Free)**: Max 5MB file size, 20 files max
- **Pro ($9.99/month)**: Max 10MB file size, 500 files max
- **Elite ($19.99/month)**: Max 25MB file size, 2000 files max

**Responses:**

**201 Created** - Image uploaded successfully

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "url": "https://storage.blob.core.windows.net/images/filename.jpg",
  "name": "filename.jpg",
  "size": 1048576
}

```

**400 Bad Request** - File too large for subscription tier

```json
{
  "error": "File size exceeds limit for your subscription tier."
}

```

**403 Forbidden** - User has reached upload limit

```json
{
  "error": "Maximum number of files reached for your subscription tier."
}

```

**402 Payment Required** - Bandwidth limit exceeded

```json
{
  "error": "Bandwidth limit exceeded for your subscription tier."
}

```

**507 Insufficient Storage** - Storage quota exceeded

```json
{
  "error": "Storage quota exceeded for your subscription tier."
}

```

**401 Unauthorized** - Invalid or missing token

**Example:**

```bash
curl -X POST "https://your-api.azurewebsites.net/api/images/upload" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "file=@image.jpg"

```

### 2. List User Images

**Endpoint:** `GET /api/images/user`

**Description:** Get a list of all images uploaded by the authenticated user.

**Headers:**

- `Authorization: Bearer <token>` (required)

**Responses:**

**200 OK** - Returns array of user images

```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "url": "https://storage.blob.core.windows.net/images/filename1.jpg",
    "name": "filename1.jpg",
    "size": 1048576,
    "uploadedAt": "2024-01-15T10:30:00Z"
  },
  {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "url": "https://storage.blob.core.windows.net/images/filename2.jpg",
    "name": "filename2.jpg",
    "size": 2097152,
    "uploadedAt": "2024-01-16T14:45:00Z"
  }
]

```

**401 Unauthorized** - Invalid or missing token

**Example:**

```bash
curl -X GET "https://your-api.azurewebsites.net/api/images/user" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

```

### 3. Delete Image

**Endpoint:** `DELETE /api/images/{id}`

**Description:** Delete an image by its ID. Only the image owner can delete their images.

**Headers:**

- `Authorization: Bearer <token>` (required)

**Path Parameters:**

- `id`: Image ID (UUID)

**Responses:**

**200 OK** - Image deleted successfully

```json
{
  "message": "Image deleted successfully"
}

```

**404 Not Found** - Image not found

```json
{
  "error": "Image not found"
}

```

**401 Unauthorized** - Invalid or missing token

**403 Forbidden** - User does not own this image

**Example:**

```bash
curl -X DELETE "https://your-api.azurewebsites.net/api/images/550e8400-e29b-41d4-a716-446655440000" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

```

### 4. WhoAmI

**Endpoint:** `GET /api/whoami`

**Description:** Get information about the authenticated user from JWT token claims.

**Headers:**

- `Authorization: Bearer <token>` (required)

**Responses:**

**200 OK** - Returns user information

```json
{
  "userId": "user@example.com",
  "claims": {
    "upn": "user@example.com",
    "name": "John Doe",
    "roles": ["user"]
  }
}

```

**401 Unauthorized** - Invalid or missing token

**Example:**

```bash
curl -X GET "https://your-api.azurewebsites.net/api/whoami" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

```

## Error Codes

| Code | Description | Common Causes |
|------|-------------|---------------|
| `400` | Bad Request | File too large for subscription tier |
| `401` | Unauthorized | Missing or invalid JWT token |
| `402` | Payment Required | Bandwidth limit exceeded |
| `403` | Forbidden | Upload limit reached, or not image owner |
| `404` | Not Found | Image not found |
| `507` | Insufficient Storage | Storage quota exceeded |

## 🚀 Quick Start

### Prerequisites


- .NET 10.0 SDK
- Azure Storage Account
- Azure Functions Core Tools v4

### Local Development


```bash
# Clone the repository
git clone https://github.com/utdcometsoccer/one-page-author-page-api.git
cd one-page-author-page-api/ImageAPI

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run locally (requires local.settings.json configuration)
func start

```

## ⚙️ Configuration

### 🔐 Security Requirements (.NET 10)

**CRITICAL**: This project uses sensitive credentials that must NOT be stored in source control.

### Development Setup (Required)

#### 1. Initialize User Secrets

```bash
cd ImageAPI
dotnet user-secrets init
```

#### 2. Add Required Configuration

```bash
# Azure Functions Core
dotnet user-secrets set "AzureWebJobsStorage" "your-storage-connection-string"
dotnet user-secrets set "FUNCTIONS_WORKER_RUNTIME" "dotnet-isolated"

# Azure Cosmos DB (Required)
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-cosmos-primary-key"
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthorDb"

# Azure Blob Storage (Required for Image Upload)
dotnet user-secrets set "AZURE_STORAGE_CONNECTION_STRING" "your-storage-connection-string"

# Azure AD Authentication (Optional for local testing)
dotnet user-secrets set "AAD_TENANT_ID" "your-tenant-id"
dotnet user-secrets set "AAD_AUDIENCE" "your-client-id"
dotnet user-secrets set "AAD_AUTHORITY" "https://login.microsoftonline.com/consumers/v2.0"
```

#### 3. Verify Configuration

```bash
dotnet user-secrets list
```

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `COSMOSDB_ENDPOINT_URI` | ✅ Yes | Azure Cosmos DB endpoint URL |
| `COSMOSDB_PRIMARY_KEY` | ✅ Yes | Cosmos DB primary access key |
| `COSMOSDB_DATABASE_ID` | ✅ Yes | Database name (typically "OnePageAuthorDb") |
| `AZURE_STORAGE_CONNECTION_STRING` | ✅ Yes | Azure Blob Storage connection string |
| `AAD_TENANT_ID` | ⚪ Optional | Azure AD tenant ID for authentication |
| `AAD_AUDIENCE` | ⚪ Optional | Azure AD client ID |
| `AAD_AUTHORITY` | ⚪ Optional | Azure AD authority URL |

### ⚠️ Migration from local.settings.json

If you have an existing `local.settings.json` file:

1. **DO NOT commit it to source control** - it contains sensitive data
2. Copy values to user secrets using the commands above
3. Delete or move the `local.settings.json` file
4. Add `local.settings.json` to your `.gitignore` if not already present

### Production Deployment

For Azure deployment, configure these values in:
- Azure Portal → Function App → Configuration → Application Settings

## 🧪 Testing

Run unit tests:

```bash
cd ../OnePageAuthor.Test
dotnet test --filter "FullyQualifiedName~ImageAPI"

```

## 📖 Documentation

- [Complete API Documentation](../API-Documentation.md)
- [System Architecture](../Complete-System-Documentation.md)
- [Authentication Guide](../README-Documentation.md)

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is part of the OnePageAuthor system. See the [main repository](../) for license information.

## 🔗 Related Projects

- [OnePageAuthorLib](../OnePageAuthorLib/) - Core business logic library
- [InkStainedWretchFunctions](../InkStainedWretchFunctions/) - Domain and external API functions
- [InkStainedWretchStripe](../InkStainedWretchStripe/) - Payment processing functions
- [function-app](../function-app/) - Main application functions
