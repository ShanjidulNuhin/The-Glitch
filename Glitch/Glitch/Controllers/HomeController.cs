using Glitch.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace glitch.Controllers
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
        public async Task<IActionResult> GameDetail(int id)
        {
            var game = await _context.Games.FindAsync(id);

            if (game == null || !game.IsAvailable)
                return RedirectToAction("Index");

            // Check if logged in customer has purchased this game
            var userIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (userIdStr != null && role == "Customer")
            {
                var userId = int.Parse(userIdStr);

                // Pass to view whether user has purchased
                ViewBag.HasPurchased = await _context.Purchases
                    .AnyAsync(p => p.UserId == userId && p.GameId == id);
            }
            else
            {
                ViewBag.HasPurchased = false;
            }

            return View(game);
        }
    }
}