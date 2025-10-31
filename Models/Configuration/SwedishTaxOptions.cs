namespace HaidersAPI.Models.Configuration;

public class SwedishTaxOptions
{
    public const string SectionName = "SwedishTax";
    
    public decimal BasicDeduction { get; set; } = 24800m;
    public decimal PreliminaryTaxRate { get; set; } = 0.30m;
    public decimal VATRate { get; set; } = 0.25m;
    public decimal VATThreshold { get; set; } = 30000m;
    public decimal SelfEmploymentTaxRate { get; set; } = 0.2897m;
    public decimal MileageRate { get; set; } = 18.50m;
    public decimal ROTMaxDeduction { get; set; } = 50000m;
    public decimal RUTMaxDeduction { get; set; } = 25000m;
    public decimal ROTRUTDeductionRate { get; set; } = 0.50m;
}