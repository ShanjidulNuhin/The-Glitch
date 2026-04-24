using System.ComponentModel.DataAnnotations;

namespace Glitch.Models.Entities
{
    public class GameRating
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int GameId { get; set; }
        
        [Range(1, 5)]
        public int Score { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User User { get; set; } = null!;
        public Game Game { get; set; } = null!;
    }
}
