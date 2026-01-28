using Azure;
using Azure.Communication.Email;
using InkStainedWretch.OnePageAuthorAPI.API;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.Services
{
    /// <summary>
    /// Email service implementation using Azure Communication Services.
    /// </summary>
    public class AzureCommunicationEmailService : IEmailService
    {
        private readonly ILogger<AzureCommunicationEmailService> _logger;
        private readonly EmailClient _emailClient;
        private readonly string _senderAddress;

        public AzureCommunicationEmailService(
            ILogger<AzureCommunicationEmailService> logger,
            string connectionString,
            string senderAddress)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrWhiteSpace(senderAddress))
                throw new ArgumentNullException(nameof(senderAddress));

            _senderAddress = senderAddress;
            _emailClient = new EmailClient(connectionString);
        }

        public async Task<bool> SendInvitationEmailAsync(string toEmail, string domainName, string invitationId)
        {
            try
            {
                _logger.LogInformation("Sending invitation email to {Email} for domain {Domain}", toEmail, domainName);

                var subject = $"You've Been Invited to One Page Author - {domainName}";
                var emailContent = new EmailContent(subject)
                {
                    PlainText = GetEmailPlainText(domainName, invitationId),
                    Html = GetEmailHtmlContent(domainName, invitationId)
                };

                var emailMessage = new EmailMessage(
                    senderAddress: _senderAddress,
                    content: emailContent,
                    recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress(toEmail) }));

                _logger.LogInformation("Sending email via Azure Communication Services to {Email}", toEmail);
                
                EmailSendOperation emailSendOperation = await _emailClient.SendAsync(
                    WaitUntil.Completed,
                    emailMessage);

                _logger.LogInformation("Email send operation completed with status: {Status}, MessageId: {MessageId}", 
                    emailSendOperation.HasCompleted ? "Completed" : "InProgress",
                    emailSendOperation.Id);

                return emailSendOperation.HasCompleted;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Communication Services request failed for {Email}. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}", 
                    toEmail, ex.Status, ex.ErrorCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invitation email to {Email}", toEmail);
                return false;
            }
        }

        private string GetEmailPlainText(string domainName, string invitationId)
        {
            return $@"
You've been invited to create an author account!

You have been invited to link your domain {domainName} to a One Page Author account.

To accept this invitation and create your Microsoft account linked to your domain, please visit:
https://signup.microsoft.com

Your invitation ID: {invitationId}

This invitation will expire in 30 days.

If you have any questions, please contact our support team.

Best regards,
The One Page Author Team
";
        }

        private string GetEmailHtmlContent(string domainName, string invitationId)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>You've Been Invited!</h1>
        </div>
        <div class=""content"">
            <h2>Welcome to One Page Author</h2>
            <p>You have been invited to link your domain <strong>{domainName}</strong> to a One Page Author account.</p>
            <p>To accept this invitation and create your Microsoft account linked to your domain, please click the button below:</p>
            <a href=""https://signup.microsoft.com"" class=""button"">Accept Invitation</a>
            <p><strong>Invitation ID:</strong> {invitationId}</p>
            <p><em>This invitation will expire in 30 days.</em></p>
            <p>If you have any questions, please contact our support team.</p>
        </div>
        <div class=""footer"">
            <p>© 2024 One Page Author. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
";
        }
    }
}
