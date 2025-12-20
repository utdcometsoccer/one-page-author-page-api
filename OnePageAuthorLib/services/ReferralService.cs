using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Services
{
    /// <summary>
    /// Service implementation for referral program business logic.
    /// </summary>
    public class ReferralService : IReferralService
    {
        private readonly IReferralRepository _referralRepository;
        private readonly ILogger<ReferralService> _logger;
        private readonly IConfiguration _configuration;

        public ReferralService(
            IReferralRepository referralRepository,
            ILogger<ReferralService> logger,
            IConfiguration configuration)
        {
            _referralRepository = referralRepository ?? throw new ArgumentNullException(nameof(referralRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<CreateReferralResponse> CreateReferralAsync(CreateReferralRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.ReferrerId))
                throw new ArgumentException("ReferrerId is required.", nameof(request.ReferrerId));

            if (string.IsNullOrWhiteSpace(request.ReferredEmail))
                throw new ArgumentException("ReferredEmail is required.", nameof(request.ReferredEmail));

            // Validate email format
            if (!IsValidEmail(request.ReferredEmail))
                throw new ArgumentException("Invalid email format.", nameof(request.ReferredEmail));

            _logger.LogInformation("Creating referral for referrer {ReferrerId} to {ReferredEmail}",
                request.ReferrerId, request.ReferredEmail);

            // Check if this email was already referred by this user
            var exists = await _referralRepository.ExistsByReferrerAndEmailAsync(
                request.ReferrerId, request.ReferredEmail);

            if (exists)
            {
                _logger.LogWarning("Referral already exists for referrer {ReferrerId} to {ReferredEmail}",
                    request.ReferrerId, request.ReferredEmail);
                throw new InvalidOperationException("This email has already been referred by you.");
            }

            // Generate unique referral code with retry limit
            var referralCode = GenerateReferralCode();

            // Ensure the code is unique (max 5 retries to avoid excessive database calls)
            var retries = 0;
            const int maxRetries = 5;
            var codeExists = await _referralRepository.GetByReferralCodeAsync(referralCode);
            
            while (codeExists != null && retries < maxRetries)
            {
                referralCode = GenerateReferralCode();
                codeExists = await _referralRepository.GetByReferralCodeAsync(referralCode);
                retries++;
            }

            if (codeExists != null)
            {
                _logger.LogError("Failed to generate unique referral code after {MaxRetries} attempts", maxRetries);
                throw new InvalidOperationException("Unable to generate a unique referral code. Please try again.");
            }

            // Create the referral entity
            var referral = new Referral(request.ReferrerId, request.ReferredEmail, referralCode);

            // Save to database
            await _referralRepository.AddAsync(referral);

            _logger.LogInformation("Successfully created referral with code {ReferralCode}", referralCode);

            // Generate response
            var response = new CreateReferralResponse
            {
                ReferralCode = referralCode,
                ReferralUrl = GenerateReferralUrl(referralCode)
            };

            return response;
        }

        public async Task<ReferralStats> GetReferralStatsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            _logger.LogInformation("Getting referral stats for user {UserId}", userId);

            // Get all referrals made by this user
            var referrals = await _referralRepository.GetByReferrerIdAsync(userId);

            var stats = new ReferralStats
            {
                TotalReferrals = referrals.Count,
                SuccessfulReferrals = referrals.Count(r => r.Status == "Converted"),
                // For now, pending credits = successful referrals (1 month per conversion)
                // In a real system, this would track redeemed vs pending separately
                PendingCredits = referrals.Count(r => r.Status == "Converted"),
                RedeemedCredits = 0 // Would need separate tracking for redeemed credits
            };

            _logger.LogInformation("User {UserId} has {TotalReferrals} referrals, {SuccessfulReferrals} successful",
                userId, stats.TotalReferrals, stats.SuccessfulReferrals);

            return stats;
        }

        public string GenerateReferralCode()
        {
            // Generate a unique 8-character alphanumeric code using cryptographically secure random
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[8];
            rng.GetBytes(bytes);
            
            var code = new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
            return code;
        }

        public string GenerateReferralUrl(string referralCode)
        {
            // Get base URL from configuration, default to a placeholder
            var baseUrl = _configuration["REFERRAL_BASE_URL"] ?? "https://inkstainedwretches.com";
            return $"{baseUrl}/signup?ref={referralCode}";
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
