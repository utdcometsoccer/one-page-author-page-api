using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI;

namespace InkStainedWretch.OnePageAuthorAPI.Services
{
    /// <summary>
    /// Service for retrieving configuration values from Azure Key Vault with feature flag support.
    /// </summary>
    public class KeyVaultConfigService : InkStainedWretch.OnePageAuthorAPI.Interfaces.IKeyVaultConfigService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeyVaultConfigService> _logger;
        private readonly SecretClient? _secretClient;
        private readonly bool _isEnabled;

        public KeyVaultConfigService(IConfiguration configuration, ILogger<KeyVaultConfigService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Check feature flag
            var useKeyVault = _configuration["USE_KEY_VAULT"];
            _isEnabled = !string.IsNullOrWhiteSpace(useKeyVault) && 
                        (useKeyVault.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                         useKeyVault.Equals("1", StringComparison.OrdinalIgnoreCase));

            if (_isEnabled)
            {
                var keyVaultUrl = _configuration["KEY_VAULT_URL"];
                if (!string.IsNullOrWhiteSpace(keyVaultUrl))
                {
                    try
                    {
                        _secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
                        _logger.LogInformation("Key Vault client initialized successfully: {KeyVaultUrl}", Utility.MaskUrl(keyVaultUrl));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to initialize Key Vault client. Will fall back to environment variables.");
                        _isEnabled = false;
                    }
                }
                else
                {
                    _logger.LogWarning("USE_KEY_VAULT is true but KEY_VAULT_URL is not configured. Will use environment variables.");
                    _isEnabled = false;
                }
            }
            else
            {
                _logger.LogInformation("Key Vault is disabled. Using environment variables for configuration.");
            }
        }

        public bool IsKeyVaultEnabled()
        {
            return _isEnabled && _secretClient != null;
        }

        public async Task<string?> GetSecretAsync(string secretName)
        {
            if (!IsKeyVaultEnabled())
            {
                _logger.LogDebug("Key Vault is disabled. Cannot retrieve secret: {SecretName}", secretName);
                return null;
            }

            try
            {
                // Convert environment variable naming to Key Vault naming convention
                // Replace underscores with hyphens for Key Vault secret names
                var keyVaultSecretName = secretName.Replace("_", "-");
                
                var response = await _secretClient!.GetSecretAsync(keyVaultSecretName);
                _logger.LogDebug("Successfully retrieved secret from Key Vault: {SecretName}", secretName);
                return response.Value.Value;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("Secret not found in Key Vault: {SecretName}", secretName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret from Key Vault: {SecretName}", secretName);
                return null;
            }
        }

        public async Task<string?> GetSecretWithFallbackAsync(string secretName, string? environmentVariableName = null)
        {
            // Use the same name for environment variable if not specified
            environmentVariableName ??= secretName;

            // Try Key Vault first if enabled
            if (IsKeyVaultEnabled())
            {
                var keyVaultValue = await GetSecretAsync(secretName);
                if (!string.IsNullOrWhiteSpace(keyVaultValue))
                {
                    _logger.LogDebug("Using value from Key Vault for: {SecretName}", secretName);
                    return keyVaultValue;
                }
            }

            // Fall back to environment variable
            var envValue = _configuration[environmentVariableName];
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                _logger.LogDebug("Using value from environment variable for: {EnvironmentVariableName}", environmentVariableName);
                return envValue;
            }

            _logger.LogWarning("Configuration value not found in Key Vault or environment variables: {SecretName}", secretName);
            return null;
        }
    }
}
