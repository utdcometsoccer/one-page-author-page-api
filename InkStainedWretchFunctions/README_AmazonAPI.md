# Amazon Product Advertising API Azure Functions

This project now includes Azure Functions that call the Amazon Product Advertising API (PAAPI 5.0) and return unmodified JSON responses.

## Functions Available

### 1. SearchAmazonBooksByAuthor
Searches for books by author name and returns the raw JSON response from Amazon Product Advertising API.

**Endpoint**: `GET /api/amazon/books/author/{authorName}`

**Parameters:**
- Route parameter: `authorName` (required) - Author name to search for
- Query parameter: `?page=1` (optional) - Page number for pagination (default: 1)

**Example Usage:**
```bash
# GET request with route parameter
curl "http://localhost:7072/api/amazon/books/author/Stephen%20King" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# GET request with pagination
curl "http://localhost:7072/api/amazon/books/author/Stephen%20King?page=2" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Configuration

The functions use the following configuration values from `local.settings.json`:

```json
{
  "Values": {
    "AMAZON_PRODUCT_ACCESS_KEY": "your-aws-access-key",
    "AMAZON_PRODUCT_SECRET_KEY": "your-aws-secret-key",
    "AMAZON_PRODUCT_PARTNER_TAG": "yourtag-20",
    "AMAZON_PRODUCT_REGION": "us-east-1",
    "AMAZON_PRODUCT_MARKETPLACE": "www.amazon.com",
    "AMAZON_PRODUCT_API_ENDPOINT": "https://webservices.amazon.com/paapi5/searchitems"
  }
}
```

### Configuration Values Explained

- **AMAZON_PRODUCT_ACCESS_KEY**: Your AWS Access Key ID from Amazon Product Advertising API
- **AMAZON_PRODUCT_SECRET_KEY**: Your AWS Secret Access Key
- **AMAZON_PRODUCT_PARTNER_TAG**: Your Amazon Associates Partner Tag (e.g., "yourtag-20")
- **AMAZON_PRODUCT_REGION**: AWS region for the API endpoint (e.g., "us-east-1")
- **AMAZON_PRODUCT_MARKETPLACE**: Amazon marketplace domain (e.g., "www.amazon.com")
- **AMAZON_PRODUCT_API_ENDPOINT**: Full API endpoint URL

## 🔍 How to Find Your Correct Amazon Partner Tag

### Step 1: Sign in to Amazon Associates
1. Go to [Amazon Associates Central](https://affiliate-program.amazon.com/)
2. Sign in with your Amazon Associates account
3. If you don't have an account, you'll need to apply first (approval required)

### Step 2: Locate Your Associate Tag
1. **Method A - Dashboard**: 
   - On your Associates dashboard, look for "Associate ID" or "Tracking ID"
   - It will be in the format: `yourstore-20` or `yourname-21`, etc.

2. **Method B - Account Settings**:
   - Go to "Account & Login Info" → "Manage Your Tracking IDs"
   - You'll see all your Associate IDs listed here

3. **Method C - Link Generator**:
   - Use the "Product Linking" → "Link to Any Page" tool
   - Your Associate ID will appear in the generated links as `tag=yourstore-20`

### Step 3: Verify Tag Format
Your Partner Tag should follow this pattern:
- Format: `{store-name}-{number}`
- Examples: `mystore-20`, `johnbooks-21`, `techreviews-20`
- Length: Usually 8-15 characters
- Ends with: `-20`, `-21`, `-22`, etc. (based on your locale)

### Step 4: Important Requirements
⚠️ **CRITICAL**: Having an Amazon Associates account is NOT enough!

You must also:
1. **Apply for Product Advertising API Access**
   - Go to [Amazon Developer Portal](https://developer.amazon.com/)
   - Sign in and navigate to "Product Advertising API"
   - Submit application (separate approval process)
   - Wait for approval (can take several days/weeks)

2. **Generate AWS Credentials**
   - After PA API approval, generate Access Key and Secret Key
   - These are different from your regular AWS credentials

### Step 5: Test Your Configuration
Update your user secrets with the correct Partner Tag:
```bash
dotnet user-secrets set "AMAZON_PRODUCT_PARTNER_TAG" "your-actual-tag-20" --project InkStainedWretchFunctions.csproj
```

### Common Issues & Solutions

❌ **404 Error with Valid Tag**: Your Associates account may not have Product Advertising API approval
- Solution: Apply for PA API access separately

❌ **403 Error**: Invalid AWS credentials or wrong region
- Solution: Verify Access Key, Secret Key, and region match your PA API setup

❌ **400 Error**: Malformed request or invalid parameters
- Solution: Check endpoint URL and request format

### Testing Your Partner Tag
You can verify your tag is working by:
1. Using the console application: `dotnet run --project AmazonProductTestConsole`
2. Checking the detailed error messages for specific guidance
3. Monitoring the debug logs for signature validation

### Partner Tag Locations by Country
- **US**: Ends with `-20`
- **UK**: Ends with `-21` 
- **Germany**: Ends with `-03`
- **France**: Ends with `-21`
- **Japan**: Ends with `-22`
- **Canada**: Ends with `-20`

## Architecture

The implementation includes:

1. **IAmazonProductConfig** - Interface for configuration settings
2. **AmazonProductConfig** - Implementation that reads from local configuration
3. **IAmazonProductService** - Service interface for API calls
4. **AmazonProductService** - Service implementation with HttpClient and AWS Signature Version 4 signing
5. **AmazonProductFunction** - Azure Functions that expose HTTP endpoints

## Features

- **Unmodified JSON Response**: Returns the exact JSON from Amazon Product Advertising API
- **Error Handling**: Proper HTTP status codes and error messages
- **Logging**: Comprehensive logging for debugging and monitoring
- **Flexible Input**: Accepts parameters via route and query string
- **Dependency Injection**: Properly registered services with HttpClient factory
- **Configuration Validation**: Validates required configuration on startup
- **JWT Authentication**: Protected endpoints requiring valid JWT tokens
- **AWS Signature V4**: Implements AWS Signature Version 4 authentication

## Running the Functions

1. Ensure your `local.settings.json` has the correct Amazon Product Advertising API configuration
2. Run the functions locally:
   ```bash
   dotnet run
   ```
3. The functions will be available at `http://localhost:7072/api/`

