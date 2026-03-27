namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Represents a non-transient WHMCS configuration or request-shaping error.
    /// These failures should not be retried until configuration is corrected.
    /// </summary>
    public sealed class WhmcsConfigurationException : Exception
    {
        public WhmcsConfigurationException(string message)
            : base(message)
        {
        }

        public WhmcsConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
