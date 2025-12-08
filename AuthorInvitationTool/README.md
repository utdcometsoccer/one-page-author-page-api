# Author Invitation Tool

A command-line tool to invite authors with existing websites to create Microsoft accounts linked to their domains for the One Page Author platform.

## Overview

This tool allows administrators to send invitation emails to authors, creating records in the Cosmos DB database and optionally sending email notifications via Azure Communication Services. When an author accepts the invitation, they can create a Microsoft account that will be linked to their specified domain.

## Features

- ✅ Create author invitations with email and domain information
- ✅ Store invitations in Cosmos DB with automatic expiration (30 days)
- ✅ Send invitation emails via Azure Communication Services (optional)
- ✅ Validate email and domain formats
- ✅ Check for duplicate invitations
- ✅ Support for optional notes/comments on invitations
- ✅ Comprehensive logging and error handling

## Prerequisites

- .NET 10.0 SDK or later
- Azure Cosmos DB account with connection credentials
- Azure Communication Services (optional, for sending emails)
- Access to the One Page Author platform configuration

## Installation

1. Clone the repository:
```bash
git clone https://github.com/utdcometsoccer/one-page-author-page-api.git
cd one-page-author-page-api/AuthorInvitationTool
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Build the project:
```bash
dotnet build
```

## Configuration

### Required Configuration

You must configure the Cosmos DB connection. You can use any of these methods:

#### Option 1: User Secrets (Recommended for Development)
```bash
dotnet user-secrets init
dotnet user-secrets set "CosmosDb:EndpointUri" "https://your-cosmos-account.documents.azure.com:443/"
dotnet user-secrets set "CosmosDb:PrimaryKey" "your-primary-key-here"
dotnet user-secrets set "CosmosDb:DatabaseId" "OnePageAuthor"
```

#### Option 2: Environment Variables
```bash
export COSMOSDB_ENDPOINT_URI="https://your-cosmos-account.documents.azure.com:443/"
export COSMOSDB_PRIMARY_KEY="your-primary-key-here"
export COSMOSDB_DATABASE_ID="OnePageAuthor"
```

#### Option 3: appsettings.json
Edit `appsettings.json` and add your configuration:
```json
{
  "CosmosDb": {
    "EndpointUri": "https://your-cosmos-account.documents.azure.com:443/",
    "PrimaryKey": "your-primary-key-here",
    "DatabaseId": "OnePageAuthor"
  }
}
```

**⚠️ Warning:** Never commit secrets to source control. Use user secrets or environment variables for production.

### Optional: Email Service Configuration

To enable email notifications, configure Azure Communication Services:

```bash
# Using user secrets
dotnet user-secrets set "Email:AzureCommunicationServices:ConnectionString" "endpoint=https://...;accesskey=..."
dotnet user-secrets set "Email:AzureCommunicationServices:SenderAddress" "DoNotReply@yourdomain.com"

# Or using environment variables
export ACS_CONNECTION_STRING="endpoint=https://...;accesskey=..."
export ACS_SENDER_ADDRESS="DoNotReply@yourdomain.com"
```

If email configuration is not provided, the tool will still create invitations in the database but skip sending emails.

## Usage

### Basic Usage

```bash
dotnet run -- <email> <domain>
```

### With Optional Notes

```bash
dotnet run -- <email> <domain> "Optional notes about the invitation"
```

### Examples

```bash
# Simple invitation
dotnet run -- author@example.com example.com

# With notes
dotnet run -- john.doe@writersworld.com writersworld.com "Invitation for established author"

# Multiple invitations
dotnet run -- jane.smith@authorsplace.com authorsplace.com
dotnet run -- mike.jones@novelsite.com novelsite.com "Premium author invitation"
```

### Built Executable Usage

After building, you can run the executable directly:

```bash
# From the output directory
./bin/Debug/net10.0/AuthorInvitationTool author@example.com example.com

# Or from anywhere after publishing
dotnet publish -c Release
./bin/Release/net10.0/publish/AuthorInvitationTool author@example.com example.com
```

## Command-Line Arguments

| Argument | Required | Description |
|----------|----------|-------------|
| `email` | Yes | The email address of the author to invite |
| `domain` | Yes | The domain name to link (e.g., example.com) |
| `notes` | No | Optional notes about the invitation |

## Output

The tool provides detailed console output showing:

- Input validation results
- Configuration status
- Invitation creation progress
- Email sending status (if configured)
- Summary of created invitation

### Example Output

```
═══════════════════════════════════════════════════════════
   Author Invitation Tool - One Page Author Platform
═══════════════════════════════════════════════════════════

