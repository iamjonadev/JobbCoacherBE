namespace HaidersAPI.Models
{
    /// <summary>
    /// Model for users (matches InventoryBE.Users structure)
    /// </summary>
    public class UserModel
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool Active { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }
}