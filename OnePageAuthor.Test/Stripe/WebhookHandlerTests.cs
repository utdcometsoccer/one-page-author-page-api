using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using Microsoft.Extensions.Logging.Abstractions;

namespace OnePageAuthor.Test.Stripe
{
    public class WebhookHandlerTests
    {
        private class StubSecretProvider : InkStainedWretch.OnePageAuthorLib.API.Stripe.IStripeWebhookSecretProvider
        {
            private readonly string _secret;
            public StubSecretProvider(string secret) { _secret = secret; }
            public string GetWebhookSecret() => _secret;
        }

        private static string ComputeStripeSig(string secret, string payload, long ts)
        {
            var signed = $"{ts}.{payload}";
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signed));
            var sig = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            return $"t={ts},v1={sig}";
        }
        [Fact]
        public async Task Returns_Error_When_Payload_Empty()
        {
            var handler = new StripeWebhookHandler(new NullLogger<StripeWebhookHandler>(), new StubSecretProvider("whsec_test"));
            var result = await handler.HandleAsync(null, "sig");
            Assert.False(result.Success);
            Assert.Contains("Empty payload", result.Message);
        }

        [Fact]
        public async Task Returns_Error_When_Signature_Missing()
        {
            var handler = new StripeWebhookHandler(new NullLogger<StripeWebhookHandler>(), new StubSecretProvider("whsec_test"));
            var result = await handler.HandleAsync("{ }", null);
            Assert.False(result.Success);
            Assert.Contains("Missing Stripe-Signature", result.Message);
        }

        [Fact]
        public async Task Success_When_Payload_And_Signature_Present()
        {
            var secret = "whsec_test";
            var payload = "{\"type\":\"invoice.payment_succeeded\"}";
            var ts = 123L;
            var sig = ComputeStripeSig(secret, payload, ts);
            var handler = new StripeWebhookHandler(new NullLogger<StripeWebhookHandler>(), new StubSecretProvider(secret));
            var result = await handler.HandleAsync(payload, sig);
            Assert.True(result.Success);
            Assert.StartsWith("Unhandled:", result.Message);
        }

        [Fact]
        public async Task Fails_When_Signature_Invalid()
        {
            var secret = "whsec_test";
            var payload = "{\"type\":\"invoice.paid\"}";
            var sig = "t=100,v1=deadbeef";
            var handler = new StripeWebhookHandler(new NullLogger<StripeWebhookHandler>(), new StubSecretProvider(secret));
            var result = await handler.HandleAsync(payload, sig);
            Assert.False(result.Success);
            Assert.Contains("Invalid signature", result.Message);
        }

        [Fact]
        public async Task Fails_When_Timestamp_Expired()
        {
            var secret = "whsec_test";
            var payload = "{\"type\":\"invoice.paid\"}";
            // Create a timestamp older than 10 minutes to bypass 5-minute tolerance
            var oldTs = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
            var sig = ComputeStripeSig(secret, payload, oldTs);
            var handler = new StripeWebhookHandler(new NullLogger<StripeWebhookHandler>(), new StubSecretProvider(secret));
            var result = await handler.HandleAsync(payload, sig);
            Assert.False(result.Success);
            Assert.Contains("Invalid signature", result.Message);
        }

        [Theory]
        [InlineData("invoice.paid", "{ \"id\": \"in_1\" }", "invoice.paid: in_1")]
        [InlineData("invoice.payment_failed", "{ \"id\": \"in_2\" }", "invoice.payment_failed: in_2")]
        [InlineData("invoice.finalized", "{ \"id\": \"in_3\" }", "invoice.finalized: in_3")]
        [InlineData("customer.subscription.deleted", "{ \"id\": \"sub_1\" }", "customer.subscription.deleted: sub_1")]
        [InlineData("customer.subscription.trial_will_end", "{ \"id\": \"sub_2\" }", "customer.subscription.trial_will_end: sub_2")]
        public async Task Handles_Known_Events(string eventType, string objectJson, string expectedMessage)
        {
            var secret = "whsec_test";
            var payload = $"{{ \"type\": \"{eventType}\", \"data\": {{ \"object\": {objectJson} }} }}";
            var sig = ComputeStripeSig(secret, payload, 100);
            var handler = new StripeWebhookHandler(new NullLogger<StripeWebhookHandler>(), new StubSecretProvider(secret));
            var result = await handler.HandleAsync(payload, sig);
            Assert.True(result.Success);
            Assert.Equal(expectedMessage, result.Message);
        }

        [Fact]
        public async Task Handles_Unknown_Event_As_Unhandled()
        {
            var secret = "whsec_test";
            var handler = new StripeWebhookHandler(new NullLogger<StripeWebhookHandler>(), new StubSecretProvider(secret));
            var payload = "{ \"type\": \"product.created\", \"data\": { \"object\": { \"id\": \"prod_1\" } } }";
            var sig = ComputeStripeSig(secret, payload, 200);
            var result = await handler.HandleAsync(payload, sig);
            Assert.True(result.Success);
            Assert.StartsWith("Unhandled:", result.Message);
        }

        [Fact]
        public async Task Extracts_CustomerId_From_String_Field()
        {
            var secret = "whsec_test";
            var handler = new StripeWebhookHandler(new NullLogger<StripeWebhookHandler>(), new StubSecretProvider(secret));
            var payload = "{ \"type\": \"invoice.paid\", \"data\": { \"object\": { \"id\": \"in_42\", \"customer\": \"cus_abc\" } } }";
            var sig = ComputeStripeSig(secret, payload, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            var result = await handler.HandleAsync(payload, sig);
            Assert.True(result.Success);
            Assert.Equal("cus_abc", result.CustomerId);
        }

        [Fact]
        public async Task Extracts_CustomerId_From_Object_Field()
        {
            var secret = "whsec_test";
            var handler = new StripeWebhookHandler(new NullLogger<StripeWebhookHandler>(), new StubSecretProvider(secret));
            var payload = "{ \"type\": \"invoice.paid\", \"data\": { \"object\": { \"id\": \"in_43\", \"customer\": { \"id\": \"cus_xyz\" } } } }";
            var sig = ComputeStripeSig(secret, payload, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            var result = await handler.HandleAsync(payload, sig);
            Assert.True(result.Success);
            Assert.Equal("cus_xyz", result.CustomerId);
        }
    }
}
