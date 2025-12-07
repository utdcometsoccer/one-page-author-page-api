using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;
using Microsoft.Extensions.Logging;
using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    /// <summary>
    /// Service for finding subscriptions by customer email and domain name.
    /// </summary>
    public class FindSubscriptions : IFindSubscriptions
    {
        private readonly ILogger<FindSubscriptions> _logger;
        private readonly StripeClient _stripeClient;
        private readonly IStripeTelemetryService? _telemetryService;

        public FindSubscriptions(
            ILogger<FindSubscriptions> logger,
            StripeClient stripeClient,
            IStripeTelemetryService? telemetryService = null)
        {
            _logger = logger;
            _stripeClient = stripeClient ?? throw new ArgumentNullException(nameof(stripeClient));
            _telemetryService = telemetryService;
        }

        /// <summary>
        /// Finds subscriptions for a customer by email address and domain name.
        /// </summary>
        /// <param name="email">The customer's email address.</param>
        /// <param name="domainName">The domain name associated with the subscription.</param>
        /// <returns>A response containing matching subscriptions.</returns>
        public async Task<FindSubscriptionResponse> FindByEmailAndDomainAsync(string email, string domainName)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email is required", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(domainName))
            {
                throw new ArgumentException("DomainName is required", nameof(domainName));
            }

            _logger.LogInformation(
                "Finding subscriptions for email {Email} and domain {DomainName}",
                email, domainName);

            try
            {
                var response = new FindSubscriptionResponse
                {
                    CustomerFound = false,
                    Subscriptions = new List<SubscriptionDto>()
                };

                // Step 1: Find customer by email
                var customerService = new CustomerService(_stripeClient);
                var customers = await customerService.ListAsync(new CustomerListOptions
                {
                    Email = email,
                    Limit = 1
                });

                if (customers.Data.Count == 0)
                {
                    _logger.LogInformation("No customer found with email {Email}", email);
                    return response;
                }

                var customer = customers.Data[0];
                response.CustomerId = customer.Id;
                response.CustomerFound = true;

                _logger.LogInformation(
                    "Found customer {CustomerId} for email {Email}",
                    customer.Id, email);

                // Step 2: List all subscriptions for the customer
                var subscriptionService = new SubscriptionService(_stripeClient);
                var subscriptions = await subscriptionService.ListAsync(new SubscriptionListOptions
                {
                    Customer = customer.Id,
                    Limit = 100 // Get up to 100 subscriptions
                });

                _logger.LogInformation(
                    "Found {Count} total subscriptions for customer {CustomerId}",
                    subscriptions.Data.Count, customer.Id);

                // Step 3: Filter subscriptions by domain name in metadata
                foreach (var subscription in subscriptions.Data)
                {
                    if (subscription.Metadata != null &&
                        subscription.Metadata.TryGetValue("domain_name", out var metadataDomain) &&
                        string.Equals(metadataDomain, domainName, StringComparison.OrdinalIgnoreCase))
                    {
                        var subscriptionDto = MapSubscription(subscription);
                        response.Subscriptions.Add(subscriptionDto);
                        
                        _logger.LogInformation(
                            "Matched subscription {SubscriptionId} with domain {DomainName}",
                            subscription.Id, domainName);
                    }
                }

                _logger.LogInformation(
                    "Found {Count} subscriptions matching domain {DomainName}",
                    response.Subscriptions.Count, domainName);

                return response;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex,
                    "Stripe error finding subscriptions. Status={Status} Code={Code} Type={Type}",
                    ex.HttpStatusCode, ex.StripeError?.Code, ex.StripeError?.Type);
                _telemetryService?.TrackStripeError("FindSubscriptions", ex.StripeError?.Code, ex.StripeError?.Type, email);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error finding subscriptions for email {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Maps a Stripe Subscription to a SubscriptionDto.
        /// Uses SubscriptionMappers for consistent mapping.
        /// </summary>
        private SubscriptionDto MapSubscription(Subscription subscription)
        {
            // Leverage existing mapper to keep mapping logic consistent
            var subscriptions = new List<Subscription> { subscription };
            var mapped = SubscriptionMappers.Map(subscriptions, false);
            return mapped.Items.FirstOrDefault() ?? new SubscriptionDto
            {
                Id = subscription.Id,
                Status = subscription.Status ?? string.Empty,
                CustomerId = subscription.CustomerId ?? string.Empty
            };
        }
    }
}
