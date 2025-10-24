using System.Security.Cryptography;
using System.Text;

namespace AmazonProductTestConsole;

/// <summary>
/// Standalone hash verification tool
/// </summary>
public static class HashVerification
{
    public static void VerifyAmazonPayloadHash()
    {
        Console.WriteLine("🔍 Amazon Payload Hash Verification");
        Console.WriteLine(new string('=', 50));

        // The exact payload being sent to Amazon
        var payload = @"{
  ""PartnerType"": ""Associates"",
  ""PartnerTag"": ""whoicomdevebl-20"",
  ""Operation"": ""SearchItems"",
  ""SearchIndex"": ""Books"",
  ""Author"": ""Stephen King"",
  ""ItemPage"": 1,
  ""Resources"": [
    ""Images.Primary.Medium"",
    ""ItemInfo.Title"",
    ""ItemInfo.ByLineInfo"",
    ""ItemInfo.ContentInfo"",
    ""ItemInfo.ProductInfo""
  ]
}".Replace("\r\n", "\n").Replace(" ", "").Replace("\n", ""); // Minified JSON

        Console.WriteLine("Testing payload hash...");
        Console.WriteLine($"Payload: {payload}");
        
        var hash = ComputeSha256Hash(payload);
        Console.WriteLine($"SHA256 Hash: {hash}");
        
        // Test AWS canonical request components
        var timestamp = "20251024T004509Z";
        var canonicalUri = "/paapi5/searchitems";
        var canonicalHeaders = "host:webservices.amazon.com\n" +
                              $"x-amz-date:{timestamp}\n" +
                              "x-amz-target:com.amazon.paapi5.v1.ProductAdvertisingAPIv1.SearchItems\n";
        var signedHeaders = "host;x-amz-date;x-amz-target";
        
        var canonicalRequest = $"POST\n{canonicalUri}\n\n{canonicalHeaders}\n{signedHeaders}\n{hash}";
        var canonicalRequestHash = ComputeSha256Hash(canonicalRequest);
        
        Console.WriteLine($"Canonical Request Hash: {canonicalRequestHash}");
        Console.WriteLine();
        Console.WriteLine("Components breakdown:");
        Console.WriteLine($"  Method: POST");
        Console.WriteLine($"  URI: {canonicalUri}");
        Console.WriteLine($"  Query: [empty]");
        Console.WriteLine($"  Headers: {canonicalHeaders.Replace("\n", "\\n")}");
        Console.WriteLine($"  Signed Headers: {signedHeaders}");
        Console.WriteLine($"  Payload Hash: {hash}");
    }

    private static string ComputeSha256Hash(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}