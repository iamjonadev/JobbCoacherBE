namespace HaidersAPI.Models.Configuration;

public class ComplianceOptions
{
    public const string SectionName = "Compliance";
    
    public bool RequireDigitalSignature { get; set; } = false;
    public int AuditRetentionYears { get; set; } = 7;
    public int BackupFrequencyHours { get; set; } = 24;
    public bool EncryptSensitiveData { get; set; } = true;
    public bool RequireApprovalForDeletion { get; set; } = true;
    public decimal MaxTransactionAmountWithoutApproval { get; set; } = 50000m;
}