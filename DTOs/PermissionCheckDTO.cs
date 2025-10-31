using System.ComponentModel.DataAnnotations;

namespace HaidersAPI.DTOs
{
    /// <summary>
    /// DTO for permission checking requests
    /// </summary>
    public class PermissionCheckDTO
    {
        public Guid? ClientId { get; set; }
        
        [Required]
        public string Action { get; set; } = "";
        
        [Required]
        public string Resource { get; set; } = "";
    }
}