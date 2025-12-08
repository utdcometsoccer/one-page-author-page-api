# Quick Start Guide - Author Invitation Tool

This guide will help you get started with the Author Invitation Tool in under 10 minutes.

## Prerequisites

✅ .NET 10.0 SDK installed  
✅ Azure subscription with Cosmos DB  
✅ Basic command-line knowledge

## Step 1: Get Your Configuration Values (2 minutes)

You need these values from your Azure resources:

### Required: Cosmos DB
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Cosmos DB account
3. Click **Keys** in the left menu
4. Copy these values:
   - **URI** (e.g., `https://your-account.documents.azure.com:443/`)
   - **Primary Key** (long string ending with `==`)
   - **Database ID** (typically `OnePageAuthor`)

### Optional: Email Service
If you want to send emails:
1. Navigate to **Communication Services** in Azure Portal
2. Click **Keys** in the left menu
3. Copy **Connection string**
4. Note the sender address from Email → Domains (e.g., `DoNotReply@xyz.azurecomm.net`)

## Step 2: Configure the Tool (2 minutes)

Choose one method:

### Option A: User Secrets (Recommended)
```bash
cd AuthorInvitationTool

# Initialize user secrets
dotnet user-secrets init

# Add required Cosmos DB settings
dotnet user-secrets set "CosmosDb:EndpointUri" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "CosmosDb:PrimaryKey" "your-primary-key-here=="
dotnet user-secrets set "CosmosDb:DatabaseId" "OnePageAuthor"

# Optional: Add email settings
dotnet user-secrets set "Email:AzureCommunicationServices:ConnectionString" "endpoint=https://...;accesskey=..."
dotnet user-secrets set "Email:AzureCommunicationServices:SenderAddress" "DoNotReply@yourcommdomain.azurecomm.net"
```

### Option B: Environment Variables
```bash
export COSMOSDB_ENDPOINT_URI="https://your-account.documents.azure.com:443/"
export COSMOSDB_PRIMARY_KEY="your-primary-key-here=="
export COSMOSDB_DATABASE_ID="OnePageAuthor"

# Optional: Email
export ACS_CONNECTION_STRING="endpoint=https://...;accesskey=..."
export ACS_SENDER_ADDRESS="DoNotReply@yourcommdomain.azurecomm.net"
```

## Step 3: Build the Tool (1 minute)

```bash
cd AuthorInvitationTool
dotnet build
```

Expected output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Step 4: Send Your First Invitation (1 minute)

```bash
dotnet run -- author@example.com example.com
```

Expected output:
```
═══════════════════════════════════════════════════════════
   Author Invitation Tool - One Page Author Platform
═══════════════════════════════════════════════════════════

Creating invitation...
✅ Invitation created successfully!
   Invitation ID: abc123-def456-...
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

## Step 5: Verify in Azure Portal (2 minutes)

1. Go to Azure Portal → Cosmos DB → Data Explorer
2. Open `OnePageAuthor` database
3. Open `AuthorInvitations` container
4. You should see your invitation record

## Common Commands

```bash
# Basic invitation
dotnet run -- email@domain.com domain.com

# With notes
dotnet run -- email@domain.com domain.com "VIP author invitation"

# Multiple invitations
dotnet run -- john@example.com example.com
dotnet run -- jane@another.com another.com
dotnet run -- mike@thirdsite.com thirdsite.com
```

## What Happens Next?

1. **Invitation Created**: Record stored in Cosmos DB
2. **Email Sent** (if configured): Author receives email with invitation
3. **Author Accepts**: Author creates Microsoft account linked to domain
4. **Status Updated**: System updates invitation status to "Accepted"

## Troubleshooting

### "COSMOSDB_ENDPOINT_URI is required"
→ You forgot to configure Cosmos DB. Go back to Step 2.

### "Invalid email address format"
→ Check email format. Must be: `user@domain.com`

### Email not sent
→ This is OK! The invitation is still created in the database.
   To enable emails, configure Azure Communication Services.

### "An invitation already exists"
→ An invitation was already sent to this email.
   Type `y` to create a new one anyway.

## Next Steps

Want to do more?

- **Deploy Infrastructure**: See [Azure Communication Services Setup](./AZURE_COMMUNICATION_SERVICES_SETUP.md)
- **Customize Emails**: Edit `AzureCommunicationEmailService.cs`
- **Batch Invitations**: Create a script with multiple invitations
- **API Integration**: Use the repository directly in your API
- **Complete Documentation**: Read [Author Invitation System](./AUTHOR_INVITATION_SYSTEM.md)

## Getting Help

- **Tool Usage**: `dotnet run -- --help`
- **CLI README**: [AuthorInvitationTool/README.md](../AuthorInvitationTool/README.md)
- **GitHub Issues**: [Report a problem](https://github.com/utdcometsoccer/one-page-author-page-api/issues)

## Quick Reference Card

```
┌─────────────────────────────────────────────────────────┐
│              AUTHOR INVITATION TOOL                     │
├─────────────────────────────────────────────────────────┤
│ Usage:                                                  │
│   dotnet run -- <email> <domain> [notes]               │
│                                                         │
│ Examples:                                               │
│   dotnet run -- user@site.com site.com                 │
│   dotnet run -- user@site.com site.com "VIP author"    │
│                                                         │
│ Configuration (User Secrets):                           │
│   CosmosDb:EndpointUri    - Cosmos DB URL              │
│   CosmosDb:PrimaryKey     - Cosmos DB key              │
│   Email:...:ConnectionString - ACS connection          │
│   Email:...:SenderAddress - From email                 │
│                                                         │
│ Status Values:                                          │
│   Pending  - Awaiting acceptance                       │
│   Accepted - Author accepted                           │
│   Expired  - Past 30 days                              │
│   Revoked  - Manually cancelled                        │
└─────────────────────────────────────────────────────────┘
```

## Success Checklist

After following this guide, you should be able to:

- ✅ Configure the tool with your Azure credentials
- ✅ Send an invitation to an author
- ✅ See the invitation in Cosmos DB
- ✅ Understand the invitation workflow
- ✅ Troubleshoot common issues

🎉 **Congratulations!** You've successfully set up and used the Author Invitation Tool.

---

**Pro Tip**: Create a shell alias for faster invitations:
```bash
alias invite='dotnet run --project /path/to/AuthorInvitationTool/AuthorInvitationTool.csproj --'
# Then use: invite author@example.com example.com
```
