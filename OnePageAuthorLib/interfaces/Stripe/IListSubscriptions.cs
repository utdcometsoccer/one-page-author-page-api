using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    /// <summary>
    /// Abstraction for listing Stripe subscriptions.
    /// </summary>
    public interface IListSubscriptions
    {
        /// <summary>
        /// Lists Stripe subscriptions with optional filters and pagination.
        /// </summary>
        /// <param name="customerId">Optional customer id filter.</param>
        /// <param name="status">Optional subscription status filter (e.g., active, trialing).</param>
        /// <param name="limit">Optional page size (default 100).</param>
        /// <param name="startingAfter">Cursor id for pagination.</param>
        /// <param name="expandLatestInvoicePaymentIntent">Whether to expand latest invoice payment intent.</param>
        /// <returns>A response containing the Stripe subscriptions list.</returns>
        Task<SubscriptionsResponse> ListAsync(
            string? customerId = null,
            string? status = null,
            int? limit = null,
            string? startingAfter = null,
            bool expandLatestInvoicePaymentIntent = false);
    }
}
