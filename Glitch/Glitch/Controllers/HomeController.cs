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
                .Include(g => g.Ratings)
                .Where(g => g.IsAvailable)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            return View(games);
        }

        // GET: /Home/GameDetail/5
        public async Task<IActionResult> GameDetail(int id)
        {
            var game = await _context.Games.Include(g => g.Screenshots).FirstOrDefaultAsync(g => g.Id == id);

            if (game == null || !game.IsAvailable)
                return RedirectToAction("Index");

            // Check if logged in customer has purchased this game
            var userIdStr = HttpContext.Session.GetString("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (userIdStr != null && (role == "Customer" || role == "Admin"))
            {
                var userId = int.Parse(userIdStr);

                // Pass to view whether user has purchased
                ViewBag.HasPurchased = await _context.Purchases
                    .AnyAsync(p => p.UserId == userId && p.GameId == id);
                
                var userRating = await _context.GameRatings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.GameId == id);
                ViewBag.UserRating = userRating?.Score ?? 0;
            }
            else
            {
                ViewBag.HasPurchased = false;
                ViewBag.UserRating = 0;
            }

            var allRatings = await _context.GameRatings.Where(r => r.GameId == id).ToListAsync();
            ViewBag.AverageRating = allRatings.Any() ? allRatings.Average(r => r.Score) : 0.0;
            ViewBag.TotalRatings = allRatings.Count;

            return View(game);
        }
    }
}