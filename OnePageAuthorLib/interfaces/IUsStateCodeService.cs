namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    public interface IUsStateCodeService
    {
        /// <summary>
        /// Normalizes a US state/province input into a 2-letter USPS code when possible.
        /// Returns the original input (trimmed) when it can't be normalized.
        /// Returns empty string for null/whitespace.
        /// </summary>
        string NormalizeToCode(string? stateOrCode);
    }
}
