using System.ComponentModel.DataAnnotations;

namespace HaidersAPI.DTOs;

/// <summary>
/// Contact form submission data transfer object
/// Contains all form fields with validation attributes
/// </summary>
public class ContactFormDTO
{
    /// <summary>
    /// Unique submission identifier
    /// </summary>
    public string SubmissionId { get; set; } = string.Empty;

    [Required(ErrorMessage = "För- och efternamn är obligatoriskt")]
    [StringLength(100, ErrorMessage = "Namnet får inte vara längre än 100 tecken")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-postadress är obligatorisk")]
    [EmailAddress(ErrorMessage = "Ogiltig e-postadress")]
    [StringLength(255, ErrorMessage = "E-postadressen får inte vara längre än 255 tecken")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefonnummer är obligatoriskt")]
    [Phone(ErrorMessage = "Ogiltigt telefonnummer")]
    [StringLength(20, ErrorMessage = "Telefonnumret får inte vara längre än 20 tecken")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hemkommun är obligatorisk")]
    [StringLength(100, ErrorMessage = "Kommunen får inte vara längre än 100 tecken")]
    public string Kommun { get; set; } = string.Empty;

    [Required(ErrorMessage = "Beskrivning är obligatorisk")]
    [StringLength(1000, ErrorMessage = "Beskrivningen får inte vara längre än 1000 tecken")]
    public string About { get; set; } = string.Empty;

    // [Required(ErrorMessage = "CV-fil är obligatorisk")] // Temporarily optional for testing
    public IFormFile? CvFile { get; set; } = null;

    [Required(ErrorMessage = "Du måste ange om du är inskriven på Arbetsförmedlingen")]
    public string IsRegisteredAF { get; set; } = string.Empty; // "ja" eller "nej"

    public DateTime? AFRegistrationDate { get; set; }

    /// <summary>
    /// Additional metadata for tracking
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for contact form submission
/// </summary>
public class ContactFormResponseDTO
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SubmissionId { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Internal DTO for email content
/// </summary>
public class ContactEmailContentDTO
{
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;
    public List<EmailAttachmentDTO> Attachments { get; set; } = new();
}

/// <summary>
/// Email attachment DTO
/// </summary>
public class EmailAttachmentDTO
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public long Size { get; set; }
}

/// <summary>
/// Contact form validation result
/// </summary>
public class ContactFormValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Request DTO for contact form submission (matches form data)
/// </summary>
public class ContactFormRequestDTO
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Kommun { get; set; } = string.Empty;
    public string About { get; set; } = string.Empty;
    public string IsRegisteredAF { get; set; } = string.Empty;
    public DateTime? AFRegistrationDate { get; set; }
    public IFormFile? CvFile { get; set; }
}

/// <summary>
/// Result of email operation
/// </summary>
public class EmailResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}