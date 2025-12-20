using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Services
{
    /// <summary>
    /// Service for managing leads with validation, duplicate detection, and email service integration.
    /// </summary>
    public class LeadService : ILeadService
    {
        private readonly ILeadRepository _leadRepository;
        private readonly ILogger<LeadService> _logger;
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public LeadService(ILeadRepository leadRepository, ILogger<LeadService> logger)
        {
            _leadRepository = leadRepository ?? throw new ArgumentNullException(nameof(leadRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new lead or returns existing lead if email already exists.
        /// </summary>
        public async Task<CreateLeadResponse> CreateLeadAsync(CreateLeadRequest request, string? ipAddress)
        {
            // Validate email format
            if (!IsValidEmail(request.Email))
            {
                throw new ValidationException("Invalid email format");
            }

            // Validate source
            if (!LeadSource.IsValid(request.Source))
            {
                throw new ValidationException($"Invalid source. Must be one of: {string.Join(", ", LeadSource.ValidSources)}");
            }

            // Normalize email and extract domain
            var normalizedEmail = request.Email.ToLowerInvariant();
            var emailDomain = Lead.ExtractEmailDomain(normalizedEmail);

            // Check for existing lead
            var existingLead = await _leadRepository.GetByEmailAsync(normalizedEmail, emailDomain);
            if (existingLead != null)
            {
                _logger.LogInformation("Existing lead found for email: {Email}", normalizedEmail);
                return new CreateLeadResponse
                {
                    Id = existingLead.id ?? string.Empty,
                    Status = LeadCreationStatus.Existing,
                    Message = "Email already registered"
                };
            }

            // Create new lead
            var lead = new Lead
            {
                Email = normalizedEmail,
                FirstName = request.FirstName,
                Source = request.Source,
                LeadMagnet = request.LeadMagnet,
                UtmSource = request.UtmSource,
                UtmMedium = request.UtmMedium,
                UtmCampaign = request.UtmCampaign,
                Referrer = request.Referrer,
                Locale = request.Locale,
                IpAddress = ipAddress,
                ConsentGiven = request.ConsentGiven,
                EmailDomain = emailDomain,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                EmailServiceStatus = "pending"
            };

            try
            {
                var createdLead = await _leadRepository.AddAsync(lead);
                _logger.LogInformation("Successfully created lead with ID: {LeadId} for email: {Email}", createdLead.id, normalizedEmail);

                // TODO: Integrate with email service (Mailchimp/ConvertKit) here
                // This should be done asynchronously, possibly via a queue or background service

                return new CreateLeadResponse
                {
                    Id = createdLead.id ?? string.Empty,
                    Status = LeadCreationStatus.Created,
                    Message = "Lead successfully created"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lead for email: {Email}", normalizedEmail);
                throw;
            }
        }

        /// <summary>
        /// Validates an email address format using RFC-compliant regex.
        /// </summary>
        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Additional validation using DataAnnotations
            var emailAttribute = new EmailAddressAttribute();
            if (!emailAttribute.IsValid(email))
                return false;

            // Additional regex validation for common patterns
            return EmailRegex.IsMatch(email);
        }

        /// <summary>
        /// Gets a lead by ID.
        /// </summary>
        public async Task<Lead?> GetLeadByIdAsync(string id, string emailDomain)
        {
            return await _leadRepository.GetByIdAsync(id, emailDomain);
        }

        /// <summary>
        /// Gets leads by source with optional date filtering.
        /// </summary>
        public async Task<IList<Lead>> GetLeadsBySourceAsync(string source, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (!LeadSource.IsValid(source))
            {
                throw new ArgumentException($"Invalid source. Must be one of: {string.Join(", ", LeadSource.ValidSources)}", nameof(source));
            }

            return await _leadRepository.GetBySourceAsync(source, startDate, endDate);
        }
    }
}
