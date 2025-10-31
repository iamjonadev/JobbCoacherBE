using HaidersAPI.DTOs;
using System.Text;

namespace HaidersAPI.Services;

/// <summary>
/// Service for creating contact form email content
/// Generates HTML and plain text versions of emails with professional formatting
/// </summary>
public class ContactEmailService
{
    private readonly ILogger<ContactEmailService> _logger;

    public ContactEmailService(ILogger<ContactEmailService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Create email content for contact form submission
    /// </summary>
    public ContactEmailContentDTO CreateContactFormEmail(ContactFormDTO contactForm)
    {
        _logger.LogInformation("Creating email content for contact form from {Name} ({Email})", contactForm.Name, contactForm.Email);

        var submissionId = Guid.NewGuid().ToString("N")[..8].ToUpper();
        var subject = $"🤝 Ny kontaktformulär från {contactForm.Name} - {contactForm.Kommun}";

        var htmlBody = CreateHtmlEmailBody(contactForm, submissionId);
        var plainTextBody = CreatePlainTextEmailBody(contactForm, submissionId);

        var emailContent = new ContactEmailContentDTO
        {
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody
        };

        _logger.LogInformation("Email content created successfully with submission ID: {SubmissionId}", submissionId);
        return emailContent;
    }

    /// <summary>
    /// Create professional HTML email body
    /// </summary>
    private string CreateHtmlEmailBody(ContactFormDTO contactForm, string submissionId)
    {
        _logger.LogInformation("CreateHtmlEmailBody called - SubmissionId: {SubmissionId}", submissionId);
        _logger.LogInformation("ContactForm properties - Name: {Name}, Email: {Email}, Phone: {Phone}, Kommun: {Kommun}, About: {About}, IsRegisteredAF: {IsRegisteredAF}, AFRegistrationDate: {AFRegistrationDate}", 
            contactForm.Name ?? "NULL", contactForm.Email ?? "NULL", contactForm.Phone ?? "NULL", 
            contactForm.Kommun ?? "NULL", contactForm.About ?? "NULL", contactForm.IsRegisteredAF ?? "NULL", contactForm.AFRegistrationDate?.ToString() ?? "NULL");

        var afStatus = (contactForm.IsRegisteredAF?.ToLower() == "ja") ? "Ja" : "Nej";
        var afDate = contactForm.AFRegistrationDate?.ToString("yyyy-MM-dd") ?? "Ej angivet";
        var afInfo = (contactForm.IsRegisteredAF?.ToLower() == "ja") 
            ? $"<strong>Ja</strong> (Inskriven sedan: {afDate})"
            : "<strong>Nej</strong>";

        var html = $@"
<!DOCTYPE html>
<html lang='sv'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Kontaktformulär - {contactForm.Name ?? "Okänd"}</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 10px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 300; }}
        .header .subtitle {{ margin: 10px 0 0 0; font-size: 16px; opacity: 0.9; }}
        .content {{ padding: 30px; }}
        .info-section {{ margin-bottom: 25px; }}
        .info-section h2 {{ color: #667eea; font-size: 18px; margin-bottom: 15px; border-bottom: 2px solid #f0f0f0; padding-bottom: 8px; }}
        .info-grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-bottom: 20px; }}
        .info-item {{ background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 4px solid #667eea; }}
        .info-item label {{ font-weight: 600; color: #555; display: block; margin-bottom: 5px; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; }}
        .info-item value {{ color: #333; font-size: 16px; }}
        .description {{ background: #f8f9fa; padding: 20px; border-radius: 8px; border-left: 4px solid #28a745; margin: 20px 0; }}
        .description label {{ font-weight: 600; color: #555; display: block; margin-bottom: 10px; font-size: 14px; }}
        .description value {{ white-space: pre-wrap; line-height: 1.6; }}
        .af-section {{ background: #e3f2fd; padding: 20px; border-radius: 8px; border-left: 4px solid #2196f3; margin: 20px 0; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; border-top: 1px solid #e9ecef; }}
        .footer .meta {{ color: #666; font-size: 12px; margin-top: 10px; }}
        .highlight {{ background: #fff3cd; color: #856404; padding: 15px; border-radius: 8px; border-left: 4px solid #ffc107; margin: 20px 0; }}
        @media (max-width: 600px) {{ .info-grid {{ grid-template-columns: 1fr; }} }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🤝 Kontaktformulär</h1>
            <div class='subtitle'>Ny ansökan från HaidersAPI</div>
        </div>
        
        <div class='content'>
            <div class='highlight'>
                <strong>📋 Submission ID:</strong> {submissionId}
            </div>

            <div class='info-section'>
                <h2>👤 Personuppgifter</h2>
                <div class='info-grid'>
                    <div class='info-item'>
                        <label>För- och efternamn</label>
                        <value>{System.Net.WebUtility.HtmlEncode(contactForm.Name ?? string.Empty)}</value>
                    </div>
                    <div class='info-item'>
                        <label>E-postadress</label>
                        <value><a href='mailto:{contactForm.Email ?? string.Empty}'>{System.Net.WebUtility.HtmlEncode(contactForm.Email ?? string.Empty)}</a></value>
                    </div>
                    <div class='info-item'>
                        <label>Telefonnummer</label>
                        <value><a href='tel:+46{contactForm.Phone ?? string.Empty}'>+46 {System.Net.WebUtility.HtmlEncode(contactForm.Phone ?? string.Empty)}</a></value>
                    </div>
                    <div class='info-item'>
                        <label>Hemkommun</label>
                        <value>{System.Net.WebUtility.HtmlEncode(contactForm.Kommun ?? string.Empty)}</value>
                    </div>
                </div>
            </div>

            <div class='description'>
                <label>📝 Beskrivning om sig själv</label>
                <value>{System.Net.WebUtility.HtmlEncode(contactForm.About ?? string.Empty)}</value>
            </div>

            <div class='af-section'>
                <h2>🏢 Arbetsförmedlingen</h2>
                <div class='info-item'>
                    <label>Inskriven på Arbetsförmedlingen</label>
                    <value>{afInfo}</value>
                </div>
            </div>

            <div class='info-section'>
                <h2>📎 Bifogade filer</h2>
                <div class='info-item'>
                    <label>CV-fil</label>
                    <value>{(contactForm.CvFile != null ? $"📄 {System.Net.WebUtility.HtmlEncode(contactForm.CvFile.FileName)} ({FormatFileSize(contactForm.CvFile.Length)})" : "❌ Ingen fil bifogad")}</value>
                </div>
            </div>
        </div>

        <div class='footer'>
            <strong>AdoteamAB - HaidersAPI</strong>
            <div class='meta'>
                Mottaget: {contactForm.SubmittedAt:yyyy-MM-dd HH:mm:ss} UTC<br>
                IP: {contactForm.IpAddress} | User Agent: {System.Net.WebUtility.HtmlEncode(contactForm.UserAgent)}
            </div>
        </div>
    </div>
</body>
</html>";

        return html;
    }

    /// <summary>
    /// Create plain text email body for fallback
    /// </summary>
    private string CreatePlainTextEmailBody(ContactFormDTO contactForm, string submissionId)
    {
        var afStatus = contactForm.IsRegisteredAF?.ToLower() == "ja" ? "Ja" : "Nej";
        var afDate = contactForm.AFRegistrationDate?.ToString("yyyy-MM-dd") ?? "Ej angivet";
        var afInfo = contactForm.IsRegisteredAF?.ToLower() == "ja" 
            ? $"Ja (Inskriven sedan: {afDate})"
            : "Nej";

        var cvFileInfo = contactForm.CvFile != null 
            ? $"{contactForm.CvFile.FileName} ({FormatFileSize(contactForm.CvFile.Length)})"
            : "Ingen fil bifogad";

        var text = $@"
🤝 KONTAKTFORMULÄR - NY ANSÖKAN
═══════════════════════════════════════════════════════════════

📋 Submission ID: {submissionId}

👤 PERSONUPPGIFTER
─────────────────
För- och efternamn: {contactForm.Name ?? string.Empty}
E-postadress: {contactForm.Email ?? string.Empty}
Telefonnummer: +46 {contactForm.Phone ?? string.Empty}
Hemkommun: {contactForm.Kommun ?? string.Empty}

📝 BESKRIVNING
─────────────
{contactForm.About ?? string.Empty}

🏢 ARBETSFÖRMEDLINGEN
────────────────────
Inskriven på Arbetsförmedlingen: {afInfo}

📎 BIFOGADE FILER
───────────────
CV-fil: {cvFileInfo}

═══════════════════════════════════════════════════════════════
AdoteamAB - HaidersAPI
Mottaget: {contactForm.SubmittedAt:yyyy-MM-dd HH:mm:ss} UTC
IP: {contactForm.IpAddress ?? string.Empty}
User Agent: {contactForm.UserAgent ?? string.Empty}
═══════════════════════════════════════════════════════════════";

        return text;
    }

    /// <summary>
    /// Format file size in human readable format
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
}