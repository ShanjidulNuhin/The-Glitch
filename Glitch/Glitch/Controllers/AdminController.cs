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

        // Auth Check Helper
        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";

        private IActionResult? RedirectIfNotAdmin()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return null;
        }

        // DASHBOARD
        public async Task<IActionResult> Index()
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var model = new AdminDashboardViewModel
            {
                TotalGames = await _context.Games.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(u => u.Role == "Customer"),
                TotalPurchases = await _context.Purchases.CountAsync(),
                BlockedUsers = await _context.Users.CountAsync(u => u.IsBlocked),
                RecentUsers = await _context.Users.OrderByDescending(u => u.CreatedAt).Take(5)
                    .Select(u => new RecentUserRow { Username = u.Username, Email = u.Email, Role = u.Role, IsBlocked = u.IsBlocked, CreatedAt = u.CreatedAt }).ToListAsync(),
                RecentPurchases = await _context.Purchases.Include(p => p.User).Include(p => p.Game).OrderByDescending(p => p.PurchasedAt).Take(5)
                    .Select(p => new RecentPurchaseRow { Username = p.User.Username, GameTitle = p.Game.Title, PricePaid = p.PricePaid, PurchasedAt = p.PurchasedAt }).ToListAsync()
            };

            return View(model);
        }

        // GAMES LIST
        public async Task<IActionResult> Games()
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var games = await _context.Games.OrderByDescending(g => g.CreatedAt).ToListAsync();
            return View(games);
        }

        // ADD GAME (GET)
        public IActionResult AddGame()
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;
            return View(new GameFormViewModel());
        }

        // ADD GAME (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGame(GameFormViewModel model)
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid) return View(model);

            string? imageFileName = null;
            if (model.ImageFile != null)
            {
                imageFileName = await SaveGameImage(model.ImageFile);
                if (imageFileName == null)
                {
                    ModelState.AddModelError("ImageFile", "Invalid image format or size.");
                    return View(model);
                }
            }

            string? gameFileName = null;
            if (model.GameFileUpload != null)
            {
                gameFileName = await SaveGameFile(model.GameFileUpload);
                if (gameFileName == null)
                {
                    ModelState.AddModelError("GameFileUpload", "Invalid game file format or size.");
                    return View(model);
                }
            }

            var game = new Game
            {
                Title = model.Title,
                Description = model.Description,
                Price = model.Price,
                Genre = model.Genre,
                TrailerUrl = ConvertToEmbedUrl(model.TrailerUrl),
                ImageFileName = imageFileName,
                GameFile = gameFileName,
                IsAvailable = model.IsAvailable,
                CreatedAt = DateTime.Now
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Game added successfully!";
            return RedirectToAction(nameof(Games));
        }

        // EDIT GAME (GET)
        public async Task<IActionResult> EditGame(int id)
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var game = await _context.Games.FindAsync(id);
            if (game == null) return NotFound();

            var model = new GameFormViewModel
            {
                Id = game.Id,
                Title = game.Title,
                Description = game.Description,
                Price = game.Price,
                Genre = game.Genre,
                TrailerUrl = game.TrailerUrl,
                ExistingImage = game.ImageFileName,
                ExistingGameFile = game.GameFile, // ViewModel e ei property thakte hobe
                IsAvailable = game.IsAvailable
            };
            return View(model);
        }

        // EDIT GAME (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGame(GameFormViewModel model)
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            if (!ModelState.IsValid) return View(model);

            var game = await _context.Games.FindAsync(model.Id);
            if (game == null) return NotFound();

            // Handle Image Update
            if (model.ImageFile != null)
            {
                DeleteGameImage(game.ImageFileName);
                game.ImageFileName = await SaveGameImage(model.ImageFile);
            }

            // Handle Game File Update
            if (model.GameFileUpload != null)
            {
                DeleteGameFile(game.GameFile);
                game.GameFile = await SaveGameFile(model.GameFileUpload);
            }

            game.Title = model.Title;
            game.Description = model.Description;
            game.Price = model.Price;
            game.Genre = model.Genre;
            game.TrailerUrl = ConvertToEmbedUrl(model.TrailerUrl);
            game.IsAvailable = model.IsAvailable;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Game updated!";
            return RedirectToAction(nameof(Games));
        }

        // DELETE GAME
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGame(int id)
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var game = await _context.Games.FindAsync(id);
            if (game != null)
            {
                DeleteGameImage(game.ImageFileName);
                DeleteGameFile(game.GameFile);
                _context.Games.Remove(game);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Game deleted!";
            }
            return RedirectToAction(nameof(Games));
        }

        // PROFILE (GET)
        public async Task<IActionResult> Profile()
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr == null) return RedirectToAction("Login", "Account");

            var userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            var model = new AdminProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                ProfileImage = user.ProfileImage
            };
            return View(model);
        }

        // PROFILE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(AdminProfileViewModel model)
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr == null) return RedirectToAction("Login", "Account");

            var userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            model.ProfileImage = user.ProfileImage; // Restore in case of return View

            if (!ModelState.IsValid) return View(model);

            // Check email uniqueness
            var emailTaken = await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId);
            if (emailTaken)
            {
                ModelState.AddModelError("Email", "This email is already used.");
                return View(model);
            }

            // Image handling
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var newImg = await SaveProfileImage(model.ImageFile);
                if (newImg != null)
                {
                    DeleteProfileImage(user.ProfileImage);
                    user.ProfileImage = newImg;
                }
                else
                {
                    ModelState.AddModelError("ImageFile", "Only JPG/PNG/WEBP up to 3MB.");
                    return View(model);
                }
            }

            // Password handling
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (!PasswordHelper.VerifyPassword(model.CurrentPassword ?? "", user.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password incorrect.");
                    return View(model);
                }
                user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
            }

            user.Username = model.Username;
            user.Email = model.Email;

            await _context.SaveChangesAsync();

            // Update session
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Email", user.Email);
            if (!string.IsNullOrEmpty(user.ProfileImage))
            {
                HttpContext.Session.SetString("ProfileImage", user.ProfileImage);
            }

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        // --- USER MANAGEMENT ---

        public async Task<IActionResult> Users()
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var users = await _context.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(int id)
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Id.ToString() != HttpContext.Session.GetString("UserId"))
            {
                user.IsBlocked = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = "User blocked successfully!";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(int id)
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsBlocked = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = "User unblocked successfully!";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteUser(int id)
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.Role = "Admin";
                await _context.SaveChangesAsync();
                TempData["Success"] = "User promoted to Admin!";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Id.ToString() != HttpContext.Session.GetString("UserId"))
            {
                DeleteProfileImage(user.ProfileImage);
                
                // Remove purchases associated with this user
                var purchases = _context.Purchases.Where(p => p.UserId == user.Id);
                _context.Purchases.RemoveRange(purchases);

                // Remove wishlist/favorites items associated with this user
                var favorites = _context.Favorites.Where(w => w.UserId == user.Id);
                _context.Favorites.RemoveRange(favorites);

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "User deleted successfully!";
            }
            return RedirectToAction(nameof(Users));
        }

        // --- HELPER METHODS ---

        private async Task<string?> SaveGameFile(IFormFile file)
        {
            var allowed = new[] { ".zip", ".rar", ".exe", ".msi", ".7z", ".tar", ".gz", ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowed.Contains(ext) || file.Length > 2L * 1024 * 1024 * 1024) return null;

            var fileName = Guid.NewGuid().ToString() + ext;
            var path = Path.Combine(_env.WebRootPath, "uploads", "gamefiles");
            Directory.CreateDirectory(path);

            var filePath = Path.Combine(path, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return fileName;
        }

        private async Task<string?> SaveGameImage(IFormFile file)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowed.Contains(ext) || file.Length > 5 * 1024 * 1024) return null;

            var fileName = Guid.NewGuid().ToString() + ext;
            var path = Path.Combine(_env.WebRootPath, "uploads", "games");
            Directory.CreateDirectory(path);

            using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return fileName;
        }

        private void DeleteGameFile(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            var path = Path.Combine(_env.WebRootPath, "uploads", "gamefiles", fileName);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        private void DeleteGameImage(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            var path = Path.Combine(_env.WebRootPath, "uploads", "games", fileName);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        private async Task<string?> SaveProfileImage(IFormFile file)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowed.Contains(ext) || file.Length > 3 * 1024 * 1024) return null;

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

        private string? ConvertToEmbedUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (url.Contains("youtube.com/embed/")) return url;
            if (url.Contains("v=")) return $"https://www.youtube.com/embed/{url.Split("v=")[1].Split("&")[0]}";
            if (url.Contains("youtu.be/")) return $"https://www.youtube.com/embed/{url.Split("youtu.be/")[1].Split("?")[0]}";
            return url;
        }
    }
}