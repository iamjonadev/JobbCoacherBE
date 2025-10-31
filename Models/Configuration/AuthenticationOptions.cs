namespace HaidersAPI.Models.Configuration;

public class AuthenticationOptions
{
    public const string SectionName = "Authentication";
    
    public string JwtSecret { get; set; } = string.Empty;
    public string PasswordKey { get; set; } = string.Empty;
    public int TokenExpiryDays { get; set; } = 30;
    public int RefreshTokenExpiryDays { get; set; } = 90;
    public int MaxFailedAttempts { get; set; } = 5;
    public int AccountLockoutMinutes { get; set; } = 30;
    public bool RequireTwoFactor { get; set; } = false;
    public int SessionTimeoutMinutes { get; set; } = 120;
}