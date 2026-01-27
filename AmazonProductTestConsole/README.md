# Amazon Product API Test Console

A .NET console application for testing and debugging the Amazon Product Advertising API integration with dependency injection.

## Features

- **Dependency Injection**: Uses Microsoft.Extensions.DependencyInjection for service registration
- **User Secrets**: Securely manages API credentials using .NET User Secrets
- **Comprehensive Error Handling**: Detailed error analysis and troubleshooting guidance
- **Configuration Display**: Shows current API configuration (with masked secrets)
- **Multiple Test Modes**: Single author, multiple authors, or custom author from command line
- **Response Analysis**: Parses and displays API response details
- **Detailed Logging**: Uses Microsoft.Extensions.Logging for diagnostic information

## ⚙️ Configuration

### Required Settings

| Variable | Description | Where to Find | Why It's Needed |
|----------|-------------|---------------|-----------------|
| `AMAZON_PRODUCT_ACCESS_KEY` | AWS Access Key ID | [AWS Console](https://console.aws.amazon.com) → Security Credentials → Access keys | Authenticate AWS API requests |
| `AMAZON_PRODUCT_SECRET_KEY` | AWS Secret Access Key | Created with Access Key ID (save immediately - shown only once) | Sign API requests with AWS Signature V4 |
| `AMAZON_PRODUCT_PARTNER_TAG` | Amazon Associates Partner Tag | [Amazon Associates](https://affiliate-program.amazon.com) → Your tracking IDs | Required for PA API access and affiliate attribution |
| `AMAZON_PRODUCT_REGION` | AWS Region | Based on your marketplace (typically "us-east-1") | Route API requests to correct regional endpoint |
| `AMAZON_PRODUCT_MARKETPLACE` | Target Amazon Marketplace | Your target marketplace (e.g., "www.amazon.com") | Specify which Amazon store to search |
| `AMAZON_PRODUCT_API_ENDPOINT` | PA API Endpoint URL | API documentation (typically "<https://webservices.amazon.com/paapi5/searchitems>") | Target endpoint for API calls |

### Why These Settings Are Needed

<details>
<summary>🔐 AWS Credentials</summary>

**`AMAZON_PRODUCT_ACCESS_KEY` & `AMAZON_PRODUCT_SECRET_KEY`**

- **Purpose**: Authenticate and sign all requests to Amazon's Product Advertising API
- **Security**: Uses AWS Signature Version 4 for request signing
- **Important**: Never share or commit these credentials

**How to Obtain**:

1. Sign up for [Amazon Associates Program](https://affiliate-program.amazon.com)
2. Apply for [Product Advertising API](https://webservices.amazon.com/paapi5/documentation/) access (separate approval required)
3. After approval, go to [AWS Console](https://console.aws.amazon.com) → Security Credentials
4. Create an Access Key and save both values immediately

</details>

<details>
<summary>🏷️ Partner Tag</summary>

**`AMAZON_PRODUCT_PARTNER_TAG`**

- **Purpose**: Identifies you as an Amazon Associate for API access and affiliate tracking
- **Format**: Varies by region (US: `-20`, UK: `-21`, DE: `-03`, JP: `-22`)
- **Required**: PA API access requires an active Associates account

**How to Obtain**:

1. Log in to [Amazon Associates Central](https://affiliate-program.amazon.com)
2. Go to your tracking IDs/Store IDs
3. Copy your Partner Tag (e.g., "yourstore-20")

</details>

### Setting Up User Secrets

```bash
cd AmazonProductTestConsole
dotnet user-secrets init

# Set all required configuration
dotnet user-secrets set "AMAZON_PRODUCT_ACCESS_KEY" "your-aws-access-key"
dotnet user-secrets set "AMAZON_PRODUCT_SECRET_KEY" "your-aws-secret-key"
dotnet user-secrets set "AMAZON_PRODUCT_PARTNER_TAG" "your-associate-tag-20"
dotnet user-secrets set "AMAZON_PRODUCT_REGION" "us-east-1"
dotnet user-secrets set "AMAZON_PRODUCT_MARKETPLACE" "www.amazon.com"
dotnet user-secrets set "AMAZON_PRODUCT_API_ENDPOINT" "https://webservices.amazon.com/paapi5/searchitems"

# Verify configuration
dotnet user-secrets list
```

## Setup

1. **Build the application:**

   ```bash

   dotnet build AmazonProductTestConsole.csproj

   ```

2. **Configure User Secrets** (see Configuration section above)

## Usage

### Basic Usage

```bash
# Test with Stephen King (default)
dotnet run

# Test with specific author
dotnet run -- "J.K. Rowling"

# Test with author name containing spaces
dotnet run -- "George R.R. Martin"

```

### Advanced Usage

```bash
# Show current configuration
dotnet run -- --config

# Test multiple authors
dotnet run -- --multiple

# Show help
dotnet run -- --help

# Combine options
dotnet run -- --config --author "Isaac Asimov"

# Run without waiting for key press (useful for scripts)
dotnet run -- --nowait "Agatha Christie"

```

## Command Line Options

| Option | Short | Description |
|--------|-------|-------------|
| `--help` | `-h` | Show help message |
| `--config` | `-c` | Display current configuration |
| `--multiple` | `-m` | Test multiple predefined authors |
| `--nowait` | `-n` | Don't wait for key press at end |
| `--author` | `-a` | Specify author name explicitly |

## Output Examples

### Successful API Call

```
=== Amazon Product API Test Console ===

🔍 Testing: Stephen King
==================================================
📞 Calling Amazon Product API...
✅ Success! API call completed in 245ms

📊 Response Analysis:
------------------------------
📚 Found 10 books for 'Stephen King'

📖 Sample Books:
  1. The Shining (ASIN: B001QEAA98)
  2. It (ASIN: B001RLGDKO)
  3. The Stand (ASIN: B001RMZGDS)

📈 Total available results: 127

```

### Configuration Error (404)

```
❌ Configuration Error:
Amazon Product API returned 404 Not Found. This usually indicates:
1. INVALID PARTNER TAG: 'whoicomdevebl-20' - Must be a real Amazon Associates Partner Tag...
2. Product Advertising API access not approved...
[Additional diagnostic information]

🛠️  Configuration Help:
To fix Amazon Product API issues:
1. Sign up for Amazon Associates: https://affiliate-program.amazon.com/
2. Apply for Product Advertising API access (separate from Associates)
3. Get your real Partner Tag from your Associates account
4. Update user secrets with your real Partner Tag

```

## Troubleshooting

### Common Issues

1. **404 Not Found**: Usually indicates invalid Partner Tag or unapproved API access
2. **403 Forbidden**: Invalid AWS credentials or insufficient permissions
3. **400 Bad Request**: Malformed request parameters or JSON payload

### Required Amazon Setup

1. **Amazon Associates Account**: Sign up at <https://affiliate-program.amazon.com/>
2. **Product Advertising API Access**: Apply separately (requires Associates account)
3. **Real Partner Tag**: Get from your Associates account dashboard (format: `yourstore-20`)

## Architecture

The application demonstrates proper dependency injection patterns:

- **Configuration**: `IAmazonProductConfig` → `AmazonProductConfig`
- **HTTP Client**: Registered via `AddHttpClient<>`
- **Service**: `IAmazonProductService` → `AmazonProductService`
- **Logging**: Microsoft.Extensions.Logging with console provider

## Dependencies

- Microsoft.Extensions.Hosting (9.0.10)
- Microsoft.Extensions.Http (9.0.10)
- Microsoft.Extensions.Configuration.UserSecrets (9.0.10)
- OnePageAuthorLib (project reference)

## Project Structure

```
AmazonProductTestConsole/
├── AmazonProductTestConsole.csproj
├── Program.cs              # Basic console application
├── AdvancedProgram.cs      # Enhanced version with debugging features
└── README.md              # This file

```

## Usage in Development

This console application is perfect for:

- Testing Amazon Product API integration during development
- Debugging API configuration issues
- Validating credentials and Partner Tag setup
- Testing different search queries and authors
- Understanding API response structure

## Attaching Debugger

To debug the application:

1. Set breakpoints in the source code
2. Run with debugger: `F5` in VS Code
3. Or attach to running process using VS Code's "Attach to Process" feature

The application uses the same `IAmazonProductService` implementation as the Azure Functions, so debugging here helps diagnose issues in the full application.
