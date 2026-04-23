using System.ComponentModel.DataAnnotations;

namespace Glitch.ViewModels.Admin
{
    public class AdminProfileViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        // Current password - needed to change password
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        // New password - optional, only if admin wants to change it
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        // Confirm new password
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string? ConfirmNewPassword { get; set; }

        // Profile image upload
        public IFormFile? ImageFile { get; set; }

        // Existing profile image filename
        public string? ProfileImage { get; set; }
    }
}