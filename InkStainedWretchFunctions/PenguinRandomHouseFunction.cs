using System.Text.Json;
using InkStainedWretch.OnePageAuthorLib.API.Penguin;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Azure Function for calling Penguin Random House API
    /// </summary>
    public class PenguinRandomHouseFunction
    {
        private readonly IPenguinRandomHouseService _penguinService;
        private readonly ILogger<PenguinRandomHouseFunction> _logger;
        private readonly IJwtValidationService _jwtValidationService;
        private readonly IUserProfileService _userProfileService;

        public PenguinRandomHouseFunction(
            IPenguinRandomHouseService penguinService,
            ILogger<PenguinRandomHouseFunction> logger,
            IJwtValidationService jwtValidationService,
            IUserProfileService userProfileService)
        {
            _penguinService = penguinService ?? throw new ArgumentNullException(nameof(penguinService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jwtValidationService = jwtValidationService ?? throw new ArgumentNullException(nameof(jwtValidationService));
            _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
        }

        /// <summary>
        /// Searches for authors by name and returns the unmodified JSON response from Penguin Random House API.
        /// </summary>
        /// <param name="req">HTTP request with authentication</param>
        /// <param name="authorName">Author name from route parameter to search for</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>200 OK</term>
        /// <description>Unmodified JSON response from Penguin Random House API</description>
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
        /// <description>No authors found with the specified name</description>
        /// </item>
        /// <item>
        /// <term>500 Internal Server Error</term>
        /// <description>Error calling Penguin Random House API</description>
        /// </item>
        /// </list>
        /// </returns>
        /// <example>
        /// <para><strong>TypeScript Example:</strong></para>
        /// <code>
        /// interface PenguinAuthor {
        ///   authorId: string;
        ///   authorName: string;
        ///   authorUrl: string;
        ///   books?: Array&lt;{
        ///     isbn: string;
        ///     title: string;
        ///     publishDate: string;
        ///   }&gt;;
        /// }
        /// 
        /// const searchAuthors = async (authorName: string, token: string): Promise&lt;PenguinAuthor[]&gt; => {
        ///   // URL encode the author name for safe transmission
        ///   const encodedName = encodeURIComponent(authorName.trim());
        ///   
        ///   const response = await fetch(`/api/penguin/authors/${encodedName}`, {
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
        ///     return []; // No authors found
        ///   } else if (response.status === 401) {
        ///     throw new Error('Unauthorized - invalid or missing token');
        ///   }
        /// 
        ///   throw new Error('Failed to search authors');
        /// };
        /// 
        /// // Usage with React/TypeScript
        /// const AuthorSearch: React.FC = () => {
        ///   const [searchTerm, setSearchTerm] = useState('');
        ///   const [authors, setAuthors] = useState&lt;PenguinAuthor[]&gt;([]);
        ///   const [loading, setLoading] = useState(false);
        /// 
        ///   const handleSearch = async (e: React.FormEvent) => {
        ///     e.preventDefault();
        ///     if (!searchTerm.trim()) return;
        /// 
        ///     setLoading(true);
        ///     try {
        ///       const results = await searchAuthors(searchTerm, userToken);
        ///       setAuthors(results);
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
        ///         {authors.map(author =&gt; (
        ///           &lt;div key={author.authorId}&gt;
        ///             &lt;h3&gt;{author.authorName}&lt;/h3&gt;
        ///             &lt;a href={author.authorUrl} target="_blank"&gt;View Profile&lt;/a&gt;
        ///           &lt;/div&gt;
        ///         ))}
        ///       &lt;/div&gt;
        ///     &lt;/div&gt;
        ///   );
        /// };
        /// </code>
        /// </example>
        [Function("SearchPenguinAuthors")]
        public async Task<IActionResult> SearchAuthors(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "penguin/authors/{authorName}")] HttpRequest req,
            string authorName)
        {
            _logger.LogInformation("SearchPenguinAuthors function processed a request.");

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
                _logger.LogWarning(ex, "User profile validation failed for SearchPenguinAuthors");
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

                _logger.LogInformation("Searching for author: {AuthorName}", authorName);

                // Call the Penguin Random House API
                using var jsonResult = await _penguinService.SearchAuthorsAsync(authorName);

                // Return the JSON result as an OK response
                var jsonString = JsonSerializer.Serialize(jsonResult, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                _logger.LogInformation("Successfully returned Penguin Random House API response for author: {AuthorName}", authorName);
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
                _logger.LogError(ex, "Error calling Penguin Random House API");
                return new ObjectResult(new { error = $"External API error: {ex.Message}" })
                {
                    StatusCode = 502 // Bad Gateway
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SearchPenguinAuthors function");
                return new ObjectResult(new { error = "An unexpected error occurred" })
                {
                    StatusCode = 500 // Internal Server Error
                };
            }
        }

        /// <summary>
        /// Gets titles by author key and returns the unmodified JSON response from Penguin Random House API.
        /// </summary>
        /// <param name="req">HTTP request with authentication</param>
        /// <param name="authorKey">Author key from route parameter (obtained from search results)</param>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>200 OK</term>
        /// <description>Unmodified JSON response from Penguin Random House API with book titles</description>
        /// </item>
        /// <item>
        /// <term>400 Bad Request</term>
        /// <description>Invalid or missing author key</description>
        /// </item>
        /// <item>
        /// <term>401 Unauthorized</term>
        /// <description>Invalid or missing JWT token</description>
        /// </item>
        /// <item>
        /// <term>404 Not Found</term>
        /// <description>No titles found for the specified author</description>
        /// </item>
        /// <item>
        /// <term>500 Internal Server Error</term>
        /// <description>Error calling Penguin Random House API</description>
        /// </item>
        /// </list>
        /// </returns>
        /// <example>
        /// <para><strong>TypeScript Example:</strong></para>
        /// <code>
        /// interface PenguinBook {
        ///   isbn: string;
        ///   title: string;
        ///   subtitle?: string;
        ///   description: string;
        ///   publishDate: string;
        ///   price: number;
        ///   currency: string;
        ///   coverImageUrl?: string;
        ///   genres: string[];
        ///   pageCount?: number;
        /// }
        /// 
        /// const getAuthorTitles = async (authorKey: string, token: string): Promise&lt;PenguinBook[]&gt; => {
        ///   const response = await fetch(`/api/penguin/authors/${authorKey}/titles`, {
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
        ///     return []; // No titles found
        ///   } else if (response.status === 401) {
        ///     throw new Error('Unauthorized - invalid or missing token');
        ///   }
        /// 
        ///   throw new Error('Failed to fetch author titles');
        /// };
        /// 
        /// // Usage with React/TypeScript
        /// const AuthorBooks: React.FC&lt;{ authorKey: string }&gt; = ({ authorKey }) => {
        ///   const [books, setBooks] = useState&lt;PenguinBook[]&gt;([]);
        ///   const [loading, setLoading] = useState(true);
        /// 
        ///   useEffect(() => {
        ///     const loadBooks = async () => {
        ///       try {
        ///         const authorBooks = await getAuthorTitles(authorKey, userToken);
        ///         setBooks(authorBooks);
        ///       } catch (error) {
        ///         console.error('Failed to load books:', error);
        ///       } finally {
        ///         setLoading(false);
        ///       }
        ///     };
        /// 
        ///     loadBooks();
        ///   }, [authorKey, userToken]);
        /// 
        ///   if (loading) return &lt;div&gt;Loading books...&lt;/div&gt;;
        /// 
        ///   return (
        ///     &lt;div&gt;
        ///       &lt;h2&gt;Books ({books.length})&lt;/h2&gt;
        ///       &lt;div className="books-grid"&gt;
        ///         {books.map(book =&gt; (
        ///           &lt;div key={book.isbn} className="book-card"&gt;
        ///             {book.coverImageUrl &amp;&amp; (
        ///               &lt;img src={book.coverImageUrl} alt={book.title} /&gt;
        ///             )}
        ///             &lt;h3&gt;{book.title}&lt;/h3&gt;
        ///             {book.subtitle &amp;&amp; &lt;h4&gt;{book.subtitle}&lt;/h4&gt;}
        ///             &lt;p&gt;{book.description}&lt;/p&gt;
        ///             &lt;p&gt;Published: {new Date(book.publishDate).toLocaleDateString()}&lt;/p&gt;
        ///             &lt;p&gt;Price: ${book.price} {book.currency}&lt;/p&gt;
        ///           &lt;/div&gt;
        ///         ))}
        ///       &lt;/div&gt;
        ///     &lt;/div&gt;
        ///   );
        /// };
        /// </code>
        /// </example>
        [Function("GetPenguinTitlesByAuthor")]
        public async Task<IActionResult> GetTitlesByAuthor(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "penguin/authors/{authorKey}/titles")] HttpRequest req,
            string authorKey)
        {
            _logger.LogInformation("GetPenguinTitlesByAuthor function processed a request.");

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
                _logger.LogWarning(ex, "User profile validation failed for GetPenguinTitlesByAuthor");
                return new UnauthorizedObjectResult(new { error = "User profile validation failed" });
            }

            try
            {
                // Validate authorKey from route parameter
                if (string.IsNullOrEmpty(authorKey))
                {
                    _logger.LogWarning("No author key provided in route parameter");
                    return new BadRequestObjectResult(new { error = "Author key is required in the route parameter." });
                }

                // URL decode the author key in case it has special characters
                authorKey = Uri.UnescapeDataString(authorKey);

                // Get optional parameters from query string
                var rowsParam = req.Query["rows"].FirstOrDefault() ?? "10";
                var startParam = req.Query["start"].FirstOrDefault() ?? "0";

                if (!int.TryParse(rowsParam, out var rows) || rows <= 0)
                {
                    rows = 10; // Default value
                }

                if (!int.TryParse(startParam, out var start) || start < 0)
                {
                    start = 0; // Default value
                }

                _logger.LogInformation("Getting titles for author key: {AuthorKey}, rows: {Rows}, start: {Start}", authorKey, rows, start);

                // Call the Penguin Random House API
                using var jsonResult = await _penguinService.GetTitlesByAuthorAsync(authorKey, rows, start);

                // Return the JSON result as an OK response
                var jsonString = JsonSerializer.Serialize(jsonResult, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                _logger.LogInformation("Successfully returned Penguin Random House API response for author key: {AuthorKey}", authorKey);
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
                _logger.LogError(ex, "Error calling Penguin Random House API");
                return new ObjectResult(new { error = $"External API error: {ex.Message}" })
                {
                    StatusCode = 502 // Bad Gateway
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetPenguinTitlesByAuthor function");
                return new ObjectResult(new { error = "An unexpected error occurred" })
                {
                    StatusCode = 500 // Internal Server Error
                };
            }
        }
    }
}