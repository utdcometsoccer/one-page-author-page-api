# Azure Communication Services Deployment Guide

This guide explains how to deploy Azure Communication Services for the Author Invitation Tool email notifications.

## Overview

The Author Invitation Tool can optionally send email notifications to authors when they are invited. This requires Azure Communication Services (ACS) with Email service configured.

## Architecture

```
Author Invitation Tool
    ↓
Azure Communication Services
    ↓
Email Service
    ↓
Verified Domain
    ↓
Author's Email Inbox
```

## Deployment Options

### Option 1: Deploy with Bicep Template (Recommended)

The infrastructure includes an optional Azure Communication Services deployment via Bicep.

#### Deploy Communication Services Only

```bash
# Set variables
RESOURCE_GROUP="your-resource-group"
BASE_NAME="onepageauthor"
LOCATION="eastus"

# Deploy Communication Services
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infra/communication-services.bicep \
  --parameters baseName=$BASE_NAME \
               location=$LOCATION \
               dataLocation="United States"
```

#### Deploy as Part of Main Infrastructure

```bash
# Deploy with Communication Services enabled
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infra/inkstainedwretches.bicep \
  --parameters baseName=$BASE_NAME \
               location=$LOCATION \
               deployCommunicationServices=true \
               cosmosDbConnectionString="..." \
               stripeApiKey="..." \
               aadTenantId="..." \
               aadAudience="..."
```

### Option 2: Deploy via GitHub Actions

Update the GitHub Actions workflow to include Communication Services deployment.

Add the following secrets to your repository:

- `DEPLOY_COMMUNICATION_SERVICES` - Set to `"true"` to enable deployment

The workflow will automatically deploy Communication Services with the infrastructure.

## Post-Deployment Configuration

### 1. Retrieve Connection String

After deployment, retrieve the connection string:

```bash
# Get resource name
ACS_NAME="${BASE_NAME}-acs"

# Get connection string
az communication list-key \
  --name $ACS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query primaryConnectionString \
  --output tsv
```

### 2. Configure Domain (Choose One Option)

#### Option A: Use Azure Managed Domain (Quick Start)

The deployment creates a managed domain automatically (e.g., `<uniqueid>.azurecomm.net`).

1. Navigate to Azure Portal
2. Go to Communication Services → Email → Domains
3. Find the "AzureManagedDomain" 
4. Copy the "Mail From" address (e.g., `DoNotReply@<uniqueid>.azurecomm.net`)

**Pros:**
- Instant setup, no verification required
- Good for testing and development

**Cons:**
- Generic domain name
- May have lower deliverability than custom domain

#### Option B: Add Custom Domain (Production)

For production use, add your own verified domain:

1. Navigate to Azure Portal → Communication Services → Email
2. Click "Add Domain" → "Add Custom Domain"
3. Enter your domain (e.g., `onepageauthor.com`)
4. Add the required DNS records to your domain registrar:
   - TXT record for verification
   - SPF record for sender authentication
   - DKIM records for email signing
5. Click "Verify" once DNS records are propagated (may take 24-48 hours)
6. Configure sender address (e.g., `DoNotReply@onepageauthor.com`)

**Pros:**
- Professional appearance
- Better email deliverability
- Full control over branding

**Cons:**
- Requires domain ownership
- DNS configuration needed
- Takes time for verification

### 3. Configure Author Invitation Tool

Update the tool configuration with your Communication Services details:

```bash
# Using user secrets (recommended)
cd AuthorInvitationTool
dotnet user-secrets set "Email:AzureCommunicationServices:ConnectionString" "endpoint=https://...;accesskey=..."
dotnet user-secrets set "Email:AzureCommunicationServices:SenderAddress" "DoNotReply@yourdomain.com"

# Or using environment variables
export ACS_CONNECTION_STRING="endpoint=https://...;accesskey=..."
export ACS_SENDER_ADDRESS="DoNotReply@yourdomain.com"
```

## Testing Email Delivery

### Test the Email Service

```bash
# Test with a valid email
cd AuthorInvitationTool
dotnet run -- test@example.com testdomain.com "Test invitation"
```

Expected output:
```
✅ Invitation created successfully!
✅ Invitation email sent successfully!
```

### Verify Email Receipt

1. Check the recipient's inbox
2. Look in spam/junk folder if not in inbox
3. Check Azure Portal → Communication Services → Email → Logs for delivery status

## Troubleshooting

### Issue: Email Not Sending

**Check 1: Connection String**
```bash
# Verify connection string is configured
dotnet user-secrets list
```

