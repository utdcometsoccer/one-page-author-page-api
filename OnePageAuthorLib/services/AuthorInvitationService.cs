using System.Net.Mail;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Services
{
    /// <summary>
    /// Service implementation for author invitation business logic.
    /// Orchestrates repository access and optional email delivery.
    /// </summary>
    public class AuthorInvitationService : IAuthorInvitationService
    {
        private readonly IAuthorInvitationRepository _repository;
        private readonly IEmailService? _emailService;
        private readonly ILogger<AuthorInvitationService> _logger;

        /// <summary>
        /// Creates a new <see cref="AuthorInvitationService"/>.
        /// </summary>
        /// <param name="repository">Repository for persisting author invitations.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="emailService">Optional email service for sending invitation emails.</param>
        public AuthorInvitationService(
            IAuthorInvitationRepository repository,
            ILogger<AuthorInvitationService> logger,
            IEmailService? emailService = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService;
        }

        /// <inheritdoc/>
        public async Task<AuthorInvitation?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Invitation ID cannot be null or empty.", nameof(id));

            return await _repository.GetByIdAsync(id);
        }

        /// <inheritdoc/>
        public async Task<IList<AuthorInvitation>> GetPendingInvitationsAsync()
        {
            return await _repository.GetPendingInvitationsAsync();
        }

        /// <inheritdoc/>
        public async Task<CreateInvitationResult> CreateInvitationAsync(
            string email,
            List<string> domainNames,
            string? notes)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email address is required.", nameof(email));

            if (domainNames == null || domainNames.Count == 0)
                throw new ArgumentException("At least one domain name is required.", nameof(domainNames));

            if (!IsValidEmail(email))
                throw new ArgumentException($"Invalid email address format: {email}", nameof(email));

            foreach (var domain in domainNames)
            {
                if (!IsValidDomain(domain))
                    throw new ArgumentException($"Invalid domain name format: {domain}", nameof(domainNames));
            }

            var existing = await _repository.GetByEmailAsync(email);
            if (existing != null)
            {
                _logger.LogWarning("Invitation already exists for {Email}. Status: {Status}", email, existing.Status);
                throw new InvalidOperationException(
                    $"An invitation already exists for {email} with status '{existing.Status}'.");
            }

            _logger.LogInformation("Creating invitation for {Email} with domains {Domains}",
                email, string.Join(", ", domainNames));

            var invitation = new AuthorInvitation(email, domainNames, notes);
            var saved = await _repository.AddAsync(invitation);

            _logger.LogInformation("Invitation created successfully. ID: {InvitationId}", saved.id);

            bool emailSent = false;
            if (_emailService != null)
            {
                try
                {
                    _logger.LogInformation("Sending invitation email to {Email}", saved.EmailAddress);
                    emailSent = await _emailService.SendInvitationEmailAsync(
                        saved.EmailAddress,
                        string.Join(", ", saved.DomainNames),
                        saved.id);

                    if (emailSent)
                    {
                        _logger.LogInformation(
                            "Invitation email sent successfully to {Email} for InvitationId {InvitationId}",
                            saved.EmailAddress, saved.id);
                        saved.LastEmailSentAt = DateTime.UtcNow;
                        await _repository.UpdateAsync(saved);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send invitation email");
                    }
                }
                catch (Exception ex)
                {
                    // Email delivery failure is non-fatal to avoid duplicate invitations on client retry.
                    _logger.LogError(
                        ex,
                        "Error sending invitation email for InvitationId {InvitationId} to {Email}",
                        saved.id, saved.EmailAddress);
                    emailSent = false;
                }
            }
            else
            {
                _logger.LogWarning("Email service not configured - invitation created but email not sent");
            }

            saved.EnsureDomainNamesMigrated();
            return new CreateInvitationResult { Invitation = saved, EmailSent = emailSent };
        }

        /// <inheritdoc/>
        public async Task<AuthorInvitation> UpdateInvitationAsync(
            string id,
            List<string>? domainNames,
            string? notes,
            DateTime? expiresAt)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Invitation ID cannot be null or empty.", nameof(id));

            var invitation = await _repository.GetByIdAsync(id);
            if (invitation == null)
                throw new InvalidOperationException($"Invitation with ID {id} not found.");

            if (invitation.Status != "Pending")
                throw new InvalidOperationException(
                    $"Cannot update invitation with status '{invitation.Status}'. Only pending invitations can be updated.");

            if (domainNames != null && domainNames.Count > 0)
            {
                foreach (var domain in domainNames)
                {
                    if (!IsValidDomain(domain))
                        throw new ArgumentException($"Invalid domain name format: {domain}", nameof(domainNames));
                }

                invitation.DomainNames = domainNames;
                invitation.SyncLegacyDomainNameFromDomainNames();
            }

            if (notes != null)
                invitation.Notes = notes;

            if (expiresAt.HasValue)
                invitation.ExpiresAt = expiresAt.Value;

            invitation.LastUpdatedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(invitation);
            _logger.LogInformation("Invitation {InvitationId} updated successfully", id);
            return updated;
        }

        /// <inheritdoc/>
        public async Task<AuthorInvitation> ResendInvitationEmailAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Invitation ID cannot be null or empty.", nameof(id));

            var invitation = await _repository.GetByIdAsync(id);
            if (invitation == null)
                throw new InvalidOperationException($"Invitation with ID {id} not found.");

            if (invitation.Status != "Pending")
                throw new InvalidOperationException(
                    $"Cannot resend invitation with status '{invitation.Status}'. Only pending invitations can be resent.");

            if (_emailService == null)
            {
                _logger.LogWarning("Email service not configured - cannot resend invitation");
                throw new InvalidOperationException("Email service is not configured.");
            }

            _logger.LogInformation("Resending invitation email to {Email}", invitation.EmailAddress);
            bool emailSent = await _emailService.SendInvitationEmailAsync(
                invitation.EmailAddress,
                string.Join(", ", invitation.DomainNames),
                invitation.id);

            if (!emailSent)
            {
                _logger.LogWarning("Failed to resend invitation email");
                throw new InvalidOperationException("Failed to send invitation email.");
            }

            _logger.LogInformation(
                "Invitation email resent successfully to {Email} for InvitationId {InvitationId}",
                invitation.EmailAddress, invitation.id);
            invitation.LastEmailSentAt = DateTime.UtcNow;
            await _repository.UpdateAsync(invitation);
            return invitation;
        }

        /// <inheritdoc/>
        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public bool IsValidDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return false;

            domain = domain.Trim();

            if (domain.Length == 0)
                return false;

            // Allow FQDN-style trailing dot
            if (domain.EndsWith(".", StringComparison.Ordinal))
            {
                domain = domain[..^1];
                if (domain.Length == 0)
                    return false;
            }

            domain = domain.ToLowerInvariant();

            if (domain.Contains(' ', StringComparison.Ordinal))
                return false;

            if (!domain.Contains('.', StringComparison.Ordinal))
                return false;

            if (string.Equals(domain, "localhost", StringComparison.OrdinalIgnoreCase))
                return false;

            var hostType = Uri.CheckHostName(domain);
            if (hostType == UriHostNameType.IPv4 || hostType == UriHostNameType.IPv6)
                return false;

            if (domain.Length > 253)
                return false;

            foreach (var label in domain.Split('.'))
            {
                if (string.IsNullOrEmpty(label) || label.Length > 63)
                    return false;

                if (label.StartsWith("-", StringComparison.Ordinal) ||
                    label.EndsWith("-", StringComparison.Ordinal))
                    return false;

                foreach (var ch in label)
                {
                    var isLetter = ch is >= 'a' and <= 'z';
                    var isDigit = ch is >= '0' and <= '9';
                    if (!isLetter && !isDigit && ch != '-')
                        return false;
                }
            }

            return true;
        }
    }
}
