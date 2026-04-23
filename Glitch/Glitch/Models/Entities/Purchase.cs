using System.ComponentModel.DataAnnotations.Schema;

namespace Glitch.Models.Entities
{
    public class Purchase
    {
        // Primary Key
        public int Id { get; set; }

        // Foreign Key - who bought it?
        public int UserId { get; set; }

        // Foreign Key - what game was bought?
        public int GameId { get; set; }

        // Price at the time of purchase (price might change later)
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePaid { get; set; }

        // When was the purchase made?
        public DateTime PurchasedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public User User { get; set; } = null!;
        public Game Game { get; set; } = null!;
    }
}