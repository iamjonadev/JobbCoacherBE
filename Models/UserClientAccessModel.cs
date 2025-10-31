namespace HaidersAPI.Models
{
    /// <summary>
    /// Model for user client access relationships
    /// </summary>
    public class UserClientAccessModel
    {
        public int UserId { get; set; }
        public Guid ClientId { get; set; }
        public string AccessLevel { get; set; } = string.Empty;
        public bool CanViewFinancials { get; set; }
        public bool CanManageInvoices { get; set; }
        public bool CanManageExpenses { get; set; }
        public bool CanViewReports { get; set; }
        public DateTime GrantedAt { get; set; }
        public int GrantedBy { get; set; }
        public bool IsActive { get; set; }
    }
}