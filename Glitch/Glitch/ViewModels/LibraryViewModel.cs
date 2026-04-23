using System.Collections.Generic;
using Glitch.Models.Entities;

namespace Glitch.ViewModels
{
    public class LibraryViewModel
    {
        public IEnumerable<Game> PurchasedGames { get; set; } = new List<Game>();
        public IEnumerable<Game> WishlistGames { get; set; } = new List<Game>();
    }
}
