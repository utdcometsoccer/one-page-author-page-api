using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Xml.Linq;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.Function;

public class GetSitemap
{
    private readonly ILogger<GetSitemap> _logger;
    private readonly IDomainRegistrationRepository _domainRegistrationRepository;

    public GetSitemap(ILogger<GetSitemap> logger, IDomainRegistrationRepository domainRegistrationRepository)
    {
        _logger = logger;
        _domainRegistrationRepository = domainRegistrationRepository;
    }

    [Function("GetSitemap")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sitemap.xml/{topLevelDomain}/{secondLevelDomain}")] HttpRequest req,
        string topLevelDomain,
        string secondLevelDomain)
    {
        _logger.LogInformation($"Generating sitemap for TLD: {topLevelDomain}, SLD: {secondLevelDomain}");

        try
        {
            // Get domain registration to retrieve last update information
            var domainRegistration = await _domainRegistrationRepository.GetByDomainAsync(topLevelDomain, secondLevelDomain);
            
            if (domainRegistration == null)
            {
                _logger.LogWarning($"Domain registration not found for {secondLevelDomain}.{topLevelDomain}");
                return new NotFoundObjectResult($"Domain registration not found for {secondLevelDomain}.{topLevelDomain}");
            }

            // Build the full domain URL
            var domainUrl = $"https://{secondLevelDomain}.{topLevelDomain}";
            
            // Generate sitemap XML
            var sitemapXml = GenerateSitemap(domainUrl, domainRegistration.LastUpdatedAt);
            
            // Return with appropriate content type
            return new ContentResult
            {
                Content = sitemapXml,
                ContentType = "application/xml",
                StatusCode = 200
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating sitemap for {secondLevelDomain}.{topLevelDomain}");
            return new StatusCodeResult(500);
        }
    }

    private static string GenerateSitemap(string domainUrl, DateTime lastUpdated)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        
        var urlset = new XElement(ns + "urlset",
            new XElement(ns + "url",
                new XElement(ns + "loc", domainUrl),
                new XElement(ns + "lastmod", lastUpdated.ToString("yyyy-MM-dd")),
                new XElement(ns + "changefreq", "weekly"),
                new XElement(ns + "priority", "1.0")
            )
        );

        var declaration = new XDeclaration("1.0", "UTF-8", null);
        var document = new XDocument(declaration, urlset);

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(declaration.ToString());
        stringBuilder.Append(document.Root?.ToString());

        return stringBuilder.ToString();
    }
}
