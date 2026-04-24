using System.ComponentModel.DataAnnotations;

namespace Glitch.ViewModels.Admin
{
    public class GameFormViewModel
    {
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

        public string? TrailerUrl { get; set; }

        public IFormFile? ImageFile { get; set; }

        public IFormFile? GameFileUpload { get; set; }

        public string? ExistingImage { get; set; }

        // Existing game file filename (for edit mode)
        public string? ExistingGameFile { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Multiple screenshots
        public List<IFormFile> ScreenshotFiles { get; set; } = new List<IFormFile>();
        
        // For displaying in edit mode
        public List<string> ExistingScreenshots { get; set; } = new List<string>();

        // System Requirements
        public string? ReqSize { get; set; }
        public string? ReqOS { get; set; }
        public string? ReqProcessor { get; set; }
        public string? ReqMemory { get; set; }
        public string? ReqGraphics { get; set; }
        public string? ReqStorage { get; set; }
    }
}