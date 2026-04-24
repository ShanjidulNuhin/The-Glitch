using Glitch.Data;
using Glitch.Helpers;
using Glitch.Models.Entities;
using Glitch.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Glitch.Controllers
{
    public class PurchaseController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PurchaseController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── Auth check ────────────────────────────────────────
        private bool IsCustomer()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Customer" || role == "Admin";
        }

        // ══════════════════════════════════════════════════════
        // PAYMENT PAGE
        // ══════════════════════════════════════════════════════

        // GET: /Purchase/Pay/5
        // Shows payment confirmation page
        public async Task<IActionResult> Pay(int id)
        {
            // Must be logged in as customer
            if (!IsCustomer())
                return RedirectToAction("Login", "Account");

            var game = await _context.Games.FindAsync(id);

            // Game must exist and be available
            if (game == null || !game.IsAvailable)
                return RedirectToAction("Index", "Home");

            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);

            // Check if already purchased
            var alreadyPurchased = await _context.Purchases
                .AnyAsync(p => p.UserId == userId && p.GameId == id);

            if (alreadyPurchased)
            {
                // Already bought - go to game detail
                TempData["Info"] = "You already own this game!";
                return RedirectToAction("GameDetail", "Home", new { id });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.Balance < game.Price)
            {
                TempData["Error"] = "Your current balance is low.";
                return RedirectToAction("Index", "Home");
            }

            // Build payment model
            var model = new PaymentViewModel
            {
                GameId = game.Id,
                GameTitle = game.Title,
                GameGenre = game.Genre,
                GamePrice = game.Price,
                GameImage = game.ImageFileName
            };

            return View(model);
        }

        // POST: /Purchase/Pay
        // Processes payment form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(PaymentViewModel model)
        {
            if (!IsCustomer())
                return RedirectToAction("Login", "Account");

            var game = await _context.Games.FindAsync(model.GameId);
            if (game == null || !game.IsAvailable)
                return RedirectToAction("Index", "Home");

            // Refill display info in case we return the view
            model.GameTitle = game.Title;
            model.GameGenre = game.Genre;
            model.GamePrice = game.Price;
            model.GameImage = game.ImageFileName;

            if (!ModelState.IsValid)
                return View(model);

            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // ── Verify password ───────────────────────────────
            if (!PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("Password",
                    "Incorrect password. Please try again.");
                return View(model);
            }

            if (model.AmountEntered != game.Price)
            {
                ModelState.AddModelError("AmountEntered",
                    "please enter the currect amount");
                return View(model);
            }

            // ── Check not already purchased ───────────────────
            var alreadyPurchased = await _context.Purchases
                .AnyAsync(p => p.UserId == userId && p.GameId == game.Id);

            if (alreadyPurchased)
            {
                TempData["Info"] = "You already own this game!";
                return RedirectToAction("GameDetail", "Home", new { id = game.Id });
            }

            // ── Check balance ─────────────────────────────────
            if (user.Balance < game.Price)
            {
                ModelState.AddModelError("", "Your current balance is low.");
                return View(model);
            }

            // Deduct balance from user
            user.Balance -= game.Price;

            // Add balance to Admin
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            if (adminUser != null)
            {
                adminUser.Balance += game.Price;
            }

            // ── Save purchase to database ─────────────────────
            var purchase = new Purchase
            {
                UserId = userId,
                GameId = game.Id,
                // Save the price at time of purchase
                PricePaid = game.Price,
                PurchasedAt = DateTime.Now
            };

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            // ── Success → go to game detail ───────────────────
            TempData["Success"] = $"🎉 Purchase successful! You can now download {game.Title}!";
            return RedirectToAction("GameDetail", "Home", new { id = game.Id });
        }

        // ══════════════════════════════════════════════════════
        // DOWNLOAD
        // ══════════════════════════════════════════════════════

        // GET: /Purchase/Download/5
        // Downloads the game file to user's PC
        public async Task<IActionResult> Download(int id)
        {
            // Must be logged in as customer
            if (!IsCustomer())
                return RedirectToAction("Login", "Account");

            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);

            // Verify user has purchased this game
            var purchased = await _context.Purchases
                .AnyAsync(p => p.UserId == userId && p.GameId == id);

            if (!purchased)
            {
                TempData["Error"] = "You must purchase this game before downloading.";
                return RedirectToAction("GameDetail", "Home", new { id });
            }

            var game = await _context.Games.FindAsync(id);
            if (game == null)
                return RedirectToAction("Index", "Home");

            // Check if game file exists
            if (string.IsNullOrEmpty(game.GameFile))
            {
                TempData["Error"] = "Game file is not available yet. Please check back later.";
                return RedirectToAction("GameDetail", "Home", new { id });
            }

            // Build full path to file
            var filePath = Path.Combine(
                _env.WebRootPath, "uploads", "gamefiles", game.GameFile);

            // Check file exists on disk
            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "Game file not found on server. Please contact support.";
                return RedirectToAction("GameDetail", "Home", new { id });
            }

            // Get file extension to set correct content type
            var ext = Path.GetExtension(game.GameFile).ToLower();
            var contentType = GetContentType(ext);

            // Read file bytes
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // Send file to browser as download
            // The downloaded file will be named after the game title
            var downloadName = $"{game.Title.Replace(" ", "_")}{ext}";

            return File(fileBytes, contentType, downloadName);
        }

        // Returns the correct MIME content type for file extension
        private string GetContentType(string ext)
        {
            return ext switch
            {
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".exe" => "application/octet-stream",
                ".msi" => "application/x-msi",
                ".7z" => "application/x-7z-compressed",
                ".tar" => "application/x-tar",
                ".gz" => "application/gzip",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }
    }
}