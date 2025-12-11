using System;
using Microsoft.Extensions.DependencyInjection;
using InkStainedWretch.OnePageAuthorAPI;
using Xunit;

namespace OnePageAuthor.Test
{
    public class ServiceFactoryBase64ValidationTests
    {
        private static readonly string ValidEndpoint = "https://localhost:8081/";
        private static readonly string ValidDatabase = "TestDb";

        // A minimal valid Base64 string (decodes to 'f')
        private static readonly string ValidBase64Key = "Zg==";

        [Theory]
        [InlineData("Zg=")] // length multiple of 4 but bad padding position
        [InlineData("Zg===")] // too many padding
        [InlineData("Z g==")] // whitespace not allowed
        // Note: leading/trailing whitespace is sanitized; internal whitespace remains invalid
        [InlineData("Zg==?")] // illegal char
        [InlineData("Zm")] // length invalid
        public void AddCosmosClient_InvalidBase64_Throws(string invalidKey)
        {
            var services = new ServiceCollection();
            var ex = Assert.Throws<ArgumentException>(() =>
                ServiceFactory.AddCosmosClient(services, ValidEndpoint, invalidKey));
            Assert.Contains("Base64", ex.Message);
        }

        [Fact]
        public void AddCosmosClient_ValidBase64_Succeeds()
        {
            var services = new ServiceCollection();
            var result = ServiceFactory.AddCosmosClient(services, ValidEndpoint, ValidBase64Key);
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData(null)] 
        [InlineData("")] 
        [InlineData("   ")]
        public void AddCosmosClient_InvalidEndpoint_Throws(string? endpoint)
        {
            var services = new ServiceCollection();
            var ex = Assert.Throws<ArgumentException>(() =>
                ServiceFactory.AddCosmosClient(services, endpoint!, ValidBase64Key));
            Assert.Contains("endpointUri", ex.Message);
        }

        [Theory]
        [InlineData("Zg=")] 
        [InlineData("Zg===")] 
        [InlineData("Z g==")] 
        // Note: leading/trailing whitespace is sanitized; internal whitespace remains invalid 
        [InlineData("Zg==?")] 
        [InlineData("Zm")] 
        public void CreateProvider_InvalidBase64_Throws(string invalidKey)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                ServiceFactory.CreateProvider(ValidEndpoint, invalidKey, ValidDatabase));
            Assert.Contains("Base64", ex.Message);
        }

        [Fact]
        public void AddCosmosClient_QuotedValues_SanitizedAndSucceeds()
        {
            var services = new ServiceCollection();
            var quotedEndpoint = "\"" + ValidEndpoint + "\""; // "https://..."
            var quotedKey = "'" + ValidBase64Key + "'"; // 'base64key'
            var result = ServiceFactory.AddCosmosClient(services, quotedEndpoint, quotedKey);
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateProvider_QuotedValues_SanitizedAndSucceeds()
        {
            var quotedEndpoint = "'" + ValidEndpoint + "'";
            var quotedKey = "\"" + ValidBase64Key + "\"";
            var quotedDb = "'" + ValidDatabase + "'";
            var provider = ServiceFactory.CreateProvider(quotedEndpoint, quotedKey, quotedDb);
            Assert.NotNull(provider);
        }

        [Fact]
        public void CreateProvider_ValidBase64_Succeeds()
        {
            var provider = ServiceFactory.CreateProvider(ValidEndpoint, ValidBase64Key, ValidDatabase);
            Assert.NotNull(provider);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateProvider_InvalidDatabaseId_Throws(string? dbId)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                ServiceFactory.CreateProvider(ValidEndpoint, ValidBase64Key, dbId!));
            Assert.Contains("databaseId", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateProvider_InvalidEndpoint_Throws(string? endpoint)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                ServiceFactory.CreateProvider(endpoint!, ValidBase64Key, ValidDatabase));
            Assert.Contains("endpointUri", ex.Message);
        }

        // Note: InitializeProvider is non-public; covered indirectly via public methods
    }
}