**Check 2: Domain Status**
```bash
# Check if domain is verified (for custom domains)
az communication email domain show \
  --name $ACS_NAME \
  --resource-group $RESOURCE_GROUP \
  --domain-name "yourdomain.com"
```

**Check 3: Sender Address**
- Ensure sender address matches the verified domain
- Format: `DoNotReply@verifieddomain.com`

### Issue: Emails Going to Spam

**Solution 1: Configure SPF Record**
```
v=spf1 include:_spf.azurecomm.net ~all
```

**Solution 2: Configure DKIM**
- Azure Portal provides DKIM keys after domain verification
- Add provided CNAME records to your DNS

**Solution 3: Configure DMARC**
```
v=DMARC1; p=quarantine; rua=mailto:dmarc@yourdomain.com
```

### Issue: Domain Verification Failing

**Check DNS Propagation:**
```bash
# Check TXT record
nslookup -type=TXT _azurecomm-verification.yourdomain.com

# Check with Google DNS
nslookup -type=TXT _azurecomm-verification.yourdomain.com 8.8.8.8
```

**Common Causes:**
- DNS records not propagated (wait 24-48 hours)
- Incorrect TXT record value
- DNS caching (clear cache or wait)

## Monitoring and Logs

### View Email Logs in Azure Portal

1. Navigate to Communication Services → Email
2. Click on "Insights" or "Logs"
3. View email delivery status, opens, clicks, bounces

### Query Logs with KQL

```kql
ACSEmailSendOperational
| where TimeGenerated > ago(24h)
| where ResultType == "Succeeded"
| project TimeGenerated, RecipientAddress, Subject, MessageId
| order by TimeGenerated desc
```

## Cost Considerations

### Azure Communication Services Pricing (as of 2024)

- **Email Service**: 
  - First 500 emails/month: Free
  - Additional emails: $0.0025 per email
  
- **Azure Managed Domain**: Free
- **Custom Domain**: Free (requires domain registration separately)

### Cost Optimization Tips

1. Use Azure Managed Domain for development/testing
2. Batch invitations to reduce API calls
3. Monitor usage through Azure Cost Management
4. Set up budget alerts for email service

## Security Best Practices

1. **Store Connection Strings Securely**
   - Use Azure Key Vault in production
   - Use user secrets for development
   - Never commit connection strings to source control

2. **Limit Access**
   - Use Azure RBAC to control who can send emails
   - Rotate access keys periodically
   - Use managed identities when possible

3. **Monitor Usage**
   - Set up alerts for unusual email volumes
   - Review email logs regularly
   - Implement rate limiting in application

4. **Validate Recipients**
   - Verify email format before sending
   - Implement unsubscribe mechanism
   - Honor bounce notifications

## GitHub Actions Integration

### Add to Workflow

The workflow in `.github/workflows/main_onepageauthorapi.yml` now supports Communication Services deployment.

Add these secrets to your GitHub repository:

```
ISW_DEPLOY_COMMUNICATION_SERVICES=true
```

The deployment will:
1. Create Communication Services resource
2. Create Email Service
3. Create Azure Managed Domain
4. Configure sender username

### Manual Secret Configuration

After deployment, manually add the connection string to GitHub Secrets:

1. Get connection string from Azure Portal
2. Add as GitHub secret: `ACS_CONNECTION_STRING`
3. Update workflow to pass to Function Apps if needed

## Production Checklist

Before going to production:

- [ ] Custom domain verified
- [ ] SPF record configured
- [ ] DKIM records configured
- [ ] DMARC policy set
- [ ] Connection string stored in Key Vault
- [ ] Email templates reviewed
- [ ] Test emails sent and received
- [ ] Spam score checked (use mail-tester.com)
- [ ] Monitoring and alerts configured
- [ ] Budget alerts set up
- [ ] Backup sender address configured

## Additional Resources

- [Azure Communication Services Documentation](https://learn.microsoft.com/en-us/azure/communication-services/)
- [Email Service Overview](https://learn.microsoft.com/en-us/azure/communication-services/concepts/email/email-overview)
- [Domain Verification Guide](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/email/add-custom-verified-domains)
- [SPF and DKIM Setup](https://learn.microsoft.com/en-us/azure/communication-services/concepts/email/email-authentication-best-practice)
- [Email Best Practices](https://learn.microsoft.com/en-us/azure/communication-services/concepts/email/best-practices)

## Support

For issues with Azure Communication Services:
- Azure Portal → Support → New Support Request
- [Azure Communication Services GitHub](https://github.com/Azure/communication)
- [Microsoft Q&A](https://learn.microsoft.com/en-us/answers/topics/azure-communication-services.html)
