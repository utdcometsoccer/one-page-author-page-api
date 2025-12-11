using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using OnePageAuthorLib.Api.Stripe;
using OnePageAuthorLib.Interfaces.Stripe;
using Stripe;

namespace OnePageAuthor.Test
{
    public class StripeExtractorTests
    {
        [Fact]
        public async Task Orchestrator_Returns_ClientSecret_From_Hydrated_PaymentIntent()
        {
            var logger = Mock.Of<ILogger<ClientSecretFromInvoice>>();
            var pi = new PaymentIntent { ClientSecret = "cs_test_123" };
            var extractor = new Mock<IInvoicePaymentIntentExtractor>();
            extractor.Setup(x => x.ExtractPaymentIntentAsync(It.IsAny<Invoice>()))
                     .ReturnsAsync(pi);
            var secretExtractor = new Mock<IPaymentIntentClientSecretExtractor>();
            secretExtractor.Setup(x => x.Extract(pi)).Returns("cs_test_123");

            var orchestrator = new ClientSecretFromInvoice(logger, secretExtractor.Object, extractor.Object);
            var invoice = new Invoice { Id = "in_test", Payments = new StripeList<InvoicePayment> { Data = new System.Collections.Generic.List<InvoicePayment> { new InvoicePayment() } } };

            var secret = await orchestrator.ExtractAsync(invoice);
            Assert.Equal("cs_test_123", secret);
        }

        [Fact]
        public async Task Orchestrator_Throws_When_No_ClientSecret()
        {
            var logger = Mock.Of<ILogger<ClientSecretFromInvoice>>();
            var pi = new PaymentIntent { ClientSecret = null };
            var extractor = new Mock<IInvoicePaymentIntentExtractor>();
            extractor.Setup(x => x.ExtractPaymentIntentAsync(It.IsAny<Invoice>()))
                     .ReturnsAsync(pi);
            var secretExtractor = new Mock<IPaymentIntentClientSecretExtractor>();
            secretExtractor.Setup(x => x.Extract(pi)).Returns((string)null!);

            var orchestrator = new ClientSecretFromInvoice(logger, secretExtractor.Object, extractor.Object);
            var invoice = new Invoice { Id = "in_test", Payments = new StripeList<InvoicePayment> { Data = new System.Collections.Generic.List<InvoicePayment> { new InvoicePayment() } } };

            await Assert.ThrowsAsync<InvalidOperationException>(() => orchestrator.ExtractAsync(invoice));
        }

        [Fact]
        public async Task Orchestrator_Throws_When_PaymentIntent_Not_Extracted()
        {
            var logger = Mock.Of<ILogger<ClientSecretFromInvoice>>();
            var extractor = new Mock<IInvoicePaymentIntentExtractor>();
            extractor.Setup(x => x.ExtractPaymentIntentAsync(It.IsAny<Invoice>()))
                     .ReturnsAsync((PaymentIntent?)null);
            var secretExtractor = new Mock<IPaymentIntentClientSecretExtractor>();

            var orchestrator = new ClientSecretFromInvoice(logger, secretExtractor.Object, extractor.Object);
            var invoice = new Invoice { Id = "in_test" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => orchestrator.ExtractAsync(invoice));
        }
    }
}