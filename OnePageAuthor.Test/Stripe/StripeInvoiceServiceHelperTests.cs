using System.Net;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using Microsoft.Extensions.Logging.Abstractions;

namespace OnePageAuthor.Test.Stripe
{
    public class StripeInvoiceServiceHelperTests
    {
        private class StubApiKeyProvider : IStripeApiKeyProvider
        {
            private readonly string _key;
            public StubApiKeyProvider(string key) => _key = key;
            public string GetApiKey() => _key;
        }
        private class StubHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;
            public StubHandler(HttpResponseMessage response) => _response = response;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_response);
        }

        [Fact]
        public async Task TryGetPaymentIntentAsync_Parses_Id_And_ClientSecret()
        {
            var json = "{\n  \"payment_intent\": { \"id\": \"pi_123\", \"client_secret\": \"cs_test_abc\" }\n}";
            var http = new HttpClient(new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            }));

            var helper = new StripeInvoiceServiceHelper(new NullLogger<StripeInvoiceServiceHelper>(), http, new StubApiKeyProvider("test_key_placeholder"));
            var result = await helper.TryGetPaymentIntentAsync("in_123");

            Assert.Equal("pi_123", result.paymentIntentId);
            Assert.Equal("cs_test_abc", result.clientSecret);
        }

        [Fact]
        public async Task TryGetPaymentIntentAsync_Returns_Empty_On_Error()
        {
            var http = new HttpClient(new StubHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)));
            var helper = new StripeInvoiceServiceHelper(new NullLogger<StripeInvoiceServiceHelper>(), http, new StubApiKeyProvider("test_key_placeholder"));
            var result = await helper.TryGetPaymentIntentAsync("in_does_not_exist");
            // Without API key and real network, helper safely returns empty strings
            Assert.Equal(string.Empty, result.paymentIntentId);
            Assert.Equal(string.Empty, result.clientSecret);
        }
    }
}
