using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.Function;

public class GetAuthorData
{
    private const string HomepagePage = "homepage";
    private const string FeaturedBookHeroVariant = "featured-book-hero";
    private const string ControlVariant = "control";

    private readonly ILogger<GetAuthorData> _logger;
    private readonly IAuthorDataService _authorDataService;
    private readonly IExperimentService? _experimentService;

    public GetAuthorData(
        ILogger<GetAuthorData> logger,
        IAuthorDataService authorDataService,
        IExperimentService? experimentService = null)
    {
        _logger = logger;
        _authorDataService = authorDataService;
        _experimentService = experimentService;
    }

    [Function("GetAuthorData")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetAuthorData/{topLevelDomain}/{secondLevelDomain}/{languageName}/{regionName?}")] HttpRequest req,
        string topLevelDomain,
        string secondLevelDomain,
        string languageName,
        string? regionName)
    {
        _logger.LogInformation("Received request for TLD: {TopLevelDomain}, SLD: {SecondLevelDomain}, Language: {LanguageName}, Region: {RegionName}",
            topLevelDomain, secondLevelDomain, languageName, regionName);

        var result = await _authorDataService.GetHomepageDataAsync(topLevelDomain, secondLevelDomain, languageName, regionName);
        if (result == null)
        {
            return new NotFoundObjectResult("No author found for the specified domain and culture.");
        }

        result.Experiment = await ResolveExperimentAsync(req);

        return new OkObjectResult(result);
    }

    private async Task<HomepageExperimentDto> ResolveExperimentAsync(HttpRequest req)
    {
        if (_experimentService == null)
            return new HomepageExperimentDto { HomepageHeroVariant = ControlVariant };

        try
        {
            var bucketingKey = req.Headers["X-Session-Id"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            var experimentResponse = await _experimentService.GetExperimentsAsync(new GetExperimentsRequest
            {
                UserId = bucketingKey,
                Page = HomepagePage
            });

            var heroVariant = experimentResponse.Experiments
                .FirstOrDefault(e => e.Name == "homepage-hero")?.Variant
                ?? ControlVariant;

            // Only allow the known variant identifiers; treat anything else as control.
            if (heroVariant != FeaturedBookHeroVariant)
                heroVariant = ControlVariant;

            return new HomepageExperimentDto { HomepageHeroVariant = heroVariant };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Experiment assignment failed; defaulting to control variant.");
            return new HomepageExperimentDto { HomepageHeroVariant = ControlVariant };
        }
    }
}