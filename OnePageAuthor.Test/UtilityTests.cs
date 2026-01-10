using InkStainedWretch.OnePageAuthorAPI;
using Xunit;

namespace OnePageAuthor.Test;

/// <summary>
/// Unit tests for the Utility class, focusing on multi-issuer parsing logic.
/// </summary>
public class UtilityTests
{
    [Fact]
    public void ParseValidIssuers_WithNullInput_ReturnsNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseValidIssuers_WithEmptyString_ReturnsNull()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseValidIssuers_WithWhitespaceOnly_ReturnsNull()
    {
        // Arrange
        var input = "   ";

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseValidIssuers_WithSingleIssuer_ReturnsArrayWithOneElement()
    {
        // Arrange
        var input = "https://login.microsoftonline.com/tenant1/v2.0";

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("https://login.microsoftonline.com/tenant1/v2.0", result[0]);
    }

    [Fact]
    public void ParseValidIssuers_WithMultipleIssuers_ReturnsArrayWithAllElements()
    {
        // Arrange
        var input = "https://login.microsoftonline.com/tenant1/v2.0,https://login.microsoftonline.com/tenant2/v2.0,https://sts.windows.net/tenant3/";

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Contains("https://login.microsoftonline.com/tenant1/v2.0", result);
        Assert.Contains("https://login.microsoftonline.com/tenant2/v2.0", result);
        Assert.Contains("https://sts.windows.net/tenant3", result);
    }

    [Fact]
    public void ParseValidIssuers_TrimsWhitespace_ReturnsCleanedArray()
    {
        // Arrange
        var input = "  https://login.microsoftonline.com/tenant1/v2.0  ,  https://login.microsoftonline.com/tenant2/v2.0  ";

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal("https://login.microsoftonline.com/tenant1/v2.0", result[0]);
        Assert.Equal("https://login.microsoftonline.com/tenant2/v2.0", result[1]);
    }

    [Fact]
    public void ParseValidIssuers_RemovesTrailingSlashes_ReturnsNormalizedArray()
    {
        // Arrange
        var input = "https://login.microsoftonline.com/tenant1/v2.0/,https://login.microsoftonline.com/tenant2/v2.0///";

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal("https://login.microsoftonline.com/tenant1/v2.0", result[0]);
        Assert.Equal("https://login.microsoftonline.com/tenant2/v2.0", result[1]);
    }

    [Fact]
    public void ParseValidIssuers_FiltersEmptyEntries_ReturnsOnlyValidEntries()
    {
        // Arrange
        var input = "https://login.microsoftonline.com/tenant1/v2.0,,  ,https://login.microsoftonline.com/tenant2/v2.0";

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal("https://login.microsoftonline.com/tenant1/v2.0", result[0]);
        Assert.Equal("https://login.microsoftonline.com/tenant2/v2.0", result[1]);
    }

    [Fact]
    public void ParseValidIssuers_RemovesDuplicatesCaseInsensitive_ReturnsDistinctArray()
    {
        // Arrange
        var input = "https://login.microsoftonline.com/tenant1/v2.0,HTTPS://LOGIN.MICROSOFTONLINE.COM/TENANT1/V2.0,https://login.microsoftonline.com/tenant2/v2.0";

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        // Should preserve the first occurrence (case-sensitive check)
        Assert.Contains(result, i => i.Equals("https://login.microsoftonline.com/tenant1/v2.0", StringComparison.Ordinal) ||
                                     i.Equals("HTTPS://LOGIN.MICROSOFTONLINE.COM/TENANT1/V2.0", StringComparison.Ordinal));
        Assert.Contains(result, i => i.Equals("https://login.microsoftonline.com/tenant2/v2.0", StringComparison.Ordinal));
    }

    [Fact]
    public void ParseValidIssuers_WithComplexInput_HandlesAllTransformations()
    {
        // Arrange
        var input = "  https://login.microsoftonline.com/tenant1/v2.0/  ,, https://login.microsoftonline.com/tenant2/v2.0/ ,  , https://sts.windows.net/tenant3///,HTTPS://LOGIN.MICROSOFTONLINE.COM/TENANT1/V2.0/";

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        // Verify all expected issuers are present (case-insensitive uniqueness)
        Assert.Contains(result, i => i.Equals("https://login.microsoftonline.com/tenant1/v2.0", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, i => i.Equals("https://login.microsoftonline.com/tenant2/v2.0", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, i => i.Equals("https://sts.windows.net/tenant3", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseValidIssuers_WithOnlyCommasAndWhitespace_ReturnsNull()
    {
        // Arrange
        var input = " , , , ";

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseValidIssuers_WithSingleIssuerAndTrailingComma_ReturnsArrayWithOneElement()
    {
        // Arrange
        var input = "https://login.microsoftonline.com/tenant1/v2.0,";

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("https://login.microsoftonline.com/tenant1/v2.0", result[0]);
    }

    [Fact]
    public void ParseValidIssuers_WithDifferentIssuers_PreservesBothV1AndV2Endpoints()
    {
        // Arrange
        var input = "https://sts.windows.net/tenant1/,https://login.microsoftonline.com/tenant1/v2.0";

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Contains("https://sts.windows.net/tenant1", result);
        Assert.Contains("https://login.microsoftonline.com/tenant1/v2.0", result);
    }

    [Fact]
    public void ParseValidIssuers_ReturnsNullWhenAllEntriesAreInvalid()
    {
        // Arrange
        var input = " , / , // , /// ";

        // Act
        var result = Utility.ParseValidIssuers(input);

        // Assert
        // After trimming trailing slashes from "/", "//", "///", they become empty strings
        // and should be filtered out, resulting in null
        Assert.Null(result);
    }
}
