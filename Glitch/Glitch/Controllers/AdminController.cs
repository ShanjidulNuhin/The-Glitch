using Glitch.Data;
using Glitch.Helpers;
using Glitch.Models.Entities;
using Glitch.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Glitch.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── Auth Check ────────────────────────────────────────
        private bool IsAdmin() =>
            HttpContext.Session.GetString("Role") == "Admin";

        private IActionResult? RedirectIfNotAdmin()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");
            return null;
        }

        // ══════════════════════════════════════════════════════
        // DASHBOARD
        // ══════════════════════════════════════════════════════
        public async Task<IActionResult> Index()
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            ViewData["Title"] = "Dashboard";
            ViewData["Subtitle"] = "Welcome back! Here's what's happening.";
            ViewData["ActivePage"] = "Dashboard";

            var model = new AdminDashboardViewModel
            {
                TotalGames = await _context.Games.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(u => u.Role == "Customer"),
                TotalPurchases = await _context.Purchases.CountAsync(),
                BlockedUsers = await _context.Users.CountAsync(u => u.IsBlocked),

                RecentUsers = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .Select(u => new RecentUserRow
                    {
                        Username = u.Username,
                        Email = u.Email,
                        Role = u.Role,
                        IsBlocked = u.IsBlocked,
                        CreatedAt = u.CreatedAt
                    })
                    .ToListAsync(),

                RecentPurchases = await _context.Purchases
                    .Include(p => p.User)
                    .Include(p => p.Game)
                    .OrderByDescending(p => p.PurchasedAt)
                    .Take(5)
                    .Select(p => new RecentPurchaseRow
                    {
                        Username = p.User.Username,
                        GameTitle = p.Game.Title,
                        PricePaid = p.PricePaid,
                        PurchasedAt = p.PurchasedAt
                    })
                    .ToListAsync()
            };

            return View(model);
        }

        // ══════════════════════════════════════════════════════
        // GAMES
        // ══════════════════════════════════════════════════════
        public async Task<IActionResult> Games()
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            ViewData["Title"] = "Games Management";
            ViewData["Subtitle"] = "Add, edit and delete games";
            ViewData["ActivePage"] = "Games";

            var games = await _context.Games
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            return View(games);
        }

        public IActionResult AddGame()
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            ViewData["Title"] = "Add New Game";
            ViewData["Subtitle"] = "Fill in the game details below";
            ViewData["ActivePage"] = "Games";

            return View(new GameFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGame(GameFormViewModel model)
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            ViewData["Title"] = "Add New Game";
            ViewData["Subtitle"] = "Fill in the game details below";
            ViewData["ActivePage"] = "Games";

            if (!ModelState.IsValid) return View(model);

            string? imageFileName = null;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                imageFileName = await SaveGameImage(model.ImageFile);
                if (imageFileName == null)
                {
                    ModelState.AddModelError("ImageFile",
                        "Only JPG, PNG or WEBP images are allowed (max 5MB)");
                    return View(model);
                }
            }

            // Convert YouTube URL to embed format
            var trailerEmbed = ConvertToEmbedUrl(model.TrailerUrl);

            var game = new Game
            {
                Title = model.Title,
                Description = model.Description,
                Price = model.Price,
                Genre = model.Genre,
                TrailerUrl = trailerEmbed,
                ImageFileName = imageFileName,
                IsAvailable = model.IsAvailable,
                CreatedAt = DateTime.Now
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Game '{game.Title}' added successfully!";
            return RedirectToAction("Games");
        }

        public async Task<IActionResult> EditGame(int id)
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            var game = await _context.Games.FindAsync(id);
            if (game == null)
            {
                TempData["Error"] = "Game not found!";
                return RedirectToAction("Games");
            }

            ViewData["Title"] = "Edit Game";
            ViewData["Subtitle"] = $"Editing: {game.Title}";
            ViewData["ActivePage"] = "Games";

            var model = new GameFormViewModel
            {
                Id = game.Id,
                Title = game.Title,
                Description = game.Description,
                Price = game.Price,
                Genre = game.Genre,
                TrailerUrl = game.TrailerUrl,
                ExistingImage = game.ImageFileName,
                IsAvailable = game.IsAvailable
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGame(GameFormViewModel model)
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            ViewData["Title"] = "Edit Game";
            ViewData["Subtitle"] = "Update the game details";
            ViewData["ActivePage"] = "Games";

            if (!ModelState.IsValid) return View(model);

            var game = await _context.Games.FindAsync(model.Id);
            if (game == null)
            {
                TempData["Error"] = "Game not found!";
                return RedirectToAction("Games");
            }

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                DeleteGameImage(game.ImageFileName);
                var newImage = await SaveGameImage(model.ImageFile);
                if (newImage == null)
                {
                    ModelState.AddModelError("ImageFile",
                        "Only JPG, PNG or WEBP images are allowed (max 5MB)");
                    model.ExistingImage = game.ImageFileName;
                    return View(model);
                }
                game.ImageFileName = newImage;
            }

            game.Title = model.Title;
            game.Description = model.Description;
            game.Price = model.Price;
            game.Genre = model.Genre;
            game.TrailerUrl = ConvertToEmbedUrl(model.TrailerUrl);
            game.IsAvailable = model.IsAvailable;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Game '{game.Title}' updated successfully!";
            return RedirectToAction("Games");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGame(int id)
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            var game = await _context.Games.FindAsync(id);
            if (game == null)
            {
                TempData["Error"] = "Game not found!";
                return RedirectToAction("Games");
            }

            DeleteGameImage(game.ImageFileName);
            _context.Games.Remove(game);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Game '{game.Title}' deleted!";
            return RedirectToAction("Games");
        }

        // ══════════════════════════════════════════════════════
        // USERS
        // ══════════════════════════════════════════════════════
        public async Task<IActionResult> Users()
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            ViewData["Title"] = "Users Management";
            ViewData["Subtitle"] = "Manage all registered users";
            ViewData["ActivePage"] = "Users";

            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(int id)
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            var user = await _context.Users.FindAsync(id);
            if (user == null) { TempData["Error"] = "User not found!"; return RedirectToAction("Users"); }

            var adminId = HttpContext.Session.GetString("UserId");
            if (user.Id.ToString() == adminId) { TempData["Error"] = "You cannot block yourself!"; return RedirectToAction("Users"); }

            user.IsBlocked = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"User '{user.Username}' has been blocked.";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(int id)
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            var user = await _context.Users.FindAsync(id);
            if (user == null) { TempData["Error"] = "User not found!"; return RedirectToAction("Users"); }

            user.IsBlocked = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"User '{user.Username}' has been unblocked.";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteUser(int id)
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            var user = await _context.Users.FindAsync(id);
            if (user == null) { TempData["Error"] = "User not found!"; return RedirectToAction("Users"); }
            if (user.Role == "Admin") { TempData["Error"] = "Already an Admin!"; return RedirectToAction("Users"); }

            user.Role = "Admin";
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{user.Username}' promoted to Admin!";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            var user = await _context.Users.FindAsync(id);
            if (user == null) { TempData["Error"] = "User not found!"; return RedirectToAction("Users"); }

            var adminId = HttpContext.Session.GetString("UserId");
            if (user.Id.ToString() == adminId) { TempData["Error"] = "You cannot delete yourself!"; return RedirectToAction("Users"); }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"User '{user.Username}' deleted.";
            return RedirectToAction("Users");
        }

        // ══════════════════════════════════════════════════════
        // ADMIN PROFILE
        // ══════════════════════════════════════════════════════

        // GET: /Admin/Profile
        public async Task<IActionResult> Profile()
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            ViewData["Title"] = "My Profile";
            ViewData["Subtitle"] = "Update your admin account details";
            ViewData["ActivePage"] = "Profile";

            var adminId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var admin = await _context.Users.FindAsync(adminId);

            if (admin == null) return RedirectToAction("Login", "Account");

            var model = new AdminProfileViewModel
            {
                Username = admin.Username,
                Email = admin.Email,
                ProfileImage = admin.ProfileImage
            };

            return View(model);
        }

        // POST: /Admin/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(AdminProfileViewModel model)
        {
            var check = RedirectIfNotAdmin();
            if (check != null) return check;

            ViewData["Title"] = "My Profile";
            ViewData["Subtitle"] = "Update your admin account details";
            ViewData["ActivePage"] = "Profile";

            var adminId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var admin = await _context.Users.FindAsync(adminId);

            if (admin == null) return RedirectToAction("Login", "Account");

            // Keep existing image in model if not uploading new one
            model.ProfileImage = admin.ProfileImage;

            if (!ModelState.IsValid) return View(model);

            // Check email uniqueness (excluding self)
            var emailTaken = await _context.Users
                .AnyAsync(u => u.Email == model.Email && u.Id != adminId);
            if (emailTaken)
            {
                ModelState.AddModelError("Email", "This email is already used by another account");
                return View(model);
            }

            // Handle profile image upload
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var newImg = await SaveProfileImage(model.ImageFile);
                if (newImg == null)
                {
                    ModelState.AddModelError("ImageFile", "Only JPG, PNG or WEBP (max 3MB)");
                    return View(model);
                }
                // Delete old profile image
                DeleteProfileImage(admin.ProfileImage);
                admin.ProfileImage = newImg;
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                // Verify current password first
                if (!PasswordHelper.VerifyPassword(model.CurrentPassword ?? "", admin.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                    return View(model);
                }
                admin.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
            }

            // Update fields
            admin.Username = model.Username;
            admin.Email = model.Email;

            await _context.SaveChangesAsync();

            // Update session with new username
            HttpContext.Session.SetString("Username", admin.Username);
            HttpContext.Session.SetString("Email", admin.Email);

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // ══════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════

        // Converts YouTube watch URL to embed URL for iframe
        // Input:  https://www.youtube.com/watch?v=XXXXX
        // Output: https://www.youtube.com/embed/XXXXX
        private string? ConvertToEmbedUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            // Already an embed URL
            if (url.Contains("youtube.com/embed/")) return url;

            // Standard watch URL: youtube.com/watch?v=XXXXX
            if (url.Contains("youtube.com/watch?v="))
            {
                var videoId = url.Split("v=")[1].Split("&")[0];
                return $"https://www.youtube.com/embed/{videoId}";
            }

            // Short URL: youtu.be/XXXXX
            if (url.Contains("youtu.be/"))
            {
                var videoId = url.Split("youtu.be/")[1].Split("?")[0];
                return $"https://www.youtube.com/embed/{videoId}";
            }

            return url;
        }

        private async Task<string?> SaveGameImage(IFormFile file)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowed.Contains(ext)) return null;
            if (file.Length > 5 * 1024 * 1024) return null;

            var fileName = Guid.NewGuid().ToString() + ext;
            var folder = Path.Combine(_env.WebRootPath, "uploads", "games");
            Directory.CreateDirectory(folder);

            using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
            await file.CopyToAsync(stream);
            return fileName;
        }

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

        private void DeleteGameImage(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            var path = Path.Combine(_env.WebRootPath, "uploads", "games", fileName);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        private void DeleteProfileImage(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            var path = Path.Combine(_env.WebRootPath, "uploads", "profiles", fileName);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }
}