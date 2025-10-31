namespace HaidersAPI.Models.Configuration;

public class MailOptions
{
    public const string SectionName = "MailSettings";
    
    public string From { get; set; } = string.Empty;
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public MailTemplateOptions Templates { get; set; } = new();
}

public class MailTemplateOptions
{
    public string InvoicePath { get; set; } = "Templates/Invoice.html";
    public string ReminderPath { get; set; } = "Templates/Reminder.html";
    public string WelcomePath { get; set; } = "Templates/Welcome.html";
}