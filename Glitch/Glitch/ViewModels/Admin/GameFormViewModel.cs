using System.ComponentModel.DataAnnotations;


namespace Glitch.ViewModels.Admin
{
    public class GameFormViewModel
    {
        // 0 means new game, >0 means editing existing game
        public int Id { get; set; }

        [Required(ErrorMessage = "Game title is required")]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 999.99, ErrorMessage = "Price must be between $0.01 and $999.99")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Genre is required")]
        public string Genre { get; set; } = string.Empty;

        // YouTube trailer link - optional
        // Admin pastes: https://www.youtube.com/watch?v=XXXXXXX
        public string? TrailerUrl { get; set; }

        // Optional image upload
        public IFormFile? ImageFile { get; set; }

        // Existing image filename (for edit mode)
        public string? ExistingImage { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}