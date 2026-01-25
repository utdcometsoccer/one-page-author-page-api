using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretchFunctions.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace InkStainedWretchFunctions;

/// <summary>
/// Azure Function HTTP endpoint for creating author invitations.
/// Supports creating invitations and sending invitation emails.
/// Requires authentication.
/// </summary>
public class AuthorInvitationFunction
{
    private readonly ILogger<AuthorInvitationFunction> _logger;
    private readonly IAuthorInvitationRepository _invitationRepository;
    private readonly IEmailService? _emailService;

    /// <summary>
    /// Creates a new <see cref="AuthorInvitationFunction"/> handler.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="invitationRepository">Repository for managing author invitations.</param>
    /// <param name="emailService">Email service for sending invitation emails (optional).</param>
    public AuthorInvitationFunction(
        ILogger<AuthorInvitationFunction> logger,
        IAuthorInvitationRepository invitationRepository,
        IEmailService? emailService = null)
    {
        _logger = logger;
        _invitationRepository = invitationRepository;
        _emailService = emailService;
    }

    /// <summary>
    /// POST /api/author-invitations - Creates a new author invitation
    /// </summary>
    /// <remarks>
    /// Example request:
    /// POST /api/author-invitations
    /// {
    ///   "emailAddress": "author@example.com",
    ///   "domainName": "example.com",
    ///   "notes": "Invitation for John Doe"
    /// }
    /// 
    /// Example response:
    /// {
    ///   "id": "abc-123",
    ///   "emailAddress": "author@example.com",
    ///   "domainName": "example.com",
    ///   "status": "Pending",
    ///   "createdAt": "2024-01-01T00:00:00Z",
    ///   "expiresAt": "2024-01-31T00:00:00Z",
    ///   "emailSent": true
    /// }
    /// </remarks>
    [Function("CreateAuthorInvitation")]
    [Authorize]
    public async Task<HttpResponseData> CreateAuthorInvitation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "author-invitations")] HttpRequestData req)
    {
        var user = req.FunctionContext.Features.Get<IHttpContextAccessor>()?.HttpContext?.User;
        var userId = user?.FindFirst("oid")?.Value ?? user?.FindFirst("sub")?.Value;
        
        _logger.LogInformation("Processing CreateAuthorInvitation request by user {UserId}", userId ?? "Anonymous");

        try
        {
            // Parse request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<CreateAuthorInvitationRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null)
            {
                return await req.CreateErrorResponseAsync(
                    HttpStatusCode.BadRequest,
                    "Invalid request body");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.EmailAddress))
            {
                return await req.CreateErrorResponseAsync(
                    HttpStatusCode.BadRequest,
                    "Email address is required");
            }

            if (string.IsNullOrWhiteSpace(request.DomainName))
            {
                return await req.CreateErrorResponseAsync(
                    HttpStatusCode.BadRequest,
                    "Domain name is required");
            }

            // Validate email format
            if (!IsValidEmail(request.EmailAddress))
            {
                return await req.CreateErrorResponseAsync(
                    HttpStatusCode.BadRequest,
                    $"Invalid email address format: {request.EmailAddress}");
            }

            // Validate domain format
            if (!IsValidDomain(request.DomainName))
            {
                return await req.CreateErrorResponseAsync(
                    HttpStatusCode.BadRequest,
                    $"Invalid domain name format: {request.DomainName}");
            }

            // Check if invitation already exists
            var existingInvitation = await _invitationRepository.GetByEmailAsync(request.EmailAddress);
            if (existingInvitation != null)
            {
                _logger.LogWarning("Invitation already exists for {Email}. Status: {Status}", 
                    request.EmailAddress, existingInvitation.Status);
                // Note: Console app prompts user for confirmation, but for API we'll just log a warning
                // and continue to create a new invitation
            }

            // Create the invitation
            _logger.LogInformation("Creating invitation for {Email} with domain {Domain}", 
                request.EmailAddress, request.DomainName);
            
            var invitation = new AuthorInvitation(
                request.EmailAddress, 
                request.DomainName, 
                request.Notes);
            
            var savedInvitation = await _invitationRepository.AddAsync(invitation);
            
            _logger.LogInformation("Invitation created successfully. ID: {InvitationId}", savedInvitation.id);

            // Send invitation email if email service is configured
            bool emailSent = false;
            if (_emailService != null)
            {
                _logger.LogInformation("Sending invitation email to {Email}", savedInvitation.EmailAddress);
                emailSent = await _emailService.SendInvitationEmailAsync(
                    savedInvitation.EmailAddress,
                    savedInvitation.DomainName,
                    savedInvitation.id);

                if (emailSent)
                {
                    _logger.LogInformation("Invitation email sent successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to send invitation email");
                }
            }
            else
            {
                _logger.LogWarning("Email service not configured - invitation created but email not sent");
            }

            // Return success response
            var responseData = new CreateAuthorInvitationResponse
            {
                Id = savedInvitation.id,
                EmailAddress = savedInvitation.EmailAddress,
                DomainName = savedInvitation.DomainName,
                Status = savedInvitation.Status,
                CreatedAt = savedInvitation.CreatedAt,
                ExpiresAt = savedInvitation.ExpiresAt,
                Notes = savedInvitation.Notes,
                EmailSent = emailSent
            };

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(responseData);
            return response;
        }
        catch (Exception ex)
        {
            return await req.HandleExceptionAsync(ex, _logger);
        }
    }

    /// <summary>
    /// Validates email address format.
    /// </summary>
    private static bool IsValidEmail(string email)
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

    /// <summary>
    /// Validates domain name format.
    /// </summary>
    private static bool IsValidDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return false;

        // Basic domain validation - at least one dot and no spaces
        return domain.Contains('.') && !domain.Contains(' ') && domain.Length > 3;
    }
}

/// <summary>
/// Request model for creating an author invitation.
/// </summary>
public class CreateAuthorInvitationRequest
{
    /// <summary>
    /// The email address of the author to invite.
    /// </summary>
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// The domain name to link to the author's account (e.g., "example.com").
    /// </summary>
    public string DomainName { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes about the invitation.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Response model for a created author invitation.
/// </summary>
public class CreateAuthorInvitationResponse
{
    /// <summary>
    /// The unique invitation ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the invited author.
    /// </summary>
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// The domain name linked to the invitation.
    /// </summary>
    public string DomainName { get; set; } = string.Empty;

    /// <summary>
    /// The status of the invitation.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the invitation was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the invitation expires (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Optional notes about the invitation.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether the invitation email was sent successfully.
    /// </summary>
    public bool EmailSent { get; set; }
}
