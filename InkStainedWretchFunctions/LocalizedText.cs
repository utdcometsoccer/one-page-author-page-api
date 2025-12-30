using InkStainedWretch.OnePageAuthorAPI.Interfaces.Authormanagement;
using InkStainedWretch.OnePageAuthorLib.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;


namespace InkStainedWretchFunctions;

/// <summary>
/// Azure Function HTTP endpoint that returns aggregated localized UI text
/// for a supplied culture code using <see cref="ILocalizationTextProvider"/>.
/// </summary>
public class LocalizedText
{
    private readonly ILogger<LocalizedText> _logger;
    private readonly ILocalizationTextProvider _provider;

    /// <summary>
    /// Creates a new <see cref="LocalizedText"/> function handler.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="provider">Localization text provider service.</param>
    public LocalizedText(ILogger<LocalizedText> logger, ILocalizationTextProvider provider)
    {
        _logger = logger;
        _provider = provider;
    }

    /// <summary>
    /// Handles HTTP GET requests for localized text.
    /// </summary>
    /// <param name="req">The incoming HTTP request.</param>
    /// <param name="culture">Route parameter representing the culture (e.g. en-US).</param>
    /// <returns>200 with JSON payload of localized text; standardized error response on failure.</returns>
    [Function("LocalizedText")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "localizedtext/{culture}")] HttpRequestData req,
        string culture)
    {
        _logger.LogInformation($"Received request for culture: {culture}");
        try
        {
            var result = await _provider.GetLocalizationTextAsync(culture);
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (System.Exception ex)
        {
            return await req.HandleExceptionAsync(ex, _logger);
        }
    }
}
