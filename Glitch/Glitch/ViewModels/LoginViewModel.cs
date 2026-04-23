using System.ComponentModel.DataAnnotations;

namespace Glitch.ViewModels
{
    public class LoginViewModel
    {
        // Email field
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        // Password field
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // Remember me checkbox
        // If true, session lasts longer
        public bool RememberMe { get; set; } = false;
    }
}