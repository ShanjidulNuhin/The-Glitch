using System.ComponentModel.DataAnnotations;

namespace Glitch.ViewModels
{
    public class CustomerProfileViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [MinLength(6, ErrorMessage = "Min 6 characters")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string? ConfirmNewPassword { get; set; }

        public IFormFile? ImageFile { get; set; }

        public string? ProfileImage { get; set; }

        public int PurchasedGamesCount { get; set; }
        public int AchievementPoints { get; set; }
        public decimal Balance { get; set; }
    }
}