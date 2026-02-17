using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using Microsoft.Extensions.Logging.Abstractions;

namespace OnePageAuthor.Test.Stripe
{
    public class StripeTelemetryServiceTests
    {
        private sealed class TestStripeTelemetryService : StripeTelemetryService
        {
            private readonly List<(string EventName, IReadOnlyDictionary<string, object?> Properties)> _sentItems;

            public TestStripeTelemetryService(List<(string EventName, IReadOnlyDictionary<string, object?> Properties)> sentItems)
                : base(new NullLogger<StripeTelemetryService>())
            {
                _sentItems = sentItems;
            }

            protected internal override void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
            {
                _sentItems.Add((eventName, properties));
            }
        }

        private sealed class ServiceHelper : IDisposable
        {
            public StripeTelemetryService Service { get; }
            public List<(string EventName, IReadOnlyDictionary<string, object?> Properties)> SentItems { get; }

            public ServiceHelper()
            {
                SentItems = new List<(string EventName, IReadOnlyDictionary<string, object?> Properties)>();
                Service = new TestStripeTelemetryService(SentItems);
            }

            public void Dispose()
            {
            }
        }

        [Fact]
        public void TrackCustomerCreated_SendsEventWithCorrectProperties()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackCustomerCreated("cus_123", "test@example.com");

            Assert.Single(helper.SentItems);
            var item = helper.SentItems[0];
            Assert.Equal("StripeCustomerCreated", item.EventName);
            Assert.Equal("StripeCustomerCreated", item.Properties["EventName"]);
            Assert.Equal("cus_123", item.Properties["CustomerId"]);
            Assert.Equal("example.com", item.Properties["EmailDomain"]);
            Assert.True(item.Properties.ContainsKey("Timestamp"));
        }

        [Fact]
        public void TrackCustomerCreated_WithoutEmail_DoesNotIncludeEmailDomain()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackCustomerCreated("cus_456", null);

