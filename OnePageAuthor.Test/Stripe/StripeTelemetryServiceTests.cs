using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging.Abstractions;

namespace OnePageAuthor.Test.Stripe
{
    public class StripeTelemetryServiceTests
    {
        private sealed class TestStripeTelemetryService : StripeTelemetryService
        {
            private readonly List<EventTelemetry> _sentItems;

            public TestStripeTelemetryService(TelemetryClient telemetryClient, List<EventTelemetry> sentItems)
                : base(telemetryClient, new NullLogger<StripeTelemetryService>())
            {
                _sentItems = sentItems;
            }

            protected internal override void TrackEvent(EventTelemetry telemetry)
            {
                _sentItems.Add(telemetry);
            }
        }

        private sealed class ServiceHelper : IDisposable
        {
            private readonly TelemetryConfiguration _configuration;
            public StripeTelemetryService Service { get; }
            public List<EventTelemetry> SentItems { get; }

            public ServiceHelper()
            {
                SentItems = new List<EventTelemetry>();
                _configuration = new TelemetryConfiguration
                {
                    ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
                };
                var client = new TelemetryClient(_configuration);
                Service = new TestStripeTelemetryService(client, SentItems);
            }

            public void Dispose()
            {
                _configuration?.Dispose();
            }
        }

        [Fact]
        public void TrackCustomerCreated_SendsEventWithCorrectProperties()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackCustomerCreated("cus_123", "test@example.com");

            Assert.Single(helper.SentItems);
            var eventTelemetry = helper.SentItems[0];
            Assert.Equal("StripeCustomerCreated", eventTelemetry.Name);
            Assert.Equal("cus_123", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("example.com", eventTelemetry.Properties["EmailDomain"]);
            Assert.True(eventTelemetry.Properties.ContainsKey("Timestamp"));
        }

        [Fact]
        public void TrackCustomerCreated_WithoutEmail_DoesNotIncludeEmailDomain()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackCustomerCreated("cus_456", null);

            Assert.Single(helper.SentItems);
            var eventTelemetry = helper.SentItems[0];
            Assert.Equal("StripeCustomerCreated", eventTelemetry.Name);
            Assert.Equal("cus_456", eventTelemetry.Properties["CustomerId"]);
            Assert.False(eventTelemetry.Properties.ContainsKey("EmailDomain"));
        }

        [Fact]
        public void TrackCheckoutSessionCreated_SendsEventWithCorrectProperties()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackCheckoutSessionCreated("cs_test_123", "cus_abc", "price_xyz");

            Assert.Single(helper.SentItems);
            var eventTelemetry = helper.SentItems[0];
            Assert.Equal("StripeCheckoutSessionCreated", eventTelemetry.Name);
            Assert.Equal("cs_test_123", eventTelemetry.Properties["CheckoutSessionId"]);
            Assert.Equal("cus_abc", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("price_xyz", eventTelemetry.Properties["PriceId"]);
        }

        [Fact]
        public void TrackCheckoutSessionRetrieved_SendsEventWithStatusInfo()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackCheckoutSessionRetrieved("cs_test_456", "cus_def", "complete", "paid");

            Assert.Single(helper.SentItems);
            var eventTelemetry = helper.SentItems[0];
            Assert.Equal("StripeCheckoutSessionRetrieved", eventTelemetry.Name);
            Assert.Equal("cs_test_456", eventTelemetry.Properties["CheckoutSessionId"]);
            Assert.Equal("cus_def", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("complete", eventTelemetry.Properties["Status"]);
            Assert.Equal("paid", eventTelemetry.Properties["PaymentStatus"]);
        }

        [Fact]
        public void TrackSubscriptionCreated_SendsEventWithCorrectProperties()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackSubscriptionCreated("sub_123", "cus_abc", "price_xyz");

            Assert.Single(helper.SentItems);
            var eventTelemetry = helper.SentItems[0];
            Assert.Equal("StripeSubscriptionCreated", eventTelemetry.Name);
            Assert.Equal("sub_123", eventTelemetry.Properties["SubscriptionId"]);
            Assert.Equal("cus_abc", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("price_xyz", eventTelemetry.Properties["PriceId"]);
        }

        [Fact]
        public void TrackSubscriptionCancelled_SendsEventWithCorrectProperties()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackSubscriptionCancelled("sub_456", "cus_def");