Creating invitation...
✅ Invitation created successfully!
   Invitation ID: abc123def-456-789-ghi
   Email: author@example.com
   Domain: example.com
   Status: Pending
   Created: 2024-12-08 21:00:00 UTC
   Expires: 2025-01-07 21:00:00 UTC

Sending invitation email...
✅ Invitation email sent successfully!

═══════════════════════════════════════════════════════════
✅ Operation completed successfully!
═══════════════════════════════════════════════════════════
```

## Error Handling

The tool includes comprehensive error handling:

- ✅ **Invalid email format**: Validates email addresses before processing
- ✅ **Invalid domain format**: Validates domain names
- ✅ **Missing configuration**: Clear error messages for missing required settings
- ✅ **Duplicate invitations**: Warns if an invitation already exists for the email
- ✅ **Database errors**: Detailed error messages for Cosmos DB issues
- ✅ **Email failures**: Continues with invitation creation even if email fails

## Database Schema

### AuthorInvitation Entity

The tool creates records with the following structure:

```csharp
{
  "id": "unique-guid",
  "EmailAddress": "author@example.com",  // Partition key
  "DomainName": "example.com",
  "CreatedAt": "2024-12-08T21:00:00Z",
  "Status": "Pending",
  "ExpiresAt": "2025-01-07T21:00:00Z",
  "AcceptedAt": null,
  "UserOid": null,
  "Notes": "Optional notes"
}
```

### Status Values

- **Pending**: Invitation sent, awaiting acceptance
- **Accepted**: Author has accepted and created account
- **Expired**: Invitation has passed expiration date
- **Revoked**: Invitation manually revoked by administrator

## Integration with Azure Communication Services

When properly configured, the tool sends professionally formatted HTML emails with:

- Clear invitation message
- Domain information
- Invitation ID for reference
- Call-to-action button
- 30-day expiration notice
- Plain text fallback for email clients that don't support HTML

### Email Template Preview

The email includes:
- Header with platform branding
- Welcome message
- Domain being linked
- Accept invitation button linking to Microsoft account signup
- Invitation ID for tracking
- Expiration reminder
- Footer with copyright information

## Security Considerations

1. **Never commit secrets**: Use user secrets or environment variables
2. **Validate inputs**: The tool validates all email and domain inputs
3. **Secure storage**: All data is stored in Azure Cosmos DB with encryption
4. **Expiration**: Invitations automatically expire after 30 days
5. **Email verification**: Azure Communication Services provides secure email delivery

## Troubleshooting

### Common Issues

**Problem**: "COSMOSDB_ENDPOINT_URI is required"
- **Solution**: Configure Cosmos DB credentials using one of the methods above

**Problem**: "Invalid email address format"
- **Solution**: Ensure email follows standard format (user@domain.com)

**Problem**: "An invitation already exists for this email"
- **Solution**: The tool will prompt you to confirm creating a duplicate invitation

**Problem**: Email not being sent
- **Solution**: Verify Azure Communication Services configuration. The invitation will still be created even if email fails.

### Debug Mode

For detailed logging, set the logging level to Debug in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

## Development

### Project Structure

```
AuthorInvitationTool/
├── Program.cs              # Main CLI logic
├── AuthorInvitationTool.csproj
├── appsettings.json        # Configuration template
└── README.md               # This file
```

### Dependencies

- **Microsoft.Extensions.Hosting**: Host builder and DI
- **Microsoft.Extensions.Configuration**: Configuration management
- **Microsoft.Extensions.Logging**: Logging infrastructure
- **OnePageAuthorLib**: Core library with entities, repositories, and services

### Related Projects

- **OnePageAuthorLib**: Core shared library
  - `entities/AuthorInvitation.cs`: Entity definition
  - `interfaces/IAuthorInvitationRepository.cs`: Repository interface
  - `interfaces/IEmailService.cs`: Email service interface
  - `nosql/AuthorInvitationRepository.cs`: Cosmos DB implementation
  - `services/AzureCommunicationEmailService.cs`: Email service implementation

## Future Enhancements

Potential improvements for future versions:

- [ ] Bulk invitation support from CSV file
- [ ] Interactive mode with prompts
- [ ] List existing invitations command
- [ ] Revoke invitation command
- [ ] Resend invitation email command
- [ ] Custom email template support
- [ ] Invitation expiration customization
- [ ] Rich email formatting with custom branding

## Support

For issues, questions, or contributions:

- **GitHub Issues**: [Report an issue](https://github.com/utdcometsoccer/one-page-author-page-api/issues)
- **Documentation**: See main repository README
- **Security**: See SECURITY.md for vulnerability reporting

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

Part of the One Page Author API Platform, a comprehensive .NET 10 solution for author management and content publishing.
