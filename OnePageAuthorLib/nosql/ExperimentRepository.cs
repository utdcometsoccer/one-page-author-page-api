using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository implementation for managing Experiment entities in Cosmos DB.
    /// </summary>
    public class ExperimentRepository : IExperimentRepository
    {
        private readonly Container _container;
        private readonly ILogger<ExperimentRepository>? _logger;

        public ExperimentRepository(Container container, ILogger<ExperimentRepository>? logger = null)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<List<Experiment>> GetActiveExperimentsByPageAsync(string page)
        {
            if (string.IsNullOrWhiteSpace(page))
                throw new ArgumentException("Page cannot be null or empty.", nameof(page));

            try
            {
                var query = new QueryDefinition(
                    "SELECT * FROM c WHERE c.Page = @page AND c.IsActive = true")
                    .WithParameter("@page", page);

                var iterator = _container.GetItemQueryIterator<Experiment>(
                    query,
                    requestOptions: new QueryRequestOptions
                    {
                        PartitionKey = new PartitionKey(page)
                    });

                var experiments = new List<Experiment>();
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    experiments.AddRange(response);
                }

                _logger?.LogInformation("Retrieved {Count} active experiments for page: {Page}", experiments.Count, page);
                return experiments;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving active experiments for page: {Page}", page);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Experiment?> GetByIdAsync(string id, string page)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be null or empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(page))
                throw new ArgumentException("Page cannot be null or empty.", nameof(page));

            try
            {
                var response = await _container.ReadItemAsync<Experiment>(
                    id,
                    new PartitionKey(page));

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger?.LogWarning("Experiment not found: {Id} on page: {Page}", id, page);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving experiment: {Id} on page: {Page}", id, page);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Experiment> CreateAsync(Experiment experiment)
        {
            if (experiment == null)
                throw new ArgumentNullException(nameof(experiment));

            if (string.IsNullOrWhiteSpace(experiment.id))
                experiment.id = Guid.NewGuid().ToString();

            experiment.CreatedAt = DateTime.UtcNow;
            experiment.UpdatedAt = DateTime.UtcNow;

            try
            {
                var response = await _container.CreateItemAsync(
                    experiment,
                    new PartitionKey(experiment.Page));

                _logger?.LogInformation("Created experiment: {Id} on page: {Page}", experiment.id, experiment.Page);
                return response.Resource;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating experiment: {Id} on page: {Page}", experiment.id, experiment.Page);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Experiment> UpdateAsync(Experiment experiment)
        {
            if (experiment == null)
                throw new ArgumentNullException(nameof(experiment));
            if (string.IsNullOrWhiteSpace(experiment.id))
                throw new ArgumentException("Experiment id cannot be null or empty.", nameof(experiment));

            experiment.UpdatedAt = DateTime.UtcNow;

            try
            {
                var response = await _container.ReplaceItemAsync(
                    experiment,
                    experiment.id,
                    new PartitionKey(experiment.Page));

                _logger?.LogInformation("Updated experiment: {Id} on page: {Page}", experiment.id, experiment.Page);
                return response.Resource;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating experiment: {Id} on page: {Page}", experiment.id, experiment.Page);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string id, string page)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be null or empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(page))
                throw new ArgumentException("Page cannot be null or empty.", nameof(page));

            try
            {
                await _container.DeleteItemAsync<Experiment>(
                    id,
                    new PartitionKey(page));

                _logger?.LogInformation("Deleted experiment: {Id} on page: {Page}", id, page);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting experiment: {Id} on page: {Page}", id, page);
                throw;
            }
        }
    }
}