            Assert.Single(helper.SentItems);
            var item = helper.SentItems[0];
            Assert.Equal("StripeCustomerCreated", item.EventName);
            Assert.Equal("cus_456", item.Properties["CustomerId"]);
            Assert.False(item.Properties.ContainsKey("EmailDomain"));
        }

        [Fact]
        public void TrackCheckoutSessionCreated_SendsEventWithCorrectProperties()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackCheckoutSessionCreated("cs_test_123", "cus_abc", "price_xyz");

            Assert.Single(helper.SentItems);
            var item = helper.SentItems[0];
            Assert.Equal("StripeCheckoutSessionCreated", item.EventName);
            Assert.Equal("cs_test_123", item.Properties["CheckoutSessionId"]);
            Assert.Equal("cus_abc", item.Properties["CustomerId"]);
            Assert.Equal("price_xyz", item.Properties["PriceId"]);
        }

        [Fact]
        public void TrackCheckoutSessionRetrieved_SendsEventWithStatusInfo()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackCheckoutSessionRetrieved("cs_test_456", "cus_def", "complete", "paid");

            Assert.Single(helper.SentItems);
            var item = helper.SentItems[0];
            Assert.Equal("StripeCheckoutSessionRetrieved", item.EventName);
            Assert.Equal("cs_test_456", item.Properties["CheckoutSessionId"]);
            Assert.Equal("cus_def", item.Properties["CustomerId"]);
            Assert.Equal("complete", item.Properties["Status"]);
            Assert.Equal("paid", item.Properties["PaymentStatus"]);
        }

        [Fact]
        public void TrackSubscriptionCreated_SendsEventWithCorrectProperties()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackSubscriptionCreated("sub_123", "cus_abc", "price_xyz");

            Assert.Single(helper.SentItems);
            var item = helper.SentItems[0];
            Assert.Equal("StripeSubscriptionCreated", item.EventName);
            Assert.Equal("sub_123", item.Properties["SubscriptionId"]);
            Assert.Equal("cus_abc", item.Properties["CustomerId"]);
            Assert.Equal("price_xyz", item.Properties["PriceId"]);
        }

        [Fact]
        public void TrackSubscriptionCancelled_SendsEventWithCorrectProperties()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackSubscriptionCancelled("sub_456", "cus_def");

            Assert.Single(helper.SentItems);
            var item = helper.SentItems[0];
            Assert.Equal("StripeSubscriptionCancelled", item.EventName);
            Assert.Equal("sub_456", item.Properties["SubscriptionId"]);
            Assert.Equal("cus_def", item.Properties["CustomerId"]);
        }

        [Fact]
        public void TrackSubscriptionUpdated_SendsEventWithCorrectProperties()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackSubscriptionUpdated("sub_789", "cus_ghi", "price_new");

            Assert.Single(helper.SentItems);
            var item = helper.SentItems[0];
            Assert.Equal("StripeSubscriptionUpdated", item.EventName);
            Assert.Equal("sub_789", item.Properties["SubscriptionId"]);
            Assert.Equal("cus_ghi", item.Properties["CustomerId"]);
            Assert.Equal("price_new", item.Properties["NewPriceId"]);
        }

        [Fact]
        public void TrackSubscriptionsListed_SendsEventWithCountMetric()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackSubscriptionsListed("cus_list", 5);

            Assert.Single(helper.SentItems);
            var item = helper.SentItems[0];
            Assert.Equal("StripeSubscriptionsListed", item.EventName);
            Assert.Equal("cus_list", item.Properties["CustomerId"]);
            Assert.Equal("5", item.Properties["SubscriptionCount"]);
        }

        [Fact]
        public void TrackWebhookEvent_SendsEventWithAllProperties()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackWebhookEvent(
                eventType: "invoice.paid",
                objectId: "in_123",
                customerId: "cus_abc",
                subscriptionId: "sub_xyz",
                invoiceId: "in_123",
                paymentIntentId: "pi_456",
                priceId: "price_789");

            Assert.Single(helper.SentItems);
            var item = helper.SentItems[0];
            Assert.Equal("StripeWebhookEvent", item.EventName);
            Assert.Equal("invoice.paid", item.Properties["EventType"]);
            Assert.Equal("in_123", item.Properties["ObjectId"]);
            Assert.Equal("cus_abc", item.Properties["CustomerId"]);
            Assert.Equal("sub_xyz", item.Properties["SubscriptionId"]);
            Assert.Equal("in_123", item.Properties["InvoiceId"]);
            Assert.Equal("pi_456", item.Properties["PaymentIntentId"]);
            Assert.Equal("price_789", item.Properties["PriceId"]);
        }

        [Fact]
        public void TrackWebhookEvent_WithNullOptionalParameters_DoesNotIncludeThem()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackWebhookEvent(
                eventType: "customer.created",
                objectId: "cus_new");

            Assert.Single(helper.SentItems);
            var item = helper.SentItems[0];
            Assert.Equal("StripeWebhookEvent", item.EventName);
            Assert.Equal("customer.created", item.Properties["EventType"]);
            Assert.Equal("cus_new", item.Properties["ObjectId"]);
            Assert.False(item.Properties.ContainsKey("CustomerId"));
            Assert.False(item.Properties.ContainsKey("SubscriptionId"));
            Assert.False(item.Properties.ContainsKey("InvoiceId"));
            Assert.False(item.Properties.ContainsKey("PaymentIntentId"));
            Assert.False(item.Properties.ContainsKey("PriceId"));
        }

        [Fact]
        public void TrackInvoicePreview_SendsEventWithCorrectProperties()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackInvoicePreview("cus_preview", "sub_123", "price_new");

            Assert.Single(helper.SentItems);
            var item = helper.SentItems[0];
            Assert.Equal("StripeInvoicePreview", item.EventName);
            Assert.Equal("cus_preview", item.Properties["CustomerId"]);
            Assert.Equal("sub_123", item.Properties["SubscriptionId"]);
            Assert.Equal("price_new", item.Properties["NewPriceId"]);
        }

        [Fact]
        public void TrackStripeError_SendsEventWithErrorDetails()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackStripeError("CreateSubscription", "card_declined", "card_error", "cus_error");

            Assert.Single(helper.SentItems);
            var item = helper.SentItems[0];
            Assert.Equal("StripeApiError", item.EventName);
            Assert.Equal("CreateSubscription", item.Properties["Operation"]);
            Assert.Equal("card_declined", item.Properties["ErrorCode"]);
            Assert.Equal("card_error", item.Properties["ErrorType"]);
            Assert.Equal("cus_error", item.Properties["CustomerId"]);
        }

        [Fact]
        public void TrackStripeError_WithNullOptionalParameters_DoesNotIncludeThem()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackStripeError("UnknownOperation");

            Assert.Single(helper.SentItems);
            var item = helper.SentItems[0];
            Assert.Equal("StripeApiError", item.EventName);
            Assert.Equal("UnknownOperation", item.Properties["Operation"]);
            Assert.False(item.Properties.ContainsKey("ErrorCode"));
            Assert.False(item.Properties.ContainsKey("ErrorType"));
            Assert.False(item.Properties.ContainsKey("CustomerId"));
        }

        [Fact]
        public void AllEvents_IncludeTimestamp()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackCustomerCreated("cus_ts");
            helper.Service.TrackCheckoutSessionCreated("cs_ts", "cus_ts");
            helper.Service.TrackSubscriptionCreated("sub_ts", "cus_ts");
            helper.Service.TrackWebhookEvent("test.event", "obj_ts");
            helper.Service.TrackStripeError("TestOp");

            Assert.Equal(5, helper.SentItems.Count);
            foreach (var item in helper.SentItems)
            {
                Assert.True(item.Properties.ContainsKey("Timestamp"));
                Assert.True(DateTimeOffset.TryParse(item.Properties["Timestamp"]?.ToString(), out _));
            }
        }

        [Fact]
        public void Constructor_ThrowsOnNullLogger()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StripeTelemetryService(null!));
        }
    }
}
