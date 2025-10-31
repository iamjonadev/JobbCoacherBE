using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HaidersAPI.DTOs;
using HaidersAPI.Services;
using System.ComponentModel.DataAnnotations;

namespace HaidersAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ContactController : ControllerBase
{
    private readonly HaidersGraphMailService? _mailService;
    private readonly ContactEmailService _emailService;
    private readonly ILogger<ContactController> _logger;
    private readonly IConfiguration _configuration;

    // Allowed file types for CV uploads
    private readonly string[] _allowedFileTypes = {
        ".pdf", ".doc", ".docx", ".txt", ".rtf"
    };

    // Maximum file size (5MB)
    private readonly long _maxFileSize = 5 * 1024 * 1024;

    public ContactController(
        ILogger<ContactController> logger,
        IConfiguration configuration,
        ContactEmailService emailService,
        HaidersGraphMailService? mailService = null)
    {
        _logger = logger;
        _configuration = configuration;
        _emailService = emailService;
        _mailService = mailService;
    }

    /// <summary>
    /// Submit contact form with CV file attachment
    /// Rate limited to prevent spam
    /// </summary>
    /// <param name="form">Contact form data with CV file</param>
    /// <returns>Contact form submission response</returns>
    [HttpPost("submit")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ContactFormResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContactFormResponseDTO>> SubmitContactForm([FromForm] ContactFormRequestDTO request)
    {
        var submissionId = Guid.NewGuid().ToString("N")[..8].ToUpper();
        var clientIP = Request.HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
        
        _logger.LogInformation("Contact form submission started - ID: {SubmissionId}, Name: {Name}, Email: {Email}, IP: {IP}", 
            submissionId, request.Name, request.Email, clientIP);

        // Basic rate limiting check for contact form (max 3 submissions per hour per IP)
        if (await IsRateLimitExceeded(clientIP))
        {
            _logger.LogWarning("Contact form rate limit exceeded for IP: {IP}", clientIP);
            return StatusCode(429, new ContactFormResponseDTO
            {
                IsSuccess = false,
                Message = "För många försök. Försök igen om en timme.",
                SubmissionId = submissionId,
                SubmittedAt = DateTime.UtcNow,
                Errors = new List<string> { "Rate limit överskriden" }
            });
        }

        try
        {
            // Validate the request
            var validationResult = ValidateContactForm(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Contact form validation failed - ID: {SubmissionId}, Errors: {Errors}", 
                    submissionId, string.Join(", ", validationResult.Errors));
                
                return BadRequest(new ContactFormResponseDTO
                {
                    IsSuccess = false,
                    Message = "Formuläret innehåller fel",
                    SubmissionId = submissionId,
                    Errors = validationResult.Errors,
                    SubmittedAt = DateTime.UtcNow
                });
            }

            // Create the contact form DTO with metadata
            var contactForm = MapToContactFormDTO(request, submissionId);

            // Check if mail service is available
            if (_mailService == null)
            {
                _logger.LogError("Mail service not configured - cannot send contact form email");
                return StatusCode(500, new ContactFormResponseDTO
                {
                    IsSuccess = false,
                    Message = "E-posttjänsten är inte konfigurerad. Kontakta support.",
                    SubmissionId = submissionId,
                    SubmittedAt = contactForm.SubmittedAt,
                    Errors = new List<string> { "E-posttjänst ej tillgänglig" }
                });
            }

            // Create email content
            var emailContent = _emailService.CreateContactFormEmail(contactForm);

            // Prepare attachment if CV file is provided
            List<EmailAttachmentDTO> attachments = new();
            if (contactForm.CvFile != null && contactForm.CvFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await contactForm.CvFile.CopyToAsync(memoryStream);
                
                attachments.Add(new EmailAttachmentDTO
                {
                    FileName = contactForm.CvFile.FileName,
                    ContentType = contactForm.CvFile.ContentType,
                    Content = memoryStream.ToArray()
                });
            }

            // Send email using Graph Mail Service
            var recipientEmail = _configuration["Company:Email"] ?? "jona@adoteam.dev";
            var emailResult = await _mailService.SendContactFormEmailAsync(
                toEmail: recipientEmail,
                subject: emailContent.Subject,
                htmlContent: emailContent.HtmlBody,
                plainTextContent: emailContent.PlainTextBody,
                replyToEmail: contactForm.Email,
                replyToName: contactForm.Name,
                attachments: attachments
            );

            if (emailResult.IsSuccess)
            {
                // Record successful submission for rate limiting
                RecordSuccessfulSubmission(clientIP);
                
                _logger.LogInformation("Contact form submitted successfully - ID: {SubmissionId}, Email sent to: {RecipientEmail}", submissionId, recipientEmail);
                
                return Ok(new ContactFormResponseDTO
                {
                    IsSuccess = true,
                    Message = "Tack för din ansökan! Vi kommer att kontakta dig inom kort.",
                    SubmissionId = submissionId,
                    SubmittedAt = contactForm.SubmittedAt,
                    Errors = new List<string>()
                });
            }
            else
            {
                _logger.LogError("Failed to send contact form email - ID: {SubmissionId}, Error: {Error}", 
                    submissionId, emailResult.ErrorMessage);
                
                return StatusCode(500, new ContactFormResponseDTO
                {
                    IsSuccess = false,
                    Message = "Ett tekniskt fel inträffade. Försök igen senare eller kontakta oss direkt.",
                    SubmissionId = submissionId,
                    SubmittedAt = contactForm.SubmittedAt,
                    Errors = new List<string> { "Email kunde inte skickas" }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during contact form submission - ID: {SubmissionId}", submissionId);
            
            return StatusCode(500, new ContactFormResponseDTO
            {
                IsSuccess = false,
                Message = "Ett oväntat fel inträffade. Kontakta support om problemet kvarstår.",
                SubmissionId = submissionId,
                SubmittedAt = DateTime.UtcNow,
                Errors = new List<string> { "Intern serverfel" }
            });
        }
    }

    /// <summary>
    /// Test email connectivity (admin endpoint - requires authorization)
    /// </summary>
    [HttpPost("test-email")]
    [Authorize(Roles = "Admin,Owner,AdoteamOwner")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> TestEmailConnectivity()
    {
        _logger.LogInformation("Testing email connectivity");

        try
        {
            if (_mailService == null)
            {
                _logger.LogWarning("Mail service not configured");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Mail service is not configured",
                    timestamp = DateTime.UtcNow
                });
            }

            var result = await _mailService.TestConnectivityAsync();
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Email connectivity test successful");
                return Ok(new { 
                    success = true, 
                    message = "Email connectivity test successful",
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogError("Email connectivity test failed: {Error}", result.ErrorMessage);
                return StatusCode(500, new { 
                    success = false, 
                    message = result.ErrorMessage,
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during email connectivity test");
            return StatusCode(500, new { 
                success = false, 
                message = "Unexpected error during connectivity test",
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get contact form information (limited info for security)
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetContactInfo()
    {
        try
        {
            return Ok(new
            {
                maxFileSize = $"{_maxFileSize / (1024 * 1024)} MB",
                allowedFileTypes = _allowedFileTypes,
                apiVersion = "1.0.0",
                timestamp = DateTime.UtcNow,
                // Removed sensitive configuration details for security
                status = "operational"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contact info");
            return StatusCode(500, new { 
                error = "Unable to get contact info", 
                timestamp = DateTime.UtcNow 
                // Removed detailed error message for security
            });
        }
    }

    #region Private Methods

    /// <summary>
    /// Validate contact form submission with enhanced security
    /// </summary>
    private ContactFormValidationResult ValidateContactForm(ContactFormRequestDTO request)
    {
        var errors = new List<string>();

        // Required field validation with security checks
        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("För- och efternamn krävs");
        else if (request.Name.Length > 100 || ContainsHtmlOrScript(request.Name))
            errors.Add("Namnet innehåller ogiltiga tecken eller är för långt");

        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add("E-postadress krävs");
        else if (!new EmailAddressAttribute().IsValid(request.Email) || ContainsHtmlOrScript(request.Email))
            errors.Add("Ogiltig e-postadress");

        if (string.IsNullOrWhiteSpace(request.Phone))
            errors.Add("Telefonnummer krävs");
        else if (request.Phone.Length > 20 || ContainsHtmlOrScript(request.Phone))
            errors.Add("Telefonnumret innehåller ogiltiga tecken");

        if (string.IsNullOrWhiteSpace(request.Kommun))
            errors.Add("Hemkommun krävs");
        else if (request.Kommun.Length > 100 || ContainsHtmlOrScript(request.Kommun))
            errors.Add("Kommunnamnet innehåller ogiltiga tecken");

        if (string.IsNullOrWhiteSpace(request.About))
            errors.Add("Beskrivning krävs");
        else if (request.About.Length > 2000 || ContainsHtmlOrScript(request.About))
            errors.Add("Beskrivningen är för lång eller innehåller ogiltiga tecken");

        if (string.IsNullOrWhiteSpace(request.IsRegisteredAF))
            errors.Add("Arbetsförmedlingen-status krävs");

        // File validation with enhanced security
        if (request.CvFile != null)
        {
            // Check file size
            if (request.CvFile.Length > _maxFileSize)
                errors.Add($"CV-filen är för stor. Max {_maxFileSize / (1024 * 1024)} MB tillåtet");

            // Check file type
            var fileExtension = Path.GetExtension(request.CvFile.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension) || !_allowedFileTypes.Contains(fileExtension))
                errors.Add($"Ogiltigt filformat. Tillåtna format: {string.Join(", ", _allowedFileTypes)}");

            // Check if file has content
            if (request.CvFile.Length == 0)
                errors.Add("CV-filen är tom");

            // Security check for file name
            if (ContainsHtmlOrScript(request.CvFile.FileName))
                errors.Add("Filnamnet innehåller ogiltiga tecken");
        }

        return new ContactFormValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// Security helper to detect potential XSS or script injection
    /// </summary>
    private bool ContainsHtmlOrScript(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var maliciousPatterns = new[]
        {
            "<script", "</script>", "javascript:", "vbscript:",
            "onload=", "onerror=", "onclick=", "onmouseover=",
            "<iframe", "<object", "<embed", "<link",
            "eval(", "alert(", "confirm(", "prompt(",
            "document.", "window.", "location.",
            "../", "..\\" // Path traversal
        };

        var lowerInput = input.ToLowerInvariant();
        return maliciousPatterns.Any(pattern => lowerInput.Contains(pattern));
    }

    /// <summary>
    /// Simple rate limiting for contact form submissions
    /// </summary>
    private static readonly Dictionary<string, List<DateTime>> _contactSubmissions = new();
    private static readonly object _rateLimitLock = new();

    private async Task<bool> IsRateLimitExceeded(string clientIP)
    {
        await Task.Delay(0); // Make it async for future database implementation
        
        lock (_rateLimitLock)
        {
            var now = DateTime.UtcNow;
            var oneHourAgo = now.AddHours(-1);

            if (!_contactSubmissions.ContainsKey(clientIP))
                _contactSubmissions[clientIP] = new List<DateTime>();

            // Clean old entries
            _contactSubmissions[clientIP] = _contactSubmissions[clientIP]
                .Where(dt => dt > oneHourAgo)
                .ToList();

            // Check if limit exceeded (3 submissions per hour)
            if (_contactSubmissions[clientIP].Count >= 3)
                return true;

            // Don't record this submission yet - only record if validation passes
            return false;
        }
    }

    /// <summary>
    /// Record a successful submission for rate limiting
    /// </summary>
    private void RecordSuccessfulSubmission(string clientIP)
    {
        lock (_rateLimitLock)
        {
            if (!_contactSubmissions.ContainsKey(clientIP))
                _contactSubmissions[clientIP] = new List<DateTime>();
            
            _contactSubmissions[clientIP].Add(DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Map request DTO to contact form DTO with metadata and security sanitization
    /// </summary>
    private ContactFormDTO MapToContactFormDTO(ContactFormRequestDTO request, string submissionId)
    {
        var ipAddress = Request.HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        return new ContactFormDTO
        {
            SubmissionId = submissionId,
            Name = System.Net.WebUtility.HtmlEncode(request.Name?.Trim() ?? string.Empty),
            Email = System.Net.WebUtility.HtmlEncode(request.Email?.Trim().ToLowerInvariant() ?? string.Empty),
            Phone = System.Net.WebUtility.HtmlEncode(request.Phone?.Trim() ?? string.Empty),
            Kommun = System.Net.WebUtility.HtmlEncode(request.Kommun?.Trim() ?? string.Empty),
            About = System.Net.WebUtility.HtmlEncode(request.About?.Trim() ?? string.Empty),
            IsRegisteredAF = System.Net.WebUtility.HtmlEncode(request.IsRegisteredAF?.Trim() ?? string.Empty),
            AFRegistrationDate = request.AFRegistrationDate,
            CvFile = request.CvFile,
            SubmittedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = System.Net.WebUtility.HtmlEncode(userAgent)
        };
    }

    #endregion
}