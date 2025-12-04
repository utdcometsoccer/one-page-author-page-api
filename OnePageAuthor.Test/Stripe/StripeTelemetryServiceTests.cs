using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging.Abstractions;

namespace OnePageAuthor.Test.Stripe
{
    public class StripeTelemetryServiceTests
    {
        private class TestTelemetryChannel : ITelemetryChannel
        {
            public List<ITelemetry> SentItems { get; } = new List<ITelemetry>();
            public bool? DeveloperMode { get; set; }
            public string EndpointAddress { get; set; } = string.Empty;
            public void Dispose() { }
            public void Flush() { }
            public void Send(ITelemetry item) => SentItems.Add(item);
        }

        private static (StripeTelemetryService service, TestTelemetryChannel channel) CreateService()
        {
            var channel = new TestTelemetryChannel();
            var config = new TelemetryConfiguration
            {
                TelemetryChannel = channel,
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
            };
            var client = new TelemetryClient(config);
            var service = new StripeTelemetryService(client, new NullLogger<StripeTelemetryService>());
            return (service, channel);
        }

        [Fact]
        public void TrackCustomerCreated_SendsEventWithCorrectProperties()
        {
            var (service, channel) = CreateService();

            service.TrackCustomerCreated("cus_123", "test@example.com");

            Assert.Single(channel.SentItems);
            var eventTelemetry = Assert.IsType<EventTelemetry>(channel.SentItems[0]);
            Assert.Equal("StripeCustomerCreated", eventTelemetry.Name);
            Assert.Equal("cus_123", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("example.com", eventTelemetry.Properties["EmailDomain"]);
            Assert.True(eventTelemetry.Properties.ContainsKey("Timestamp"));
        }

        [Fact]
        public void TrackCustomerCreated_WithoutEmail_DoesNotIncludeEmailDomain()
        {
            var (service, channel) = CreateService();

            service.TrackCustomerCreated("cus_456", null);

            Assert.Single(channel.SentItems);
            var eventTelemetry = Assert.IsType<EventTelemetry>(channel.SentItems[0]);
            Assert.Equal("StripeCustomerCreated", eventTelemetry.Name);
            Assert.Equal("cus_456", eventTelemetry.Properties["CustomerId"]);
            Assert.False(eventTelemetry.Properties.ContainsKey("EmailDomain"));
        }

        [Fact]
        public void TrackCheckoutSessionCreated_SendsEventWithCorrectProperties()
        {
            var (service, channel) = CreateService();

            service.TrackCheckoutSessionCreated("cs_test_123", "cus_abc", "price_xyz");

            Assert.Single(channel.SentItems);
            var eventTelemetry = Assert.IsType<EventTelemetry>(channel.SentItems[0]);
            Assert.Equal("StripeCheckoutSessionCreated", eventTelemetry.Name);
            Assert.Equal("cs_test_123", eventTelemetry.Properties["CheckoutSessionId"]);
            Assert.Equal("cus_abc", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("price_xyz", eventTelemetry.Properties["PriceId"]);
        }

        [Fact]
        public void TrackCheckoutSessionRetrieved_SendsEventWithStatusInfo()
        {
            var (service, channel) = CreateService();

            service.TrackCheckoutSessionRetrieved("cs_test_456", "cus_def", "complete", "paid");

            Assert.Single(channel.SentItems);
            var eventTelemetry = Assert.IsType<EventTelemetry>(channel.SentItems[0]);
            Assert.Equal("StripeCheckoutSessionRetrieved", eventTelemetry.Name);
            Assert.Equal("cs_test_456", eventTelemetry.Properties["CheckoutSessionId"]);
            Assert.Equal("cus_def", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("complete", eventTelemetry.Properties["Status"]);
            Assert.Equal("paid", eventTelemetry.Properties["PaymentStatus"]);
        }

        [Fact]
        public void TrackSubscriptionCreated_SendsEventWithCorrectProperties()
        {
            var (service, channel) = CreateService();

            service.TrackSubscriptionCreated("sub_123", "cus_abc", "price_xyz");

            Assert.Single(channel.SentItems);
            var eventTelemetry = Assert.IsType<EventTelemetry>(channel.SentItems[0]);
            Assert.Equal("StripeSubscriptionCreated", eventTelemetry.Name);
            Assert.Equal("sub_123", eventTelemetry.Properties["SubscriptionId"]);
            Assert.Equal("cus_abc", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("price_xyz", eventTelemetry.Properties["PriceId"]);
        }

        [Fact]
        public void TrackSubscriptionCancelled_SendsEventWithCorrectProperties()
        {
            var (service, channel) = CreateService();

            service.TrackSubscriptionCancelled("sub_456", "cus_def");

            Assert.Single(channel.SentItems);
            var eventTelemetry = Assert.IsType<EventTelemetry>(channel.SentItems[0]);
            Assert.Equal("StripeSubscriptionCancelled", eventTelemetry.Name);
            Assert.Equal("sub_456", eventTelemetry.Properties["SubscriptionId"]);
            Assert.Equal("cus_def", eventTelemetry.Properties["CustomerId"]);
        }

        [Fact]
        public void TrackSubscriptionUpdated_SendsEventWithCorrectProperties()
        {
            var (service, channel) = CreateService();

            service.TrackSubscriptionUpdated("sub_789", "cus_ghi", "price_new");

            Assert.Single(channel.SentItems);
            var eventTelemetry = Assert.IsType<EventTelemetry>(channel.SentItems[0]);
            Assert.Equal("StripeSubscriptionUpdated", eventTelemetry.Name);
            Assert.Equal("sub_789", eventTelemetry.Properties["SubscriptionId"]);
            Assert.Equal("cus_ghi", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("price_new", eventTelemetry.Properties["NewPriceId"]);
        }

        [Fact]
        public void TrackSubscriptionsListed_SendsEventWithCountMetric()
        {
            var (service, channel) = CreateService();

            service.TrackSubscriptionsListed("cus_list", 5);

            Assert.Single(channel.SentItems);
            var eventTelemetry = Assert.IsType<EventTelemetry>(channel.SentItems[0]);
            Assert.Equal("StripeSubscriptionsListed", eventTelemetry.Name);
            Assert.Equal("cus_list", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal(5, eventTelemetry.Metrics["SubscriptionCount"]);
        }

        [Fact]
        public void TrackWebhookEvent_SendsEventWithAllProperties()
        {
            var (service, channel) = CreateService();

            service.TrackWebhookEvent(
                eventType: "invoice.paid",
                objectId: "in_123",
                customerId: "cus_abc",
                subscriptionId: "sub_xyz",
                invoiceId: "in_123",
                paymentIntentId: "pi_456",
                priceId: "price_789");

            Assert.Single(channel.SentItems);
            var eventTelemetry = Assert.IsType<EventTelemetry>(channel.SentItems[0]);
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
            var (service, channel) = CreateService();

            service.TrackWebhookEvent(
                eventType: "customer.created",
                objectId: "cus_new");

            Assert.Single(channel.SentItems);
            var eventTelemetry = Assert.IsType<EventTelemetry>(channel.SentItems[0]);
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
            var (service, channel) = CreateService();

            service.TrackInvoicePreview("cus_preview", "sub_123", "price_new");

            Assert.Single(channel.SentItems);
            var eventTelemetry = Assert.IsType<EventTelemetry>(channel.SentItems[0]);
            Assert.Equal("StripeInvoicePreview", eventTelemetry.Name);
            Assert.Equal("cus_preview", eventTelemetry.Properties["CustomerId"]);
            Assert.Equal("sub_123", eventTelemetry.Properties["SubscriptionId"]);
            Assert.Equal("price_new", eventTelemetry.Properties["NewPriceId"]);
        }

        [Fact]
        public void TrackStripeError_SendsEventWithErrorDetails()
        {
            var (service, channel) = CreateService();

            service.TrackStripeError("CreateSubscription", "card_declined", "card_error", "cus_error");

            Assert.Single(channel.SentItems);
            var eventTelemetry = Assert.IsType<EventTelemetry>(channel.SentItems[0]);
            Assert.Equal("StripeApiError", eventTelemetry.Name);
            Assert.Equal("CreateSubscription", eventTelemetry.Properties["Operation"]);
            Assert.Equal("card_declined", eventTelemetry.Properties["ErrorCode"]);
            Assert.Equal("card_error", eventTelemetry.Properties["ErrorType"]);
            Assert.Equal("cus_error", eventTelemetry.Properties["CustomerId"]);
        }

        [Fact]
        public void TrackStripeError_WithNullOptionalParameters_DoesNotIncludeThem()
        {
            var (service, channel) = CreateService();

            service.TrackStripeError("UnknownOperation");

            Assert.Single(channel.SentItems);
            var eventTelemetry = Assert.IsType<EventTelemetry>(channel.SentItems[0]);
            Assert.Equal("StripeApiError", eventTelemetry.Name);
            Assert.Equal("UnknownOperation", eventTelemetry.Properties["Operation"]);
            Assert.False(eventTelemetry.Properties.ContainsKey("ErrorCode"));
            Assert.False(eventTelemetry.Properties.ContainsKey("ErrorType"));
            Assert.False(eventTelemetry.Properties.ContainsKey("CustomerId"));
        }

        [Fact]
        public void AllEvents_IncludeTimestamp()
        {
            var (service, channel) = CreateService();

            service.TrackCustomerCreated("cus_ts");
            service.TrackCheckoutSessionCreated("cs_ts", "cus_ts");
            service.TrackSubscriptionCreated("sub_ts", "cus_ts");
            service.TrackWebhookEvent("test.event", "obj_ts");
            service.TrackStripeError("TestOp");

            Assert.Equal(5, channel.SentItems.Count);
            foreach (var item in channel.SentItems)
            {
                var eventTelemetry = Assert.IsType<EventTelemetry>(item);
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
            var config = new TelemetryConfiguration
            {
                TelemetryChannel = new TestTelemetryChannel(),
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
            };
            var client = new TelemetryClient(config);

            Assert.Throws<ArgumentNullException>(() =>
                new StripeTelemetryService(client, null!));
        }
    }
}
