using System.ComponentModel.DataAnnotations;

namespace Glitch.Models.Entities
{
    public class User
    {
        // Primary Key - auto increments (1, 2, 3...)
        public int Id { get; set; }

        // The display name of the user
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        // Must be unique - we enforce this in DbContext
        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        // We will store hashed password, never plain text
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // "Admin" or "Customer" - stored as text in database
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Customer";

        // Profile picture filename (we store name, not full path)
        public string? ProfileImage { get; set; }

        // Admin can block a user - blocked user cannot login
        public bool IsBlocked { get; set; } = false;

        // When did this user register?
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties - links to other tables
        public ICollection<AlternativeEmail> AlternativeEmails { get; set; } = new List<AlternativeEmail>();
        public ICollection<Cart> CartItems { get; set; } = new List<Cart>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}