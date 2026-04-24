using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Glitch.Models.Entities
{
    public class GameScreenshot
    {
        public int Id { get; set; }
        
        public int GameId { get; set; }
        public Game Game { get; set; } = null!;

        [Required]
        public string ImageFileName { get; set; } = string.Empty;
    }
}
