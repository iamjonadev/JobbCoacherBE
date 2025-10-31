namespace HaidersAPI.Models.Configuration;

public class CompanyOptions
{
    public const string SectionName = "Company";
    
    public string Name { get; set; } = string.Empty;
    public string OrgNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "Sverige";
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string BankGiro { get; set; } = string.Empty;
    public string PlusGiro { get; set; } = string.Empty;
    public string VATNumber { get; set; } = string.Empty;
}