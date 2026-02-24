using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretchFunctions.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace InkStainedWretchFunctions;

/// <summary>
/// Azure Function HTTP endpoints for managing author invitations.
/// Supports creating, retrieving, updating, and resending invitations.
/// All endpoints require authentication.
/// </summary>
public class AuthorInvitationFunction
{
    private readonly ILogger<AuthorInvitationFunction> _logger;
    private readonly IAuthorInvitationService _invitationService;

    /// <summary>
    /// Creates a new <see cref="AuthorInvitationFunction"/> handler.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="invitationService">Service encapsulating author invitation business logic.</param>
    public AuthorInvitationFunction(
        ILogger<AuthorInvitationFunction> logger,
        IAuthorInvitationService invitationService)
    {
        _logger = logger;
        _invitationService = invitationService;
    }

    /// <summary>
    /// POST /api/author-invitations - Creates a new author invitation.
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
    /// <returns>
    /// 201 Created with the new invitation; 400 Bad Request on validation failure;
    /// 409 Conflict when an invitation for the email already exists.
    /// </returns>
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
            using var reader = new StreamReader(req.Body);
            string requestBody = await reader.ReadToEndAsync();

            CreateAuthorInvitationRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<CreateAuthorInvitationRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON in CreateAuthorInvitation request body");
                return await req.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "Invalid JSON in request body.");
            }

            if (request == null)
                return await req.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "Invalid request body");

            if (string.IsNullOrWhiteSpace(request.EmailAddress))
                return await req.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "Email address is required");

            // Support both single domain and multiple domains
            var domainNames = new List<string>();
            if (request.DomainNames != null && request.DomainNames.Any())
                domainNames = request.DomainNames;
            else if (!string.IsNullOrWhiteSpace(request.DomainName))
                domainNames.Add(request.DomainName);

            if (!domainNames.Any())
                return await req.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "At least one domain name is required");

            CreateInvitationResult result;
            try
            {
                result = await _invitationService.CreateInvitationAsync(request.EmailAddress, domainNames, request.Notes);
            }
            catch (ArgumentException ex)
            {
                return await req.CreateErrorResponseAsync(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return await req.CreateErrorResponseAsync(HttpStatusCode.Conflict, ex.Message);
            }

            var savedInvitation = result.Invitation;
            var responseData = new CreateAuthorInvitationResponse
            {
                Id = savedInvitation.id,
                EmailAddress = savedInvitation.EmailAddress,
                DomainName = savedInvitation.GetPrimaryDomainName(),
                DomainNames = savedInvitation.DomainNames,
                Status = savedInvitation.Status,
                CreatedAt = savedInvitation.CreatedAt,
                ExpiresAt = savedInvitation.ExpiresAt,
                Notes = savedInvitation.Notes,
                EmailSent = result.EmailSent,
                LastEmailSentAt = savedInvitation.LastEmailSentAt
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
    /// GET /api/author-invitations - Lists all pending author invitations.
    /// </summary>
    /// <returns>
    /// 200 OK with the list of pending invitations.
    /// </returns>
    [Function("ListAuthorInvitations")]
    [Authorize]
    public async Task<HttpResponseData> ListAuthorInvitations(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "author-invitations")] HttpRequestData req)
    {
        var user = req.FunctionContext.Features.Get<IHttpContextAccessor>()?.HttpContext?.User;
        var userId = user?.FindFirst("oid")?.Value ?? user?.FindFirst("sub")?.Value;

        _logger.LogInformation("Processing ListAuthorInvitations request by user {UserId}", userId ?? "Anonymous");

        try
        {
            var invitations = await _invitationService.GetPendingInvitationsAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(invitations);
            return response;
        }
        catch (Exception ex)
        {
            return await req.HandleExceptionAsync(ex, _logger);
        }
    }

    /// <summary>
    /// GET /api/author-invitations/{id} - Retrieves a single author invitation by ID.
    /// </summary>
    /// <returns>
    /// 200 OK with the invitation; 404 Not Found when no invitation matches the supplied ID.
    /// </returns>
    [Function("GetAuthorInvitationById")]
    [Authorize]
    public async Task<HttpResponseData> GetAuthorInvitationById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "author-invitations/{id}")] HttpRequestData req,
        string id)
    {
        var user = req.FunctionContext.Features.Get<IHttpContextAccessor>()?.HttpContext?.User;
        var userId = user?.FindFirst("oid")?.Value ?? user?.FindFirst("sub")?.Value;

        _logger.LogInformation("Processing GetAuthorInvitationById request for ID {InvitationId} by user {UserId}", id, userId ?? "Anonymous");

        try
        {
            var invitation = await _invitationService.GetByIdAsync(id);

            if (invitation == null)
            {
                return await req.CreateErrorResponseAsync(
                    HttpStatusCode.NotFound,
                    $"Invitation with ID {id} not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(invitation);
            return response;
        }
        catch (Exception ex)
        {
            return await req.HandleExceptionAsync(ex, _logger);
        }
    }

    /// <summary>
    /// PUT /api/author-invitations/{id} - Updates an existing pending author invitation.
    /// </summary>
    /// <returns>
    /// 200 OK with the updated invitation; 400 Bad Request on validation or status errors;
    /// 404 Not Found when no invitation matches the supplied ID.
    /// </returns>
    [Function("UpdateAuthorInvitation")]
    [Authorize]
    public async Task<HttpResponseData> UpdateAuthorInvitation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "author-invitations/{id}")] HttpRequestData req,
        string id)
    {
        var user = req.FunctionContext.Features.Get<IHttpContextAccessor>()?.HttpContext?.User;
        var userId = user?.FindFirst("oid")?.Value ?? user?.FindFirst("sub")?.Value;

        _logger.LogInformation("Processing UpdateAuthorInvitation request for ID {InvitationId} by user {UserId}", id, userId ?? "Anonymous");

        try
        {
            using var reader = new StreamReader(req.Body);
            string requestBody = await reader.ReadToEndAsync();

            UpdateAuthorInvitationRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<UpdateAuthorInvitationRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON in UpdateAuthorInvitation request body");
                return await req.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "Invalid JSON in request body.");
            }

            if (request == null)
                return await req.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "Invalid request body");

            AuthorInvitation updatedInvitation;
            try
            {
                updatedInvitation = await _invitationService.UpdateInvitationAsync(
                    id, request.DomainNames, request.Notes, request.ExpiresAt);
            }
            catch (ArgumentException ex)
            {
                return await req.CreateErrorResponseAsync(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return await req.CreateErrorResponseAsync(HttpStatusCode.NotFound, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return await req.CreateErrorResponseAsync(HttpStatusCode.BadRequest, ex.Message);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(updatedInvitation);
            return response;
        }
        catch (Exception ex)
        {
            return await req.HandleExceptionAsync(ex, _logger);
        }
    }

    /// <summary>
    /// POST /api/author-invitations/{id}/resend - Resends the invitation email for a pending invitation.
    /// </summary>
    /// <returns>
    /// 200 OK on success; 400 Bad Request when the invitation is not pending;
    /// 404 Not Found when no invitation matches the supplied ID;
    /// 503 Service Unavailable when the email service is not configured.
    /// </returns>
    [Function("ResendAuthorInvitation")]
    [Authorize]
    public async Task<HttpResponseData> ResendAuthorInvitation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "author-invitations/{id}/resend")] HttpRequestData req,
        string id)
    {
        var user = req.FunctionContext.Features.Get<IHttpContextAccessor>()?.HttpContext?.User;
        var userId = user?.FindFirst("oid")?.Value ?? user?.FindFirst("sub")?.Value;

        _logger.LogInformation("Processing ResendAuthorInvitation request for ID {InvitationId} by user {UserId}", id, userId ?? "Anonymous");

        try
        {
            AuthorInvitation invitation;
            try
            {
                invitation = await _invitationService.ResendInvitationEmailAsync(id);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return await req.CreateErrorResponseAsync(HttpStatusCode.NotFound, ex.Message);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not configured"))
            {
                return await req.CreateErrorResponseAsync(HttpStatusCode.ServiceUnavailable, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return await req.CreateErrorResponseAsync(HttpStatusCode.BadRequest, ex.Message);
            }

            var responseData = new ResendInvitationResponse
            {
                Id = invitation.id,
                EmailAddress = invitation.EmailAddress,
                EmailSent = true,
                LastEmailSentAt = invitation.LastEmailSentAt
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(responseData);
            return response;
        }
        catch (Exception ex)
        {
            return await req.HandleExceptionAsync(ex, _logger);
        }
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
    /// This is kept for backward compatibility. Use DomainNames for multiple domains.
    /// </summary>
    public string? DomainName { get; set; }

    /// <summary>
    /// The domain names to link to the author's account (e.g., ["example.com", "author-site.com"]).
    /// </summary>
    public List<string>? DomainNames { get; set; }

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
    /// The domain name linked to the invitation (first domain for backward compatibility).
    /// </summary>
    public string DomainName { get; set; } = string.Empty;

    /// <summary>
    /// The domain names linked to the invitation.
    /// </summary>
    public List<string> DomainNames { get; set; } = new List<string>();

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

    /// <summary>
    /// When the invitation email was last sent (UTC).
    /// </summary>
    public DateTime? LastEmailSentAt { get; set; }
}

/// <summary>
/// Request model for updating an author invitation.
/// </summary>
public class UpdateAuthorInvitationRequest
{
    /// <summary>
    /// The domain names to link to the author's account (e.g., ["example.com", "author-site.com"]).
    /// </summary>
    public List<string>? DomainNames { get; set; }

    /// <summary>
    /// Optional notes about the invitation.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When the invitation expires (UTC).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Response model for resending an invitation.
/// </summary>
public class ResendInvitationResponse
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
    /// Whether the invitation email was sent successfully.
    /// </summary>
    public bool EmailSent { get; set; }

    /// <summary>
    /// When the invitation email was last sent (UTC).
    /// </summary>
    public DateTime? LastEmailSentAt { get; set; }
}
