using System.ComponentModel.DataAnnotations;

namespace HaidersAPI.DTOs
{
    /// <summary>
    /// DTO for token validation requests
    /// </summary>
    public class ValidateTokenDTO
    {
        /// <summary>
        /// JWT token to validate
        /// </summary>
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;
    }
}