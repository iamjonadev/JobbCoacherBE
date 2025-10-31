namespace HaidersAPI.Models.Configuration;

public class SecurityOptions
{
    public const string SectionName = "Security";
    
    public bool RequireHttps { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public string[] IPWhitelist { get; set; } = Array.Empty<string>();
    public bool AuditAllActions { get; set; } = true;
    public int DataRetentionDays { get; set; } = 2555; // 7 years
}