using System.ComponentModel.DataAnnotations;

namespace Glitch.Models.Entities
{
    public class AlternativeEmail
    {
        // Primary Key
        public int Id { get; set; }

        // The alternative email address
        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        // Foreign Key - which user owns this email?
        public int UserId { get; set; }

        // Navigation property - gives us access to the full User object
        public User User { get; set; } = null!;
    }
}