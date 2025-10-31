using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using Azure.Identity;
using HaidersAPI.DTOs;

namespace HaidersAPI.Services;

/// <summary>
/// Microsoft Graph mail service for HaidersAPI
/// Handles email sending with attachments using Azure AD authentication
/// </summary>
public class HaidersGraphMailService
{
    private readonly GraphServiceClient _graph;
    private readonly string _mailbox;
    private readonly ILogger<HaidersGraphMailService> _logger;

    public HaidersGraphMailService(IConfiguration config, ILogger<HaidersGraphMailService> logger)
    {
        _logger = logger;
        
        // Try to get from environment variables first (loaded by DotEnv), then fallback to configuration
        var tenantId = Environment.GetEnvironmentVariable("AZUREAD_TENANTID") ?? config["AZUREAD_TENANTID"];
        var clientId = Environment.GetEnvironmentVariable("AZUREAD_CLIENTID") ?? config["AZUREAD_CLIENTID"];
        var clientSecret = Environment.GetEnvironmentVariable("AZUREAD_CLIENTSECRET") ?? config["AZUREAD_CLIENTSECRET"];
        _mailbox = Environment.GetEnvironmentVariable("MAIL_FROM") ?? config["MAIL_FROM"] ?? string.Empty;

        _logger.LogInformation("Configuration check - MAIL_FROM: {MailFrom}, TenantId: {TenantId}, ClientId: {ClientId}, ClientSecret: {HasSecret}",
            _mailbox ?? "NULL", tenantId ?? "NULL", clientId ?? "NULL", !string.IsNullOrEmpty(clientSecret) ? "SET" : "NULL");

        if (string.IsNullOrEmpty(_mailbox))
        {
            _logger.LogError("MAIL_FROM configuration is missing in environment variables and configuration");
            throw new InvalidOperationException("MAIL_FROM configuration is missing");
        }

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            _logger.LogError("Azure AD configuration is incomplete. TenantId: {TenantId}, ClientId: {ClientId}, ClientSecret: {HasSecret}", 
                tenantId ?? "NULL", clientId ?? "NULL", !string.IsNullOrEmpty(clientSecret) ? "SET" : "NULL");
            throw new InvalidOperationException("Azure AD configuration is incomplete. Check AZUREAD_TENANTID, AZUREAD_CLIENTID, and AZUREAD_CLIENTSECRET.");
        }

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        _graph = new GraphServiceClient(credential);

        _logger.LogInformation("HaidersGraphMailService initialized with mailbox: {Mailbox}", _mailbox);
    }

    /// <summary>
    /// Send contact form email with attachments
    /// </summary>
    public async Task<EmailResult> SendContactFormEmailAsync(
        string toEmail,
        string subject,
        string htmlContent,
        string plainTextContent = "",
        string replyToEmail = "",
        string replyToName = "",
        List<EmailAttachmentDTO>? attachments = null)
    {
        try
        {
            _logger.LogInformation("Preparing to send contact form email to: {ToEmail}", toEmail);

            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = htmlContent
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = toEmail,
                            Name = "Jona AdoteamAB"
                        }
                    }
                }
            };

            // Add reply-to if provided
            if (!string.IsNullOrEmpty(replyToEmail))
            {
                message.ReplyTo = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = replyToEmail,
                            Name = replyToName ?? replyToEmail
                        }
                    }
                };
            }

            // Add attachments if provided
            if (attachments != null && attachments.Count > 0)
            {
                message.Attachments = new List<Attachment>();
                foreach (var attachment in attachments)
                {
                    var fileAttachment = new FileAttachment
                    {
                        Name = attachment.FileName,
                        ContentType = attachment.ContentType,
                        ContentBytes = attachment.Content,
                        Size = attachment.Content?.Length ?? 0
                    };
                    message.Attachments.Add(fileAttachment);
                }
            }

            var sendMailRequest = new SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = true
            };

            await _graph.Users[_mailbox].SendMail.PostAsync(sendMailRequest);

            _logger.LogInformation("Contact form email sent successfully to: {ToEmail}", toEmail);
            return new EmailResult { IsSuccess = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact form email to: {ToEmail}", toEmail);
            return new EmailResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Test Graph connectivity
    /// </summary>
    public async Task<EmailResult> TestConnectivityAsync()
    {
        try
        {
            _logger.LogInformation("Testing Graph connectivity...");
            
            var user = await _graph.Users[_mailbox].GetAsync();
            
            _logger.LogInformation("Graph connectivity test successful. User: {DisplayName}", user?.DisplayName);
            return new EmailResult { IsSuccess = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Graph connectivity test failed");
            return new EmailResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }
}