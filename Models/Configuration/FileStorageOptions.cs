namespace HaidersAPI.Models.Configuration;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    
    public string ReceiptsPath { get; set; } = "Storage/Receipts";
    public string InvoicesPath { get; set; } = "Storage/Invoices";
    public string ReportsPath { get; set; } = "Storage/Reports";
    public string BackupsPath { get; set; } = "Storage/Backups";
    public int MaxFileSizeMB { get; set; } = 10;
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
}