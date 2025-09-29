using System.Text.Json;
using System.Text;

namespace InkStainedWretch.OnePageAuthorAPI.Authentication;

/// <summary>
/// Utility class for debugging JWT tokens
/// </summary>
public static class JwtDebugHelper
{
    /// <summary>
    /// Analyzes a JWT token and returns debugging information
    /// </summary>
    public static string AnalyzeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "Token is null or empty";

        var segments = token.Split('.');
        var analysis = new StringBuilder();

        analysis.AppendLine($"Token Length: {token.Length}");
        analysis.AppendLine($"Segment Count: {segments.Length} (Expected: 3)");

        if (segments.Length != 3)
        {
            analysis.AppendLine("❌ INVALID: JWT must have exactly 3 segments (header.payload.signature)");

            for (int i = 0; i < segments.Length; i++)
            {
                analysis.AppendLine($"  Segment {i + 1}: Length={segments[i].Length}, Preview={segments[i][..Math.Min(10, segments[i].Length)]}...");
            }

            return analysis.ToString();
        }

        // Analyze each segment
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            analysis.AppendLine($"Segment {i + 1} ({GetSegmentName(i)}): Length={segment.Length}");

            if (i < 2) // Header and payload are base64url encoded JSON
            {
                try
                {
                    var json = DecodeBase64Url(segment);
                    var formatted = FormatJson(json);
                    analysis.AppendLine($"  Content: {formatted}");
                }
                catch (Exception ex)
                {
                    analysis.AppendLine($"  ❌ Failed to decode: {ex.Message}");
                }
            }
            else
            {
                analysis.AppendLine($"  Signature: {segment[..Math.Min(20, segment.Length)]}...");
            }
        }

        analysis.AppendLine("✅ Token format appears valid");
        return analysis.ToString();
    }

    private static string GetSegmentName(int index) => index switch
    {
        0 => "Header",
        1 => "Payload",
        2 => "Signature",
        _ => "Unknown"
    };

    private static string DecodeBase64Url(string base64Url)
    {
        // Add padding if needed
        var padding = 4 - (base64Url.Length % 4);
        if (padding != 4)
            base64Url += new string('=', padding);

        // Replace URL-safe characters
        base64Url = base64Url.Replace('-', '+').Replace('_', '/');

        var bytes = Convert.FromBase64String(base64Url);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string FormatJson(string json)
    {
        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
            return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            return json; // Return as-is if formatting fails
        }
    }
}