using System;
using Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Defines a contract for creating Stripe customers from a request payload.
    /// </summary>
    [Obsolete("Use InkStainedWretch.OnePageAuthorLib.API.Stripe.ICreateCustomer in interfaces/Stripe instead.")]
    public interface ICreateStripeCustomer
    {
        /// <summary>
        /// Executes the create-customer operation.
        /// </summary>
        /// <param name="request">The input containing customer details (at minimum the email address).</param>
        /// <returns>
        /// An initialized <see cref="CreateCustomerResponse"/>. The <see cref="Customer"/> field will be
        /// populated with any data mapped from the request that is available at this layer.
        /// </returns>
        CreateCustomerResponse Execute(CreateCustomerRequest request);
    }
}