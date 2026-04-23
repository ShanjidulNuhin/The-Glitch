using Glitch.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Glitch.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /
        public async Task<IActionResult> Index()
        {
            var games = await _context.Games
                .Where(g => g.IsAvailable)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            return View(games);
        }

        // GET: /Home/GameDetail/5
        // Shows full game detail page
        public async Task<IActionResult> GameDetail(int id)
        {
            var game = await _context.Games.FindAsync(id);

            // If game not found or not available, go back home
            if (game == null || !game.IsAvailable)
                return RedirectToAction("Index");

            return View(game);
        }
    }
}