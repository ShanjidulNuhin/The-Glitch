namespace Glitch.Models.Entities
{
    public class Favorite
    {
        // Primary Key
        public int Id { get; set; }

        // Foreign Key - which user favorited this?
        public int UserId { get; set; }

        // Foreign Key - which game was favorited?
        public int GameId { get; set; }

        // When was it favorited?
        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public User User { get; set; } = null!;
        public Game Game { get; set; } = null!;
    }
}