## Response Format

The functions return the raw JSON response from the Amazon Product Advertising API, preserving the original structure and data without any modifications.

### Sample Response

```json
{
  "SearchResult": {
    "Items": [
      {
        "ASIN": "B001EXAMPLE",
        "DetailPageURL": "https://www.amazon.com/dp/B001EXAMPLE",
        "Images": {
          "Primary": {
            "Medium": {
              "URL": "https://m.media-amazon.com/images/I/example.jpg",
              "Height": 160,
              "Width": 107
            }
          }
        },
        "ItemInfo": {
          "ByLineInfo": {
            "Contributors": [
              {
                "Name": "Stephen King",
                "Role": "Author"
              }
            ]
          },
          "Title": {
            "DisplayValue": "The Shining"
          }
        },
        "Offers": {
          "Listings": [
            {
              "Price": {
                "Amount": 9.99,
                "Currency": "USD",
                "DisplayAmount": "$9.99"
              }
            }
          ]
        }
      }
    ],
    "TotalResultCount": 150
  }
}
```

## Getting Amazon Product Advertising API Credentials

1. Sign up for [Amazon Associates Program](https://affiliate-program.amazon.com/)
2. Register for [Product Advertising API](https://webservices.amazon.com/paapi5/documentation/)
3. Obtain your Access Key, Secret Key, and Partner Tag
4. Add credentials to your `local.settings.json` or environment variables

## Testing

Unit tests are available in the `OnePageAuthor.Test` project:

```bash
dotnet test --filter "Category=AmazonProduct"
```

## API Documentation

For more details on Amazon Product Advertising API, see:
- [Official PAAPI 5.0 Documentation](https://webservices.amazon.com/paapi5/documentation/)
- [SearchItems Operation](https://webservices.amazon.com/paapi5/documentation/search-items.html)
