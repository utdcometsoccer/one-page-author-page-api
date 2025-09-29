# ImageAPI

[![Build Status](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml/badge.svg)](https://github.com/utdcometsoccer/one-page-author-page-api/actions/workflows/main_onepageauthorapi.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-orange.svg)](https://docs.microsoft.com/en-us/azure/azure-functions/)

Azure Functions app for image management operations including upload, retrieval, and deletion with subscription tier validation.

## 🚀 Overview

The ImageAPI provides a comprehensive image management system for the OnePageAuthor platform. It handles user image uploads to Azure Blob Storage with subscription tier-based validation, retrieval of user images, and secure deletion operations.

## 🏗️ Architecture

- **Runtime**: Azure Functions v4 (.NET 9)
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

| Method | Endpoint | Description | Authentication |
|--------|----------|-------------|----------------|
| `POST` | `/api/images/upload` | Upload image file | Required |
| `GET` | `/api/images/user` | Get user's images | Required |
| `DELETE` | `/api/images/{imageId}` | Delete image | Required |
| `GET` | `/api/whoami` | Get user info | Required |

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
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

### Configuration
Create a `local.settings.json` file:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "your-storage-connection-string",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CosmosDbConnectionString": "your-cosmos-connection-string",
    "BlobStorageConnectionString": "your-blob-storage-connection-string"
  }
}
```

## 🧪 Testing

Run unit tests:
```bash
cd ../OnePageAuthor.Test
dotnet test --filter "Category=ImageAPI"
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