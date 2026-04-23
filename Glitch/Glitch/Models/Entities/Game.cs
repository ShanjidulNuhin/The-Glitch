using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Glitch.Models.Entities
{
    public class Game
    {
        // Primary Key
        public int Id { get; set; }

        // Game title e.g. "Cyberpunk 2077"
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        // Full description of the game
        [Required]
        public string Description { get; set; } = string.Empty;

        // Price with 2 decimal places e.g. 29.99
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // Filename of the game cover image
        public string? ImageFileName { get; set; }

        // Is this game visible to customers?
        public bool IsAvailable { get; set; } = true;

        // When was this game added?
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<Cart> CartItems { get; set; } = new List<Cart>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}