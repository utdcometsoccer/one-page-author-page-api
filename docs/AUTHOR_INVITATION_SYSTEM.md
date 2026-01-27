# Author Invitation System - Complete Documentation

This document provides comprehensive documentation for the Author Invitation System, a command-line tool and infrastructure for inviting authors with existing websites to create Microsoft accounts linked to their domains.

## Table of Contents

1. [System Overview](#system-overview)
2. [Architecture](#architecture)
3. [Components](#components)
4. [Deployment Guide](#deployment-guide)
5. [Usage Guide](#usage-guide)
6. [Configuration](#configuration)
7. [Email Service Setup](#email-service-setup)
8. [Testing](#testing)
9. [Troubleshooting](#troubleshooting)
10. [API Reference](#api-reference)

## System Overview

The Author Invitation System enables administrators to invite authors with existing websites to join the One Page Author platform. The system:

1. **Creates invitation records** in Cosmos DB with email, domain, and metadata
2. **Sends email notifications** to authors via Azure Communication Services (optional)
3. **Tracks invitation status** (Pending, Accepted, Expired, Revoked)
4. **Links domains to Microsoft accounts** when authors accept invitations

### Key Features

- ✅ Command-line interface for easy invitation management
- ✅ Cosmos DB storage with automatic expiration (30 days)
- ✅ Optional email notifications via Azure Communication Services
- ✅ Input validation (email format, domain format)
- ✅ Duplicate invitation detection
- ✅ Comprehensive logging and error handling
- ✅ Infrastructure as Code with Bicep templates
- ✅ CI/CD integration with GitHub Actions

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   Administrator                              │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│           AuthorInvitationTool (CLI)                         │
│  • Validates input                                           │
│  • Creates invitation record                                 │
│  • Triggers email notification                               │
└───────┬────────────────────────┬────────────────────────────┘
        │                        │
        ▼                        ▼
┌──────────────────┐    ┌────────────────────────────┐
│   Cosmos DB      │    │ Azure Communication        │
│  (AuthorInvite)  │    │ Services (Email)           │
└──────────────────┘    └─────────────┬──────────────┘
                                      │
                                      ▼
                        ┌──────────────────────────┐
                        │    Author's Email        │
                        │    (Invitation Notice)   │
                        └──────────────────────────┘
                                      │
                                      ▼
                        ┌──────────────────────────┐
                        │  Microsoft Account       │
                        │  Sign-up / Link Domain   │
                        └──────────────────────────┘
```

## Components

### 1. Core Entities

#### AuthorInvitation Entity

Located: `OnePageAuthorLib/entities/AuthorInvitation.cs`

```csharp
public class AuthorInvitation
{
    public string id { get; set; }           // Unique GUID
    public string EmailAddress { get; set; } // Partition key
    public string DomainName { get; set; }   // Domain to link
    public DateTime CreatedAt { get; set; }  // UTC timestamp
    public string Status { get; set; }       // Pending/Accepted/Expired
    public DateTime? AcceptedAt { get; set; }
    public string? UserOid { get; set; }     // User ID after acceptance
    public DateTime ExpiresAt { get; set; }  // 30 days from creation
    public string? Notes { get; set; }       // Optional notes
}
```

### 2. Repository Layer

#### IAuthorInvitationRepository

Located: `OnePageAuthorLib/interfaces/IAuthorInvitationRepository.cs`

Methods:

- `GetByIdAsync(string id)` - Get invitation by ID
- `GetByEmailAsync(string emailAddress)` - Get invitation by email
- `GetByDomainAsync(string domainName)` - Get invitations for domain
- `GetPendingInvitationsAsync()` - Get all pending invitations
- `AddAsync(AuthorInvitation)` - Create new invitation
- `UpdateAsync(AuthorInvitation)` - Update existing invitation
- `DeleteAsync(string id)` - Delete invitation

#### AuthorInvitationRepository

Located: `OnePageAuthorLib/nosql/AuthorInvitationRepository.cs`

Cosmos DB implementation using:

- Container: `AuthorInvitations`
- Partition Key: `/EmailAddress`

### 3. Email Service

#### IEmailService

Located: `OnePageAuthorLib/interfaces/IEmailService.cs`

```csharp
public interface IEmailService
{
    Task<bool> SendInvitationEmailAsync(
        string toEmail, 
        string domainName, 
        string invitationId);
}
```

#### AzureCommunicationEmailService

Located: `OnePageAuthorLib/services/AzureCommunicationEmailService.cs`

Features:

- HTML and plain text email templates
- Professional formatting with branding
- Error handling and logging
- Configurable sender address

### 4. Command-Line Tool

#### AuthorInvitationTool

Located: `AuthorInvitationTool/`

Features:

- Command-line argument parsing
- Configuration via appsettings.json, user secrets, or environment variables
- Input validation
- Duplicate detection with confirmation prompt
- Comprehensive console output
- Detailed error messages

### 5. Infrastructure

#### Bicep Templates

**communication-services.bicep**

- Azure Communication Services resource
- Email Service with Azure Managed Domain
- Sender username configuration
- Outputs for connection string retrieval

**inkstainedwretches.bicep** (updated)

- Optional Communication Services deployment
- Integration with existing infrastructure
- Conditional deployment based on parameter

## Deployment Guide

### Prerequisites

- Azure subscription with appropriate permissions
- Azure CLI installed and configured
- .NET 10.0 SDK installed
- GitHub repository with Actions enabled (for CI/CD)

### Step 1: Deploy Infrastructure

#### Option A: Deploy via Azure CLI

```bash
# Set variables
RESOURCE_GROUP="onepageauthor-rg"
LOCATION="eastus"
BASE_NAME="onepageauthor"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Deploy infrastructure with Communication Services
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infra/inkstainedwretches.bicep \
  --parameters baseName=$BASE_NAME \
               location=$LOCATION \
               deployCommunicationServices=true \
               cosmosDbConnectionString="<your-cosmos-connection-string>" \
               stripeApiKey="<your-stripe-key>" \
               aadTenantId="<your-tenant-id>" \
               aadAudience="<your-client-id>"
```

#### Option B: Deploy via GitHub Actions

1. Add required secrets to your GitHub repository:
   - `ISW_RESOURCE_GROUP`
   - `ISW_LOCATION`
   - `ISW_BASE_NAME`
   - `COSMOSDB_CONNECTION_STRING`
   - `STRIPE_API_KEY`
   - `AAD_TENANT_ID`
   - `AAD_AUDIENCE`
   - `DEPLOY_COMMUNICATION_SERVICES` (set to `"true"`)

2. Push to main branch or trigger workflow manually

### Step 2: Configure Communication Services

After deployment, configure the email service:

```bash
# Get Communication Services connection string
ACS_NAME="${BASE_NAME}-acs"
az communication list-key \
  --name $ACS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query primaryConnectionString \
  --output tsv
```

Save this connection string for later use.

### Step 3: Build and Install CLI Tool

```bash
# Clone repository
git clone https://github.com/utdcometsoccer/one-page-author-page-api.git
cd one-page-author-page-api/AuthorInvitationTool

# Build the project
dotnet build -c Release

# Optional: Publish for distribution
dotnet publish -c Release -o ./publish
```

### Step 4: Configure CLI Tool

```bash
# Configure using user secrets (recommended for development)
dotnet user-secrets init
dotnet user-secrets set "CosmosDb:EndpointUri" "https://your-cosmos.documents.azure.com:443/"
dotnet user-secrets set "CosmosDb:PrimaryKey" "your-primary-key"
dotnet user-secrets set "CosmosDb:DatabaseId" "OnePageAuthor"
dotnet user-secrets set "Email:AzureCommunicationServices:ConnectionString" "endpoint=https://...;accesskey=..."
dotnet user-secrets set "Email:AzureCommunicationServices:SenderAddress" "DoNotReply@yourcommdomain.azurecomm.net"
```

## Usage Guide

### Basic Invitation

```bash
# Invite an author
dotnet run -- author@example.com example.com

# With notes
dotnet run -- author@example.com example.com "Premium author invitation"
```

### Batch Invitations

```bash
# Create a script for multiple invitations
#!/bin/bash
authors=(
  "john@authorsworld.com authorsworld.com"
  "jane@writersplace.com writersplace.com"
  "mike@novelsite.com novelsite.com"
)

for author in "${authors[@]}"; do
  dotnet run -- $author
  sleep 1
done
```

### Using Published Executable

```bash
# After publishing
cd publish
./AuthorInvitationTool author@example.com example.com
```

## Configuration

### Configuration Priority

1. Command-line arguments (email, domain)
2. User secrets (development)
3. Environment variables (production)
4. appsettings.json (defaults)

### Required Settings

| Setting | Description | Example |
|---------|-------------|---------|
| `CosmosDb:EndpointUri` | Cosmos DB endpoint | `https://account.documents.azure.com:443/` |
| `CosmosDb:PrimaryKey` | Cosmos DB key | `your-primary-key==` |
| `CosmosDb:DatabaseId` | Database name | `OnePageAuthor` |

### Optional Settings

| Setting | Description | Example |
|---------|-------------|---------|
| `Email:AzureCommunicationServices:ConnectionString` | ACS connection | `endpoint=https://...` |
| `Email:AzureCommunicationServices:SenderAddress` | From email | `DoNotReply@domain.com` |

### Environment Variables

```bash
# Required
export COSMOSDB_ENDPOINT_URI="https://..."
export COSMOSDB_PRIMARY_KEY="..."
export COSMOSDB_DATABASE_ID="OnePageAuthor"

# Optional (for email)
export ACS_CONNECTION_STRING="endpoint=https://..."
export ACS_SENDER_ADDRESS="DoNotReply@domain.com"
```

## Email Service Setup

For detailed email service configuration, see:

- [Azure Communication Services Setup Guide](./AZURE_COMMUNICATION_SERVICES_SETUP.md)

Quick steps:

1. Deploy Communication Services (done via Bicep)
2. Retrieve connection string from Azure Portal
3. Configure domain (Azure Managed Domain or custom)
4. Add sender address
5. Test email delivery

## Testing

### Unit Testing

The project structure supports adding unit tests:

```bash
# Location for tests
OnePageAuthor.Test/Services/EmailServiceTests.cs
OnePageAuthor.Test/Repositories/AuthorInvitationRepositoryTests.cs
```

Example test structure:

```csharp
[Fact]
public async Task AddAsync_ShouldCreateInvitation()
{
    // Arrange
    var invitation = new AuthorInvitation("test@example.com", "example.com");
    
    // Act
    var result = await _repository.AddAsync(invitation);
    
    // Assert
    Assert.NotNull(result.id);
    Assert.Equal("Pending", result.Status);
}
```

### Integration Testing

```bash
# Test with real services (requires configuration)
cd AuthorInvitationTool

# Test invitation creation (no email)
dotnet run -- test@example.com testdomain.com "Test invitation"

# Test with email sending (requires ACS configuration)
dotnet user-secrets set "Email:AzureCommunicationServices:ConnectionString" "..."
dotnet run -- test@example.com testdomain.com
```

### End-to-End Testing

1. Run CLI tool
2. Verify invitation in Cosmos DB
3. Check email received
4. Verify invitation status updates

## Troubleshooting

### Common Issues

#### Issue: "COSMOSDB_ENDPOINT_URI is required"

**Cause**: Missing Cosmos DB configuration
**Solution**: Configure via user secrets or environment variables

#### Issue: "Invalid email address format"

**Cause**: Malformed email address
**Solution**: Verify email format (<user@domain.com>)

#### Issue: Email not received

**Cause**: ACS not configured or domain not verified
**Solution**:

1. Check ACS configuration
2. Verify domain in Azure Portal
3. Check spam folder
4. Review email service logs

#### Issue: "An invitation already exists"

**Cause**: Duplicate invitation for same email
**Solution**: Choose to create duplicate or update existing

### Debug Mode

Enable detailed logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  }
}
```

### Logs Location

- CLI output: Console
- Azure Portal: Communication Services → Insights
- Cosmos DB: Data Explorer

## API Reference

### AuthorInvitationRepository

```csharp
// Get invitation by email
var invitation = await repository.GetByEmailAsync("author@example.com");

// Create invitation
var newInvitation = new AuthorInvitation(
    emailAddress: "author@example.com",
    domainName: "example.com",
    notes: "Optional notes"
);
await repository.AddAsync(newInvitation);

// Update invitation
invitation.Status = "Accepted";
invitation.AcceptedAt = DateTime.UtcNow;
invitation.UserOid = "user-oid-from-jwt";
await repository.UpdateAsync(invitation);

// Get pending invitations
var pending = await repository.GetPendingInvitationsAsync();
```

### EmailService

```csharp
// Send invitation email
var success = await emailService.SendInvitationEmailAsync(
    toEmail: "author@example.com",
    domainName: "example.com",
    invitationId: "invitation-guid"
);
```

### ServiceFactory Extensions

```csharp
// Configure services
services
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId)
    .AddAuthorInvitationRepository()
    .AddEmailService(connectionString, senderAddress);
```

## Security Considerations

1. **Never commit secrets** - Use Key Vault, user secrets, or environment variables
2. **Validate all inputs** - Email and domain format validation included
3. **Implement rate limiting** - Prevent abuse of invitation system
4. **Monitor invitation volume** - Set up alerts for unusual patterns
5. **Rotate access keys** - Regular key rotation for Cosmos DB and ACS
6. **Use HTTPS** - All communication encrypted in transit
7. **Implement expiration** - Automatic 30-day expiration on invitations

## Production Checklist

Before deploying to production:

- [ ] Infrastructure deployed via Bicep
- [ ] Communication Services configured with custom domain
- [ ] SPF, DKIM, DMARC records configured
- [ ] Connection strings stored in Key Vault
- [ ] CLI tool tested end-to-end
- [ ] Email templates reviewed and approved
- [ ] Monitoring and alerts configured
- [ ] Budget alerts set up for ACS
- [ ] Documentation reviewed
- [ ] Backup and disaster recovery plan in place

## Support and Contributing

### Getting Help

- **GitHub Issues**: [Report bugs or request features](https://github.com/utdcometsoccer/one-page-author-page-api/issues)
- **Documentation**: See README and related docs
- **Security**: See SECURITY.md for vulnerability reporting

### Contributing

See CONTRIBUTING.md for guidelines on:

- Code style and conventions
- Pull request process
- Testing requirements
- Documentation standards

## Related Documentation

- [AuthorInvitationTool README](../AuthorInvitationTool/README.md) - CLI tool usage
- [Azure Communication Services Setup](./AZURE_COMMUNICATION_SERVICES_SETUP.md) - Email service configuration
- [Main README](../README.md) - Platform overview
- [API Documentation](./API-Documentation.md) - Complete API reference

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Changelog

### Version 1.0.0 (2024-12-08)

**Added:**

- AuthorInvitation entity with full CRUD operations
- Command-line tool for sending invitations
- Azure Communication Services integration for email
- Bicep templates for infrastructure deployment
- Comprehensive documentation
- GitHub Actions workflow integration

**Features:**

- Cosmos DB storage with 30-day expiration
- Email notifications with HTML templates
- Input validation (email, domain)
- Duplicate invitation detection
- Configurable via multiple sources
- Complete error handling and logging
