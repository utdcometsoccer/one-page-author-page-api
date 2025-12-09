namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Service for retrieving configuration values from Azure Key Vault.
    /// </summary>
    public interface IKeyVaultConfigService
    {
        /// <summary>
        /// Gets a secret value from Azure Key Vault.
        /// </summary>
        /// <param name="secretName">The name of the secret to retrieve.</param>
        /// <returns>The secret value, or null if not found.</returns>
        Task<string?> GetSecretAsync(string secretName);

        /// <summary>
        /// Gets a secret value from Azure Key Vault with fallback to environment variable.
        /// </summary>
        /// <param name="secretName">The name of the secret in Key Vault.</param>
        /// <param name="environmentVariableName">The environment variable name to fall back to.</param>
        /// <returns>The secret value from Key Vault or environment variable, or null if not found.</returns>
        Task<string?> GetSecretWithFallbackAsync(string secretName, string? environmentVariableName = null);

        /// <summary>
        /// Checks if Key Vault is enabled and configured.
        /// </summary>
        /// <returns>True if Key Vault is enabled, false otherwise.</returns>
        bool IsKeyVaultEnabled();
    }
}
