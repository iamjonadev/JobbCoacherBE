using System.ComponentModel.DataAnnotations;

namespace HaidersAPI.DTOs
{
    /// <summary>
    /// Request DTO for token validation
    /// </summary>
    public class ValidateTokenRequest
    {
        /// <summary>
        /// JWT token to validate
        /// </summary>
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;
    }
}