using System.ComponentModel.DataAnnotations;

namespace HaidersAPI.DTOs
{
    /// <summary>
    /// DTO for granting client access requests
    /// </summary>
    public class GrantAccessDTO
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public Guid ClientId { get; set; }
        
        public string AccessLevel { get; set; } = "ReadOnly";
        public bool CanViewFinancials { get; set; } = false;
        public bool CanManageInvoices { get; set; } = false;
        public bool CanManageExpenses { get; set; } = false;
        public bool CanViewReports { get; set; } = true;
    }
}