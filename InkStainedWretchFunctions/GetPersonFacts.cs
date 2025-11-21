using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorLib.API.Wikipedia;

namespace InkStainedWretchFunctions;

/// <summary>
/// Azure Function for retrieving person facts from Wikipedia.
/// </summary>
public class GetPersonFacts
{
    private readonly ILogger<GetPersonFacts> _logger;
    private readonly IWikipediaService _wikipediaService;

    public GetPersonFacts(
        ILogger<GetPersonFacts> logger,
        IWikipediaService wikipediaService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wikipediaService = wikipediaService ?? throw new ArgumentNullException(nameof(wikipediaService));
    }

    /// <summary>
    /// Gets structured facts about a person from Wikipedia, including title, description, extract, 
    /// lead paragraph, thumbnail, and canonical URL.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="language">The Wikipedia language code (e.g., "en", "es", "fr", "ar", "zh").</param>
    /// <param name="personName">The name of the person to search for.</param>
    /// <returns>
    /// <list type="table">
    /// <item>
    /// <term>200 OK</term>
    /// <description>Structured JSON response with person facts from Wikipedia</description>
    /// </item>
    /// <item>
    /// <term>400 Bad Request</term>
    /// <description>Invalid or missing parameters</description>
    /// </item>
    /// <item>
    /// <term>404 Not Found</term>
    /// <description>No Wikipedia page found for the specified person</description>
    /// </item>
    /// <item>
    /// <term>500 Internal Server Error</term>
    /// <description>Error calling Wikipedia APIs</description>
    /// </item>
    /// </list>
    /// </returns>
    /// <example>
    /// <para><strong>TypeScript Example:</strong></para>
    /// <code>
    /// interface WikipediaPersonFacts {
    ///   title: string;
    ///   description: string;
    ///   extract: string;
    ///   leadParagraph: string;
    ///   thumbnail?: {
    ///     source: string;
    ///     width: number;
    ///     height: number;
    ///   };
    ///   canonicalUrl: string;
    ///   language: string;
    /// }
    /// 
    /// const getPersonFacts = async (
    ///   personName: string, 
    ///   language: string = 'en'
    /// ): Promise&lt;WikipediaPersonFacts&gt; => {
    ///   // URL encode the person name for safe transmission
    ///   const encodedName = encodeURIComponent(personName.trim());
    ///   
    ///   const response = await fetch(
    ///     `/api/wikipedia/${language}/${encodedName}`,
    ///     {
    ///       method: 'GET',
    ///       headers: {
    ///         'Content-Type': 'application/json'
    ///       }
    ///     }
    ///   );
    /// 
    ///   if (response.ok) {
    ///     return await response.json();
    ///   } else if (response.status === 404) {
    ///     throw new Error('Person not found on Wikipedia');
    ///   } else if (response.status === 400) {
    ///     throw new Error('Invalid request parameters');
    ///   }
    /// 
    ///   throw new Error('Failed to fetch person facts');
    /// };
    /// 
    /// // Usage with React/TypeScript
    /// const PersonProfile: React.FC&lt;{ name: string }&gt; = ({ name }) => {
    ///   const [facts, setFacts] = useState&lt;WikipediaPersonFacts | null&gt;(null);
    ///   const [loading, setLoading] = useState(true);
    ///   const [error, setError] = useState&lt;string | null&gt;(null);
    /// 
    ///   useEffect(() => {
    ///     const loadFacts = async () => {
    ///       try {
    ///         setLoading(true);
    ///         setError(null);
    ///         const personFacts = await getPersonFacts(name, 'en');
    ///         setFacts(personFacts);
    ///       } catch (err) {
    ///         setError(err.message);
    ///       } finally {
    ///         setLoading(false);
    ///       }
    ///     };
    /// 
    ///     loadFacts();
    ///   }, [name]);
    /// 
    ///   if (loading) return &lt;div&gt;Loading person facts...&lt;/div&gt;;
    ///   if (error) return &lt;div&gt;Error: {error}&lt;/div&gt;;
    ///   if (!facts) return &lt;div&gt;No information found&lt;/div&gt;;
    /// 
    ///   return (
    ///     &lt;div className="person-profile"&gt;
    ///       {facts.thumbnail &amp;&amp; (
    ///         &lt;img 
    ///           src={facts.thumbnail.source} 
    ///           alt={facts.title}
    ///           width={facts.thumbnail.width}
    ///           height={facts.thumbnail.height}
    ///         /&gt;
    ///       )}
    ///       &lt;h1&gt;{facts.title}&lt;/h1&gt;
    ///       &lt;p className="description"&gt;{facts.description}&lt;/p&gt;
    ///       &lt;div className="extract"&gt;{facts.extract}&lt;/div&gt;
    ///       &lt;div className="lead"&gt;{facts.leadParagraph}&lt;/div&gt;
    ///       &lt;a 
    ///         href={facts.canonicalUrl} 
    ///         target="_blank" 
    ///         rel="noopener noreferrer"
    ///       &gt;
    ///         Read more on Wikipedia
    ///       &lt;/a&gt;
    ///     &lt;/div&gt;
    ///   );
    /// };
    /// 
    /// // Example usage with different languages
    /// const MultiLanguagePersonProfile: React.FC&lt;{ name: string }&gt; = ({ name }) => {
    ///   const [language, setLanguage] = useState('en');
    ///   const [facts, setFacts] = useState&lt;WikipediaPersonFacts | null&gt;(null);
    /// 
    ///   const loadFactsInLanguage = async (lang: string) => {
    ///     try {
    ///       const personFacts = await getPersonFacts(name, lang);
    ///       setFacts(personFacts);
    ///       setLanguage(lang);
    ///     } catch (err) {
    ///       console.error('Failed to load facts:', err);
    ///     }
    ///   };
    /// 
    ///   return (
    ///     &lt;div&gt;
    ///       &lt;div className="language-selector"&gt;
    ///         &lt;button onClick={() =&gt; loadFactsInLanguage('en')}&gt;English&lt;/button&gt;
    ///         &lt;button onClick={() =&gt; loadFactsInLanguage('es')}&gt;Español&lt;/button&gt;
    ///         &lt;button onClick={() =&gt; loadFactsInLanguage('fr')}&gt;Français&lt;/button&gt;
    ///         &lt;button onClick={() =&gt; loadFactsInLanguage('de')}&gt;Deutsch&lt;/button&gt;
    ///       &lt;/div&gt;
    ///       {facts &amp;&amp; (
    ///         &lt;div&gt;
    ///           &lt;h1&gt;{facts.title}&lt;/h1&gt;
    ///           &lt;p&gt;{facts.description}&lt;/p&gt;
    ///           &lt;div&gt;{facts.extract}&lt;/div&gt;
    ///         &lt;/div&gt;
    ///       )}
    ///     &lt;/div&gt;
    ///   );
    /// };
    /// </code>
    /// <para><strong>cURL Example:</strong></para>
    /// <code>
    /// # Get facts about Albert Einstein in English
    /// curl "http://localhost:7071/api/wikipedia/en/Albert_Einstein"
    /// 
    /// # Get facts about Marie Curie in French
    /// curl "http://localhost:7071/api/wikipedia/fr/Marie_Curie"
    /// 
    /// # Get facts with spaces in name (URL encoded)
    /// curl "http://localhost:7071/api/wikipedia/en/Stephen%20Hawking"
    /// </code>
    /// <para><strong>Response Example:</strong></para>
    /// <code>
    /// {
    ///   "title": "Albert Einstein",
    ///   "description": "German-born scientist (1879–1955)",
    ///   "extract": "Albert Einstein was a German-born theoretical physicist who is widely held to be one of the greatest and most influential scientists of all time.",
    ///   "leadParagraph": "Albert Einstein (14 March 1879 – 18 April 1955) was a German-born theoretical physicist...",
    ///   "thumbnail": {
    ///     "source": "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3e/Einstein_1921_by_F_Schmutzer.jpg/320px-Einstein_1921_by_F_Schmutzer.jpg",
    ///     "width": 320,
    ///     "height": 396
    ///   },
    ///   "canonicalUrl": "https://en.wikipedia.org/wiki/Albert_Einstein",
    ///   "language": "en"
    /// }
    /// </code>
    /// </example>
    [Function("GetPersonFacts")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "wikipedia/{language}/{personName}")] HttpRequest req,
        string language,
        string personName)
    {
        _logger.LogInformation("GetPersonFacts function processed a request for person: {PersonName} in language: {Language}", 
            personName, language);

        // Validate parameters
        if (string.IsNullOrWhiteSpace(language))
        {
            _logger.LogWarning("Language parameter is null or empty");
            return new BadRequestObjectResult(new { error = "Language parameter is required" });
        }

        if (string.IsNullOrWhiteSpace(personName))
        {
            _logger.LogWarning("PersonName parameter is null or empty");
            return new BadRequestObjectResult(new { error = "Person name parameter is required" });
        }

        try
        {
            // URL decode the person name in case it has special characters
            personName = Uri.UnescapeDataString(personName);

            // Normalize language code to lowercase
            var normalizedLanguage = language.ToLowerInvariant();

            _logger.LogInformation("Fetching Wikipedia facts for: {PersonName} in {Language}", personName, normalizedLanguage);

            // Call the Wikipedia service
            var facts = await _wikipediaService.GetPersonFactsAsync(personName, normalizedLanguage);

            // Check if we got any meaningful data
            if (string.IsNullOrEmpty(facts.Title) && string.IsNullOrEmpty(facts.Extract))
            {
                _logger.LogInformation("No Wikipedia page found for person: {PersonName}", personName);
                return new NotFoundObjectResult(new 
                { 
                    message = $"No Wikipedia page found for: {personName}",
                    language = normalizedLanguage,
                    searchTerm = personName
                });
            }

            _logger.LogInformation("Successfully retrieved Wikipedia facts for: {PersonName}", personName);
            return new OkObjectResult(facts);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters for person: {PersonName}", personName);
            return new BadRequestObjectResult(new { error = $"Invalid request: {ex.Message}" });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling Wikipedia APIs for person: {PersonName}", personName);
            return new ObjectResult(new { error = $"External API error: {ex.Message}" })
            {
                StatusCode = StatusCodes.Status502BadGateway
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetPersonFacts function for person: {PersonName}", personName);
            return new ObjectResult(new { error = "An unexpected error occurred while retrieving person facts" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
