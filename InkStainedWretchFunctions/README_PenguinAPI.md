# Penguin Random House API Azure Functions

This project now includes Azure Functions that call the Penguin Random House API and return unmodified JSON responses.

## Functions Available

### 1. SearchPenguinAuthors
Searches for authors by name and returns the raw JSON response from Penguin Random House API.

**Endpoint**: `GET/POST /api/SearchPenguinAuthors`

**Parameters:**
- Query parameter: `?authorName=Stephen King`
- Or JSON body: `{"authorName": "Stephen King"}`
- Alternative parameter names: `author`,
ame`

**Example Usage:**
```bash
# GET request with query parameter
curl "http://localhost:7072/api/SearchPenguinAuthors?authorName=Stephen%20King"

# POST request with JSON body
curl -X POST "http://localhost:7072/api/SearchPenguinAuthors" \
  -H "Content-Type: application/json" \
  -d '{"authorName": "Stephen King"}'
```

### 2. GetPenguinTitlesByAuthor
Gets titles by author key and returns the raw JSON response from Penguin Random House API.

**Endpoint**: `GET/POST /api/GetPenguinTitlesByAuthor`

**Parameters:**
- Query parameters: `?authorKey=123456&rows=10&start=0`
- Or JSON body: `{"authorKey": "123456", "rows": 10, "start": 0}`

**Example Usage:**
```bash
# GET request with query parameters
curl "http://localhost:7072/api/GetPenguinTitlesByAuthor?authorKey=123456&rows=10&start=0"

# POST request with JSON body
curl -X POST "http://localhost:7072/api/GetPenguinTitlesByAuthor" \
  -H "Content-Type: application/json" \
  -d '{"authorKey": "123456", "rows": 10, "start": 0}'
```

## Configuration

The functions use the following configuration values from `local.settings.json`:

```json
{
  "Values": {
    "PENGUIN_RANDOM_HOUSE_API_URL": "https://api.penguinrandomhouse.com/",
    "PENGUIN_RANDOM_HOUSE_API_KEY": "your-api-key",
    "PENGUIN_RANDOM_HOUSE_API_DOMAIN": "PRH.US",
    "PENGUIN_RANDOM_HOUSE_SEARCH_API": "resources/v2/title/domains/{domain}/search?rows=0&q={query}&docType=author&api_key={api_key}",
    "PENGUIN_RANDOM_HOUSE_LIST_TITLES_BY_AUTHOR_API": "resources/v2/title/domains/{domain}/authors/{authorKey}/titles?rows={rows}&start={start}&api_key={api_key}",
    "PENGUIN_RANDOM_HOUSE_URL": "https://www.penguinrandomhouse.com/"
  }
}
```

## Architecture

The implementation includes:

1. **IPenguinRandomHouseConfig** - Interface for configuration settings
2. **PenguinRandomHouseConfig** - Implementation that reads from local configuration
3. **IPenguinRandomHouseService** - Service interface for API calls
4. **PenguinRandomHouseService** - Service implementation with HttpClient
5. **PenguinRandomHouseFunction** - Azure Functions that expose HTTP endpoints

## Features

- **Unmodified JSON Response**: Returns the exact JSON from Penguin Random House API
- **Error Handling**: Proper HTTP status codes and error messages
- **Logging**: Comprehensive logging for debugging and monitoring
- **Flexible Input**: Accepts parameters via query string or JSON body
- **Dependency Injection**: Properly registered services with HttpClient factory
- **Configuration Validation**: Validates required configuration on startup

## Running the Functions

1. Ensure your `local.settings.json` has the correct Penguin Random House API configuration
2. Run the functions locally:
   ```bash
   dotnet run
   ```
3. The functions will be available at `http://localhost:7072/api/`

## Response Format

The functions return the raw JSON response from the Penguin Random House API, preserving the original structure and data without any modifications.
