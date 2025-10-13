using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorLib.API.Amazon
{
    /// <summary>
    /// Service implementation for calling Amazon Product Advertising API
    /// </summary>
    public class AmazonProductService : IAmazonProductService
    {
        private readonly HttpClient _httpClient;
        private readonly IAmazonProductConfig _config;
        private readonly ILogger<AmazonProductService> _logger;
        private const string ServiceName = "ProductAdvertisingAPI";
        private const string Algorithm = "AWS4-HMAC-SHA256";

        public AmazonProductService(
            HttpClient httpClient,
            IAmazonProductConfig config,
            ILogger<AmazonProductService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Searches for books by author name and returns the raw JSON response
        /// </summary>
        /// <param name="authorName">Name of the author to search for</param>
        /// <param name="itemPage">Page number for pagination (default: 1)</param>
        /// <returns>Raw JSON response from the API</returns>
        public async Task<JsonDocument> SearchBooksByAuthorAsync(string authorName, int itemPage = 1)
        {
            if (string.IsNullOrEmpty(authorName))
            {
                throw new ArgumentException("Author name cannot be null or empty", nameof(authorName));
            }

            if (itemPage < 1)
            {
                throw new ArgumentException("Item page must be greater than 0", nameof(itemPage));
            }

            try
            {
                // Build the request payload
                var requestPayload = new
                {
                    PartnerType = "Associates",
                    PartnerTag = _config.PartnerTag,
                    Operation = "SearchItems",
                    SearchIndex = "Books",
                    Author = authorName,
                    ItemPage = itemPage,
                    Resources = new[]
                    {
                        "Images.Primary.Medium",
                        "ItemInfo.Title",
                        "ItemInfo.ByLineInfo",
                        "ItemInfo.ContentInfo",
                        "ItemInfo.ProductInfo",
                        "Offers.Listings.Price"
                    }
                };

                var requestJson = JsonSerializer.Serialize(requestPayload);
                _logger.LogInformation("Searching Amazon for books by author: {AuthorName}, page: {ItemPage}", authorName, itemPage);

                // Create signed request
                var request = CreateSignedRequest(requestJson);

                // Send the request
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Parse and return the JSON response
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(responseContent);

                _logger.LogInformation("Successfully retrieved Amazon product data for author: {AuthorName}", authorName);
                return jsonDocument;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while calling Amazon Product API for author: {AuthorName}", authorName);
                throw new InvalidOperationException($"Failed to call Amazon Product API: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Amazon Product API response for author: {AuthorName}", authorName);
                throw new InvalidOperationException($"Failed to parse Amazon Product API response: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while searching Amazon for author: {AuthorName}", authorName);
                throw new InvalidOperationException($"Failed to search Amazon products: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a signed HTTP request using AWS Signature Version 4
        /// </summary>
        private HttpRequestMessage CreateSignedRequest(string requestBody)
        {
            var timestamp = DateTime.UtcNow;
            var dateStamp = timestamp.ToString("yyyyMMdd");
            var amzDate = timestamp.ToString("yyyyMMddTHHmmssZ");

            var request = new HttpRequestMessage(HttpMethod.Post, _config.ApiEndpoint);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Add required headers
            request.Headers.Add("host", new Uri(_config.ApiEndpoint).Host);
            request.Headers.Add("x-amz-date", amzDate);
            request.Headers.Add("x-amz-target", "com.amazon.paapi5.v1.ProductAdvertisingAPIv1.SearchItems");

            // Create canonical request
            var canonicalUri = new Uri(_config.ApiEndpoint).AbsolutePath;
            var canonicalQueryString = "";
            var canonicalHeaders = $"host:{new Uri(_config.ApiEndpoint).Host}\n" +
                                   $"x-amz-date:{amzDate}\n" +
                                   $"x-amz-target:com.amazon.paapi5.v1.ProductAdvertisingAPIv1.SearchItems\n";
            var signedHeaders = "host;x-amz-date;x-amz-target";

            // Hash the payload
            var payloadHash = GetSha256Hash(requestBody);

            var canonicalRequest = $"POST\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

            // Create string to sign
            var credentialScope = $"{dateStamp}/{_config.Region}/{ServiceName}/aws4_request";
            var stringToSign = $"{Algorithm}\n{amzDate}\n{credentialScope}\n{GetSha256Hash(canonicalRequest)}";

            // Calculate signature
            var signingKey = GetSignatureKey(_config.SecretKey, dateStamp, _config.Region, ServiceName);
            var signature = BytesToHex(HmacSha256(signingKey, stringToSign));

            // Add authorization header using TryAddWithoutValidation to bypass validation
            var authorizationHeader = $"{Algorithm} Credential={_config.AccessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);

            return request;
        }

        /// <summary>
        /// Computes SHA-256 hash of a string
        /// </summary>
        private static string GetSha256Hash(string text)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = sha256.ComputeHash(bytes);
            return BytesToHex(hash);
        }

        /// <summary>
        /// Converts byte array to hexadecimal string
        /// </summary>
        private static string BytesToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Computes HMAC-SHA256
        /// </summary>
        private static byte[] HmacSha256(byte[] key, string data)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Derives the signing key for AWS Signature Version 4
        /// </summary>
        private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
        {
            var kSecret = Encoding.UTF8.GetBytes($"AWS4{key}");
            var kDate = HmacSha256(kSecret, dateStamp);
            var kRegion = HmacSha256(kDate, regionName);
            var kService = HmacSha256(kRegion, serviceName);
            var kSigning = HmacSha256(kService, "aws4_request");
            return kSigning;
        }
    }
}
