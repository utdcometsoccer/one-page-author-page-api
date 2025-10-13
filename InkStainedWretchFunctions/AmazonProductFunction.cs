using System.Text.Json;
using InkStainedWretch.OnePageAuthorLib.API.Amazon;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Azure Function for calling Amazon Product Advertising API
    /// </summary>
    public class AmazonProductFunction
    {
        private readonly IAmazonProductService _amazonService;
        private readonly ILogger<AmazonProductFunction> _logger;
        private readonly IJwtValidationService _jwtValidationService;
        private readonly IUserProfileService _userProfileService;

        public AmazonProductFunction(
            IAmazonProductService amazonService,
            ILogger<AmazonProductFunction> logger,
            IJwtValidationService jwtValidationService,
            IUserProfileService userProfileService)
        {
            _amazonService = amazonService ?? throw new ArgumentNullException(nameof(amazonService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jwtValidationService = jwtValidationService ?? throw new ArgumentNullException(nameof(jwtValidationService));
            _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
        }

        /// <summary>
        /// Searches for books by author name and returns the unmodified JSON response from Amazon Product Advertising API.
        /// </summary>
        /// <param name="req">HTTP request with authentication</param>
        /// <param name="authorName">Author name from route parameter to search for</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>200 OK</term>
        /// <description>Unmodified JSON response from Amazon Product Advertising API</description>
        /// </item>
        /// <item>
        /// <term>400 Bad Request</term>
        /// <description>Invalid or missing author name</description>
        /// </item>
        /// <item>
        /// <term>401 Unauthorized</term>
        /// <description>Invalid or missing JWT token</description>
        /// </item>
        /// <item>
        /// <term>404 Not Found</term>
        /// <description>No books found with the specified author</description>
        /// </item>
        /// <item>
        /// <term>500 Internal Server Error</term>
        /// <description>Error calling Amazon Product Advertising API</description>
        /// </item>
        /// </list>
        /// </returns>
        /// <example>
        /// <para><strong>TypeScript Example:</strong></para>
        /// <code>
        /// interface AmazonBook {
        ///   asin: string;
        ///   title: string;
        ///   authors?: string[];
        ///   detailPageURL: string;
        ///   imageUrl?: string;
        ///   price?: {
        ///     amount: number;
        ///     currency: string;
        ///   };
        /// }
        /// 
        /// const searchAmazonBooks = async (authorName: string, token: string, page: number = 1): Promise&lt;AmazonBook[]&gt; => {
        ///   // URL encode the author name for safe transmission
        ///   const encodedName = encodeURIComponent(authorName.trim());
        ///   
        ///   const response = await fetch(`/api/amazon/books/author/${encodedName}?page=${page}`, {
        ///     method: 'GET',
        ///     headers: {
        ///       'Authorization': `Bearer ${token}`,
        ///       'Content-Type': 'application/json'
        ///     }
        ///   });
        /// 
        ///   if (response.ok) {
        ///     return await response.json();
        ///   } else if (response.status === 404) {
        ///     return []; // No books found
        ///   } else if (response.status === 401) {
        ///     throw new Error('Unauthorized - invalid or missing token');
        ///   }
        /// 
        ///   throw new Error('Failed to search Amazon books');
        /// };
        /// 
        /// // Usage with React/TypeScript
        /// const AmazonBookSearch: React.FC = () => {
        ///   const [searchTerm, setSearchTerm] = useState('');
        ///   const [books, setBooks] = useState&lt;AmazonBook[]&gt;([]);
        ///   const [loading, setLoading] = useState(false);
        /// 
        ///   const handleSearch = async (e: React.FormEvent) => {
        ///     e.preventDefault();
        ///     if (!searchTerm.trim()) return;
        /// 
        ///     setLoading(true);
        ///     try {
        ///       const results = await searchAmazonBooks(searchTerm, userToken);
        ///       setBooks(results);
        ///     } catch (error) {
        ///       console.error('Search failed:', error);
        ///     } finally {
        ///       setLoading(false);
        ///     }
        ///   };
        /// 
        ///   return (
        ///     &lt;div&gt;
        ///       &lt;form onSubmit={handleSearch}&gt;
        ///         &lt;input
        ///           type="text"
        ///           value={searchTerm}
        ///           onChange={(e) =&gt; setSearchTerm(e.target.value)}
        ///           placeholder="Enter author name..."
        ///         /&gt;
        ///         &lt;button type="submit" disabled={loading}&gt;
        ///           {loading ? 'Searching...' : 'Search'}
        ///         &lt;/button&gt;
        ///       &lt;/form&gt;
        /// 
        ///       &lt;div&gt;
        ///         {books.map(book =&gt; (
        ///           &lt;div key={book.asin}&gt;
        ///             {book.imageUrl &amp;&amp; &lt;img src={book.imageUrl} alt={book.title} /&gt;}
        ///             &lt;h3&gt;{book.title}&lt;/h3&gt;
        ///             &lt;p&gt;{book.authors?.join(', ')}&lt;/p&gt;
        ///             &lt;a href={book.detailPageURL} target="_blank"&gt;View on Amazon&lt;/a&gt;
        ///           &lt;/div&gt;
        ///         ))}
        ///       &lt;/div&gt;
        ///     &lt;/div&gt;
        ///   );
        /// };
        /// </code>
        /// </example>
        [Function("SearchAmazonBooksByAuthor")]
        public async Task<IActionResult> SearchBooksByAuthor(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "amazon/books/author/{authorName}")] HttpRequest req,
            string authorName)
        {
            _logger.LogInformation("SearchAmazonBooksByAuthor function processed a request.");

            // Validate JWT token and get authenticated user
            var (authenticatedUser, authError) = await JwtAuthenticationHelper.ValidateJwtTokenAsync(req, _jwtValidationService, _logger);
            if (authError != null)
            {
                return authError;
            }

            try
            {
                // Ensure user profile exists
                await _userProfileService.EnsureUserProfileAsync(authenticatedUser!);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "User profile validation failed for SearchAmazonBooksByAuthor");
                return new UnauthorizedObjectResult(new { error = "User profile validation failed" });
            }

            try
            {
                // Validate authorName from route parameter
                if (string.IsNullOrEmpty(authorName))
                {
                    _logger.LogWarning("No author name provided in route parameter");
                    return new BadRequestObjectResult(new { error = "Author name is required in the route parameter." });
                }

                // URL decode the author name in case it has special characters
                authorName = Uri.UnescapeDataString(authorName);

                // Get optional page parameter from query string
                var pageParam = req.Query["page"].FirstOrDefault() ?? "1";
                if (!int.TryParse(pageParam, out var page) || page < 1)
                {
                    page = 1; // Default value
                }

                _logger.LogInformation("Searching Amazon for books by author: {AuthorName}, page: {Page}", authorName, page);

                // Call the Amazon Product Advertising API
                using var jsonResult = await _amazonService.SearchBooksByAuthorAsync(authorName, page);

                // Return the JSON result as an OK response
                var jsonString = JsonSerializer.Serialize(jsonResult, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                _logger.LogInformation("Successfully returned Amazon Product API response for author: {AuthorName}", authorName);
                return new ContentResult
                {
                    Content = jsonString,
                    ContentType = "application/json",
                    StatusCode = 200
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request parameters");
                return new BadRequestObjectResult(new { error = $"Invalid request: {ex.Message}" });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling Amazon Product API");
                return new ObjectResult(new { error = $"External API error: {ex.Message}" })
                {
                    StatusCode = 502 // Bad Gateway
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SearchAmazonBooksByAuthor function");
                return new ObjectResult(new { error = "An unexpected error occurred" })
                {
                    StatusCode = 500 // Internal Server Error
                };
            }
        }
    }
}
