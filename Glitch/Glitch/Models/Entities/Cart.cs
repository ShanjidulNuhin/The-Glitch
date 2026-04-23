namespace Glitch.Models.Entities
{
    public class Cart
    {
        // Primary Key
        public int Id { get; set; }

        // Foreign Key - which user added this?
        public int UserId { get; set; }

        // Foreign Key - which game was added?
        public int GameId { get; set; }

        // When was it added to cart?
        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public User User { get; set; } = null!;
        public Game Game { get; set; } = null!;
    }
}