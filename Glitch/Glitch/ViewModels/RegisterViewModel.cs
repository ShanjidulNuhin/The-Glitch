using System.ComponentModel.DataAnnotations;

namespace Glitch.ViewModels
{
    public class RegisterViewModel
    {
        // Username field
        [Required(ErrorMessage = "Username is required")]
        [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        public string Username { get; set; } = string.Empty;

        // Email field
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        // Password field
        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // Confirm Password field
        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        // Compare makes sure this matches the Password field
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Role selection - only shown to first user
        // "Admin" or "Customer"
        public string Role { get; set; } = "Customer";

        // This tells the view whether to show role selection or not
        // We set this in the controller before showing the form
        public bool IsFirstUser { get; set; } = false;
    }
}