using Glitch.Data;
using Glitch.Helpers;
using Glitch.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Glitch.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CustomerController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── Auth Check ────────────────────────────────────────
        private bool IsCustomer()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Customer" || role == "Admin";
        }

        private IActionResult? RedirectIfNotCustomer()
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Login", "Account");
            return null;
        }

        // ══════════════════════════════════════════════════════
        // CUSTOMER PROFILE
        // ══════════════════════════════════════════════════════

        // GET: /Customer/Profile
        public async Task<IActionResult> Profile()
        {
            var check = RedirectIfNotCustomer();
            if (check != null) return check;

            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return RedirectToAction("Login", "Account");

            var purchasedCount = await _context.Purchases.CountAsync(p => p.UserId == userId);

            var model = new CustomerProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                ProfileImage = user.ProfileImage,
                PurchasedGamesCount = purchasedCount,
                AchievementPoints = purchasedCount * 5,
                Balance = user.Balance
            };

            return View(model);
        }

        // POST: /Customer/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(CustomerProfileViewModel model)
        {
            var check = RedirectIfNotCustomer();
            if (check != null) return check;

            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return RedirectToAction("Login", "Account");

            model.ProfileImage = user.ProfileImage;

            if (!ModelState.IsValid) return View(model);

            // Check email uniqueness
            var emailTaken = await _context.Users
                .AnyAsync(u => u.Email == model.Email && u.Id != userId);
            if (emailTaken)
            {
                ModelState.AddModelError("Email", "This email is already used");
                return View(model);
            }

            // Check username uniqueness among customers
            var usernameTaken = await _context.Users
                .AnyAsync(u => u.Username == model.Username
                           && u.Role == "Customer"
                           && u.Id != userId);
            if (usernameTaken)
            {
                ModelState.AddModelError("Username",
                    "This username already exists, try another one");
                return View(model);
            }

            // Handle profile image
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var newImg = await SaveProfileImage(model.ImageFile);
                if (newImg == null)
                {
                    ModelState.AddModelError("ImageFile", "Only JPG, PNG or WEBP (max 3MB)");
                    return View(model);
                }
                DeleteProfileImage(user.ProfileImage);
                user.ProfileImage = newImg;
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (!PasswordHelper.VerifyPassword(model.CurrentPassword ?? "", user.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                    return View(model);
                }
                user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
            }

            user.Username = model.Username;
            user.Email = model.Email;

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Email", user.Email);
            if (!string.IsNullOrEmpty(user.ProfileImage))
            {
                HttpContext.Session.SetString("ProfileImage", user.ProfileImage);
            }

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // ══════════════════════════════════════════════════════
        // LIBRARY & WISHLIST
        // ══════════════════════════════════════════════════════

        // GET: /Customer/Library
        public async Task<IActionResult> Library()
        {
            var check = RedirectIfNotCustomer();
            if (check != null) return check;

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");
            
            var userId = int.Parse(userIdStr);

            var purchasedGames = await _context.Purchases
                .Include(p => p.Game)
                .Where(p => p.UserId == userId)
                .Select(p => p.Game)
                .ToListAsync();

            var wishlistGames = await _context.Favorites
                .Include(f => f.Game)
                .Where(f => f.UserId == userId)
                .Select(f => f.Game)
                .ToListAsync();

            var model = new LibraryViewModel
            {
                PurchasedGames = purchasedGames,
                WishlistGames = wishlistGames
            };

            return View(model);
        }

        // POST: /Customer/ToggleWishlist
        [HttpPost]
        public async Task<IActionResult> ToggleWishlist(int gameId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) 
                return Json(new { success = false, message = "Not logged in" });

            var userId = int.Parse(userIdStr);
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.GameId == gameId);

            bool added = false;
            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
            }
            else
            {
                _context.Favorites.Add(new Models.Entities.Favorite { UserId = userId, GameId = gameId });
                added = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, added = added });
        }

        // POST: /Customer/RefundGame
        [HttpPost]
        public async Task<IActionResult> RefundGame(int gameId)
        {
            var check = RedirectIfNotCustomer();
            if (check != null) return check;

            var userIdStr = HttpContext.Session.GetString("UserId");
            var userId = int.Parse(userIdStr!);

            var purchase = await _context.Purchases
                .Include(p => p.Game)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.GameId == gameId);

            if (purchase == null)
            {
                TempData["RefundError"] = "Purchase not found.";
                return RedirectToAction("Library");
            }

            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            // The prompt requests "when admin balance is 0. then show a message" 
            if (adminUser == null || adminUser.Balance == 0 || adminUser.Balance < purchase.PricePaid)
            {
                TempData["RefundError"] = "You can not refunt this game now";
                return RedirectToAction("Library");
            }

            var customer = await _context.Users.FindAsync(userId);
            if (customer != null)
            {
                customer.Balance += purchase.PricePaid;
            }

            adminUser.Balance -= purchase.PricePaid;

            _context.Purchases.Remove(purchase);

            // Remove from wishlist if it exists
            var favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.GameId == gameId);
            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
            }

            // Remove rating if it exists
            var rating = await _context.GameRatings.FirstOrDefaultAsync(r => r.UserId == userId && r.GameId == gameId);
            if (rating != null)
            {
                _context.GameRatings.Remove(rating);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Game refunded successfully!";
            return RedirectToAction("Library");
        }

        // POST: /Customer/RateGame
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateGame(int gameId, int score)
        {
            var check = RedirectIfNotCustomer();
            if (check != null) return check;

            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);

            // Ensure the user actually bought the game
            var ownsGame = await _context.Purchases.AnyAsync(p => p.UserId == userId && p.GameId == gameId);
            if (!ownsGame)
            {
                TempData["Error"] = "You must purchase the game before rating it.";
                return RedirectToAction("GameDetail", "Home", new { id = gameId });
            }

            // Ensure valid score
            if (score < 1 || score > 5)
            {
                TempData["Error"] = "Invalid rating score.";
                return RedirectToAction("GameDetail", "Home", new { id = gameId });
            }

            // Check if already rated
            var existingRating = await _context.GameRatings.FirstOrDefaultAsync(r => r.UserId == userId && r.GameId == gameId);
            if (existingRating != null)
            {
                existingRating.Score = score;
                existingRating.CreatedAt = DateTime.Now;
            }
            else
            {
                _context.GameRatings.Add(new Models.Entities.GameRating
                {
                    UserId = userId,
                    GameId = gameId,
                    Score = score
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thanks for rating this game!";
            return RedirectToAction("GameDetail", "Home", new { id = gameId });
        }

        // ══════════════════════════════════════════════════════
        // ADD BALANCE
        // ══════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> AddBalance()
        {
            var check = RedirectIfNotCustomer();
            if (check != null) return check;

            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var user = await _context.Users.FindAsync(userId);

            var model = new AddBalanceViewModel();
            if (user != null)
            {
                model.DateOfBirth = user.DateOfBirth;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBalance(AddBalanceViewModel model)
        {
            var check = RedirectIfNotCustomer();
            if (check != null) return check;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return RedirectToAction("Login", "Account");

            user.Balance += model.Amount;

            // Save DateOfBirth if it was provided (e.g. from Card payment) and user didn't have it
            if (model.DateOfBirth.HasValue && !user.DateOfBirth.HasValue)
            {
                user.DateOfBirth = model.DateOfBirth.Value;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "money added to your acount";
            return RedirectToAction("Index", "Home");
        }

        // ══════════════════════════════════════════════════════
        // DELETE ACCOUNT
        // ══════════════════════════════════════════════════════

        // GET: /Customer/DeleteAccount
        public IActionResult DeleteAccount()
        {
            var check = RedirectIfNotCustomer();
            if (check != null) return check;

            return View();
        }

        // POST: /Customer/DeleteAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountConfirmed(string password, string confirmation)
        {
            var check = RedirectIfNotCustomer();
            if (check != null) return check;

            // User must type "DELETE" to confirm
            if (confirmation != "DELETE")
            {
                TempData["Error"] = "Please type DELETE to confirm account deletion";
                return RedirectToAction("DeleteAccount");
            }

            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return RedirectToAction("Login", "Account");

            // Verify password before deleting
            if (!PasswordHelper.VerifyPassword(password, user.PasswordHash))
            {
                TempData["Error"] = "Incorrect password. Account not deleted.";
                return RedirectToAction("DeleteAccount");
            }

            // Delete profile image from disk
            DeleteProfileImage(user.ProfileImage);

            // Delete user from database
            // All related cart, favorites, purchases deleted via cascade
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Clear session
            HttpContext.Session.Clear();

            TempData["Success"] = "Your account has been permanently deleted.";
            return RedirectToAction("Login", "Account");
        }

        // ── Image Helpers ─────────────────────────────────────
        private async Task<string?> SaveProfileImage(IFormFile file)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowed.Contains(ext)) return null;
            if (file.Length > 3 * 1024 * 1024) return null;

            var fileName = Guid.NewGuid().ToString() + ext;
            var folder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(folder);

            using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
            await file.CopyToAsync(stream);
            return fileName;
        }

        private void DeleteProfileImage(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            var path = Path.Combine(_env.WebRootPath, "uploads", "profiles", fileName);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }
}