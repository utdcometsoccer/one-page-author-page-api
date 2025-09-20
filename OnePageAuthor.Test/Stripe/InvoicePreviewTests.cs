using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Stripe;

namespace OnePageAuthor.Test.Stripe
{
    public class InvoicePreviewTests
    {
        [Fact]
        public void Mapper_Maps_Invoice_To_Dto()
        {
            var inv = new Invoice
            {
                Id = "in_test",
                Currency = "usd",
                AmountDue = 1234,
                Subtotal = 1000,
                Total = 1100,
                Lines = new StripeList<InvoiceLineItem>
                {
                    Data = new List<InvoiceLineItem>
                    {
                        new InvoiceLineItem
                        {
                            Description = "Test line",
                            Amount = 1100,
                            Currency = "usd",
                            Quantity = 1
                        }
                    }
                }
            };

            var dto = InvoicePreviewMappers.Map(inv);
            Assert.Equal("in_test", dto.InvoiceId);
            Assert.Equal("usd", dto.Currency);
            Assert.Equal(1234, dto.AmountDue);
            Assert.Equal(1000, dto.Subtotal);
            Assert.Equal(1100, dto.Total);
            Assert.Single(dto.Lines);
            var line = dto.Lines[0];
            Assert.Equal("Test line", line.Description);
            Assert.Equal(string.Empty, line.PriceId);
            Assert.Equal(1, line.Quantity);
            Assert.Equal(1100, line.Amount);
            Assert.Equal("usd", line.Currency);
        }

        [Fact]
        public void Decimal_Conversions_Work()
        {
            var dto = new InvoicePreviewResponse
            {
                AmountDue = 1234,
                Subtotal = 5678,
                Total = 9012
            };

            Assert.Equal(12.34m, dto.AmountDueDecimal);
            Assert.Equal(56.78m, dto.SubtotalDecimal);
            Assert.Equal(90.12m, dto.TotalDecimal);
        }

                [Fact]
                        public void Rest_Mapper_Extracts_Line_PriceId()
                {
                                var json = @"{
            ""id"": ""in_test"",
            ""currency"": ""usd"",
            ""amount_due"": 500,
            ""subtotal"": 400,
            ""total"": 500,
            ""lines"": {
                ""data"": [
                    {
                        ""description"": ""Line A"",
                        ""quantity"": 1,
                        ""amount"": 500,
                        ""currency"": ""usd"",
                        ""price"": { ""id"": ""price_123"" }
                    }
                ]
            }
        }";
                        var resp = InvoicePreviewServiceTestAccessor.MapFromJson_ForTest(json);
                        Assert.Equal("in_test", resp.InvoiceId);
                        Assert.Single(resp.Lines);
                        Assert.Equal("price_123", resp.Lines[0].PriceId);
                }
    }
}