            Assert.Single(helper.SentItems);
            var eventTelemetry = helper.SentItems[0];
            Assert.Equal("StripeSubscriptionCancelled", eventTelemetry.Name);
            Assert.Equal("sub_456", eventTelemetry.Properties["SubscriptionId"]);
            Assert.Equal("cus_def", eventTelemetry.Properties["CustomerId"]);
        }

        [Fact]
        public void TrackSubscriptionUpdated_SendsEventWithCorrectProperties()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackSubscriptionUpdated("sub_789", "cus_ghi", "price_new");

            Assert.Single(helper.SentItems);
            var eventTelemetry = helper.SentItems[0];
            Assert.Equal("StripeSubscriptionUpdated", eventTelemetry.Name);
            Assert.Equal("sub_789", eventTelemetry.Properties["SubscriptionId"]);
            Assert.Equal("cus_ghi", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("price_new", eventTelemetry.Properties["NewPriceId"]);
        }

        [Fact]
        public void TrackSubscriptionsListed_SendsEventWithCountMetric()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackSubscriptionsListed("cus_list", 5);

            Assert.Single(helper.SentItems);
            var eventTelemetry = helper.SentItems[0];
            Assert.Equal("StripeSubscriptionsListed", eventTelemetry.Name);
            Assert.Equal("cus_list", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("5", eventTelemetry.Properties["SubscriptionCount"]);
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
            var eventTelemetry = helper.SentItems[0];
            Assert.Equal("StripeWebhookEvent", eventTelemetry.Name);
            Assert.Equal("invoice.paid", eventTelemetry.Properties["EventType"]);
            Assert.Equal("in_123", eventTelemetry.Properties["ObjectId"]);
            Assert.Equal("cus_abc", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("sub_xyz", eventTelemetry.Properties["SubscriptionId"]);
            Assert.Equal("in_123", eventTelemetry.Properties["InvoiceId"]);
            Assert.Equal("pi_456", eventTelemetry.Properties["PaymentIntentId"]);
            Assert.Equal("price_789", eventTelemetry.Properties["PriceId"]);
        }

        [Fact]
        public void TrackWebhookEvent_WithNullOptionalParameters_DoesNotIncludeThem()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackWebhookEvent(
                eventType: "customer.created",
                objectId: "cus_new");

            Assert.Single(helper.SentItems);
            var eventTelemetry = helper.SentItems[0];
            Assert.Equal("StripeWebhookEvent", eventTelemetry.Name);
            Assert.Equal("customer.created", eventTelemetry.Properties["EventType"]);
            Assert.Equal("cus_new", eventTelemetry.Properties["ObjectId"]);
            Assert.False(eventTelemetry.Properties.ContainsKey("CustomerId"));
            Assert.False(eventTelemetry.Properties.ContainsKey("SubscriptionId"));
            Assert.False(eventTelemetry.Properties.ContainsKey("InvoiceId"));
            Assert.False(eventTelemetry.Properties.ContainsKey("PaymentIntentId"));
            Assert.False(eventTelemetry.Properties.ContainsKey("PriceId"));
        }

        [Fact]
        public void TrackInvoicePreview_SendsEventWithCorrectProperties()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackInvoicePreview("cus_preview", "sub_123", "price_new");

            Assert.Single(helper.SentItems);
            var eventTelemetry = helper.SentItems[0];
            Assert.Equal("StripeInvoicePreview", eventTelemetry.Name);
            Assert.Equal("cus_preview", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("sub_123", eventTelemetry.Properties["SubscriptionId"]);
            Assert.Equal("price_new", eventTelemetry.Properties["NewPriceId"]);
        }

        [Fact]
        public void TrackStripeError_SendsEventWithErrorDetails()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackStripeError("CreateSubscription", "card_declined", "card_error", "cus_error");

            Assert.Single(helper.SentItems);
            var eventTelemetry = helper.SentItems[0];
            Assert.Equal("StripeApiError", eventTelemetry.Name);
            Assert.Equal("CreateSubscription", eventTelemetry.Properties["Operation"]);
            Assert.Equal("card_declined", eventTelemetry.Properties["ErrorCode"]);
            Assert.Equal("card_error", eventTelemetry.Properties["ErrorType"]);
            Assert.Equal("cus_error", eventTelemetry.Properties["CustomerId"]);
        }

        [Fact]
        public void TrackStripeError_WithNullOptionalParameters_DoesNotIncludeThem()
        {
            using var helper = new ServiceHelper();

            helper.Service.TrackStripeError("UnknownOperation");

            Assert.Single(helper.SentItems);
            var eventTelemetry = helper.SentItems[0];
            Assert.Equal("StripeApiError", eventTelemetry.Name);
            Assert.Equal("UnknownOperation", eventTelemetry.Properties["Operation"]);
            Assert.False(eventTelemetry.Properties.ContainsKey("ErrorCode"));
            Assert.False(eventTelemetry.Properties.ContainsKey("ErrorType"));
            Assert.False(eventTelemetry.Properties.ContainsKey("CustomerId"));
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
            foreach (var eventTelemetry in helper.SentItems)
            {
                Assert.True(eventTelemetry.Properties.ContainsKey("Timestamp"));
                // Verify timestamp is in ISO 8601 format
                Assert.True(DateTimeOffset.TryParse(eventTelemetry.Properties["Timestamp"], out _));
            }
        }

        [Fact]
        public void Constructor_ThrowsOnNullTelemetryClient()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StripeTelemetryService(null!, new NullLogger<StripeTelemetryService>()));
        }

        [Fact]
        public void Constructor_ThrowsOnNullLogger()
        {
            var configuration = new TelemetryConfiguration
            {
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
            };
            var client = new TelemetryClient(configuration);

            try
            {
                Assert.Throws<ArgumentNullException>(() =>
                    new StripeTelemetryService(client, null!));
            }
            finally
            {
                configuration?.Dispose();
            }
        }
    }
}
