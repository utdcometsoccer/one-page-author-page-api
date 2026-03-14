namespace AmazonProductTestConsole;

/// <summary>
/// Partner Tag validation and helper utilities
/// </summary>
public static class PartnerTagValidator
{
    /// <summary>
    /// Validates if a partner tag follows the correct format
    /// </summary>
    public static (bool IsValid, string Message) ValidatePartnerTag(string partnerTag)
    {
        if (string.IsNullOrWhiteSpace(partnerTag))
            return (false, "Partner tag cannot be empty");

        // Check for placeholder values
        var placeholders = new[] { "yourtag", "placeholder", "test", "example", "your-tag", "yourstore" };
        if (placeholders.Any(p => partnerTag.ToLower().Contains(p)))
            return (false, $"'{partnerTag}' appears to be a placeholder. You need a real Amazon Associates Partner Tag.");

        // Check format: should be like "storename-20", "mybooks-21", etc.
        if (!partnerTag.Contains('-'))
            return (false, $"'{partnerTag}' is missing the required dash (-). Format should be 'storename-XX'");

        var parts = partnerTag.Split('-');
        if (parts.Length != 2)
            return (false, $"'{partnerTag}' has incorrect format. Should be 'storename-XX' with exactly one dash");

        var storeName = parts[0];
        var suffix = parts[1];

        // Validate store name part
        if (storeName.Length < 3 || storeName.Length > 15)
            return (false, $"Store name '{storeName}' should be 3-15 characters long");

        if (!storeName.All(c => char.IsLetterOrDigit(c)))
            return (false, $"Store name '{storeName}' should contain only letters and numbers");

        // Validate suffix part  
        if (!int.TryParse(suffix, out var suffixNumber))
            return (false, $"Suffix '{suffix}' should be a number (like 20, 21, 22, etc.)");

        var validSuffixes = new[] { 3, 20, 21, 22 }; // Common suffixes for different regions
        if (!validSuffixes.Contains(suffixNumber))
            return (false, $"Suffix '{suffix}' is unusual. Common suffixes are: 03 (Germany), 20 (US/Canada), 21 (UK/France), 22 (Japan)");

        return (true, $"'{partnerTag}' has valid format for region suffix {suffix}");
    }

    /// <summary>
    /// Gets the likely region based on partner tag suffix
    /// </summary>
    public static string GetRegionFromPartnerTag(string partnerTag)
    {
        if (!partnerTag.Contains('-'))
            return "Unknown";

        var suffix = partnerTag.Split('-').Last();

        return suffix switch
        {
            "03" => "Germany (amazon.de)",
            "20" => "US/Canada (amazon.com/.ca)",
            "21" => "UK/France (amazon.co.uk/.fr)",
            "22" => "Japan (amazon.co.jp)",
            _ => $"Unknown region (suffix: {suffix})"
        };
    }

    /// <summary>
    /// Provides guidance on finding the correct partner tag
    /// </summary>
    public static void ShowPartnerTagGuidance()
    {
        Console.WriteLine("🔍 How to Find Your Correct Amazon Partner Tag");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine();
        Console.WriteLine("1. 📝 Sign in to Amazon Associates:");
        Console.WriteLine("   https://affiliate-program.amazon.com/");
        Console.WriteLine();
        Console.WriteLine("2. 🔎 Find your Associate ID in one of these ways:");
        Console.WriteLine("   • Dashboard: Look for 'Associate ID' or 'Tracking ID'");
        Console.WriteLine("   • Account Settings → 'Manage Your Tracking IDs'");
        Console.WriteLine("   • Product Linking → Generate a link and find 'tag=' parameter");
        Console.WriteLine();
        Console.WriteLine("3. ✅ Verify the format:");
        Console.WriteLine("   • Should be: storename-XX (e.g., 'mybooks-20')");
        Console.WriteLine("   • US/Canada: ends with -20");
        Console.WriteLine("   • UK/France: ends with -21");
        Console.WriteLine("   • Germany: ends with -03");
        Console.WriteLine("   • Japan: ends with -22");
        Console.WriteLine();
        Console.WriteLine("4. ⚠️  IMPORTANT: Apply for Product Advertising API access:");
        Console.WriteLine("   https://developer.amazon.com/");
        Console.WriteLine("   • Having Associates account is NOT enough");
        Console.WriteLine("   • Need separate PA API approval");
        Console.WriteLine("   • Generate AWS credentials from Developer Portal");
        Console.WriteLine();
        Console.WriteLine("5. 🧪 Test your configuration:");
        Console.WriteLine("   dotnet run --project AmazonProductTestConsole -- --config");
    }

    /// <summary>
    /// Analyzes the current partner tag and provides feedback
    /// </summary>
    public static void AnalyzePartnerTag(string partnerTag)
    {
        Console.WriteLine("🔍 Partner Tag Analysis");
        Console.WriteLine(new string('-', 30));
        Console.WriteLine($"Current Tag: '{partnerTag}'");

        var (isValid, message) = ValidatePartnerTag(partnerTag);

        if (isValid)
        {
            Console.WriteLine($"✅ Validation: {message}");
            Console.WriteLine($"🌍 Region: {GetRegionFromPartnerTag(partnerTag)}");
        }
        else
        {
            Console.WriteLine($"❌ Validation: {message}");
            Console.WriteLine();
            ShowPartnerTagGuidance();
        }
    }
}