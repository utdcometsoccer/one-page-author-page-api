using Microsoft.Extensions.Logging;
using Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    /// <summary>
    /// Basic implementation that builds an initialized response object from the request.
    /// </summary>
    /// <remarks>
    /// This class does not perform any remote Stripe calls. It simply maps the request into an initialized
    /// <see cref="CreateCustomerResponse"/> instance. Upstream orchestrators can replace or decorate this
    /// implementation to invoke the actual Stripe SDK.
    /// </remarks>
    public class CreateCustomer : ICreateCustomer
    {
        // Logger used to capture operational and diagnostic information for this service
        private readonly ILogger<CreateCustomer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateCustomer"/> class.
        /// </summary>
        /// <param name="logger">The logger used for diagnostics and operational telemetry.</param>
        public CreateCustomer(ILogger<CreateCustomer> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Creates an initialized <see cref="CreateCustomerResponse"/> from the provided request.
        /// </summary>
        /// <param name="request">The input payload. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
        /// <returns>An initialized response with a <see cref="Customer"/> containing the provided email.</returns>
        public CreateCustomerResponse Execute(CreateCustomerRequest request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                // Create the customer service
                // First, check if a customer with this email already exists
                var service = new CustomerService();

                // First, check if a customer with this email already exists
                var existingCustomer = service.List(new CustomerListOptions
                {
                    Email = request.Email
                }).FirstOrDefault();

                if (existingCustomer != null)
                {
                    _logger.LogInformation("Customer with email {Email} already exists.", request.Email);
                    return new CreateCustomerResponse
                    {
                        Customer = existingCustomer
                    };
                }

                // Initialize a Stripe.Customer with the data we have at this layer.
                var options = new CustomerCreateOptions
                {
                    Email = request.Email
                };




                // Create the actual Stripe customer
                var customer = service.Create(options);

                return new CreateCustomerResponse
                {
                    Customer = customer
                };
            }
            catch (StripeException ex)
            {
                // Log Stripe-specific failures with available context
                _logger.LogError(ex,
                    "Stripe error while creating customer. StatusCode={StatusCode}, Code={StripeCode}, Type={StripeType}",
                    ex.HttpStatusCode,
                    ex.StripeError?.Code,
                    ex.StripeError?.Type);
                throw;
            }
            catch (Exception ex)
            {
                // Log unexpected failures
                _logger.LogError(ex, "Unexpected error while creating Stripe customer.");
                throw;
            }
            finally
            {
                // No resources to dispose currently. Reserved for future cleanup/telemetry hooks.
            }
        }
    }
}
