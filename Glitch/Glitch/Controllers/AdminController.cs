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

            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            var adminBalance = adminUser != null ? adminUser.Balance : 0;

            var oneWeekAgo = DateTime.Now.AddDays(-7);
            var weeklySales = await _context.Purchases
                .Where(p => p.PurchasedAt >= oneWeekAgo)
                .SumAsync(p => p.PricePaid);

            var mostSellGames = await _context.Purchases
                .Include(p => p.Game)
                .GroupBy(p => p.Game)
                .Select(g => new MostSellGameRow { Title = g.Key.Title, SalesCount = g.Count() })
                .OrderByDescending(g => g.SalesCount)
                .Take(5)
                .ToListAsync();

            var mostRatedGames = await _context.GameRatings
                .Include(r => r.Game)
                .GroupBy(r => r.Game)
                .Select(g => new MostRatedGameRow { Title = g.Key.Title, AverageScore = g.Average(r => r.Score) })
                .OrderByDescending(g => g.AverageScore)
                .Take(5)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalGames = await _context.Games.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(u => u.Role == "Customer"),
                TotalPurchases = await _context.Purchases.CountAsync(),
                BlockedUsers = await _context.Users.CountAsync(u => u.IsBlocked),
                TotalBalance = adminBalance,
                WeeklySales = weeklySales,
                MostSellGames = mostSellGames,
                MostRatedGames = mostRatedGames,
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
                CreatedAt = DateTime.Now,
                ReqSize = model.ReqSize,
                ReqOS = model.ReqOS,
                ReqProcessor = model.ReqProcessor,
                ReqMemory = model.ReqMemory,
                ReqGraphics = model.ReqGraphics,
                ReqStorage = model.ReqStorage
            };

            if (model.ScreenshotFiles != null && model.ScreenshotFiles.Count > 0)
            {
                foreach (var file in model.ScreenshotFiles)
                {
                    var ssName = await SaveGameImage(file);
                    if (ssName != null)
                    {
                        game.Screenshots.Add(new GameScreenshot { ImageFileName = ssName });
                    }
                }
            }

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
                IsAvailable = game.IsAvailable,
                ReqSize = game.ReqSize,
                ReqOS = game.ReqOS,
                ReqProcessor = game.ReqProcessor,
                ReqMemory = game.ReqMemory,
                ReqGraphics = game.ReqGraphics,
                ReqStorage = game.ReqStorage,
                ExistingScreenshots = await _context.GameScreenshots
                    .Where(s => s.GameId == id)
                    .Select(s => s.ImageFileName)
                    .ToListAsync()
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
            
            game.ReqSize = model.ReqSize;
            game.ReqOS = model.ReqOS;
            game.ReqProcessor = model.ReqProcessor;
            game.ReqMemory = model.ReqMemory;
            game.ReqGraphics = model.ReqGraphics;
            game.ReqStorage = model.ReqStorage;

            if (model.ScreenshotFiles != null && model.ScreenshotFiles.Count > 0)
            {
                foreach (var file in model.ScreenshotFiles)
                {
                    var ssName = await SaveGameImage(file);
                    if (ssName != null)
                    {
                        _context.GameScreenshots.Add(new GameScreenshot { GameId = game.Id, ImageFileName = ssName });
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Game updated!";
            return RedirectToAction(nameof(Games));
        }

        // POST: Delete Screenshot
        [HttpPost]
        public async Task<IActionResult> DeleteScreenshot(int gameId, string filename)
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return Json(new { success = false });

            var screenshot = await _context.GameScreenshots.FirstOrDefaultAsync(s => s.GameId == gameId && s.ImageFileName == filename);
            if (screenshot != null)
            {
                DeleteGameImage(screenshot.ImageFileName);
                _context.GameScreenshots.Remove(screenshot);
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        // DELETE GAME
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGame(int id)
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var game = await _context.Games.Include(g => g.Screenshots).FirstOrDefaultAsync(g => g.Id == id);
            if (game != null)
            {
                foreach(var ss in game.Screenshots)
                {
                    DeleteGameImage(ss.ImageFileName);
                }
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
                ProfileImage = user.ProfileImage,
                TotalGames = await _context.Games.CountAsync(),
                TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer"),
                TotalPurchases = await _context.Purchases.CountAsync(),
                AdminBalance = user.Balance
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
            model.TotalGames = await _context.Games.CountAsync();
            model.TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");
            model.TotalPurchases = await _context.Purchases.CountAsync();
            model.AdminBalance = user.Balance;

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
        public async Task<IActionResult> DemoteAdmin(int id)
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Id.ToString() != HttpContext.Session.GetString("UserId"))
            {
                user.Role = "Customer";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Admin demoted to Customer!";
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

        // --- WITHDRAW BALANCE ---
        public async Task<IActionResult> WithdrawBalance()
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var adminUser = await _context.Users.FindAsync(userId);
            
            if (adminUser == null) return RedirectToAction("Login", "Account");

            var model = new WithdrawViewModel
            {
                AvailableBalance = adminUser.Balance,
                WithdrawableBalance = adminUser.Balance * 0.5m
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawBalance(WithdrawViewModel model)
        {
            var authCheck = RedirectIfNotAdmin();
            if (authCheck != null) return authCheck;

            var userId = int.Parse(HttpContext.Session.GetString("UserId")!);
            var adminUser = await _context.Users.FindAsync(userId);
            
            if (adminUser == null) return RedirectToAction("Login", "Account");

            model.AvailableBalance = adminUser.Balance;
            model.WithdrawableBalance = adminUser.Balance * 0.5m;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Amount > model.WithdrawableBalance)
            {
                ModelState.AddModelError("Amount", "You cannot withdraw more than your withdrawable balance.");
                return View(model);
            }

            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Amount must be greater than zero.");
                return View(model);
            }

            // Deduct the balance
            adminUser.Balance -= model.Amount;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Successfully withdrew ${model.Amount:F2}.";
            return RedirectToAction("WithdrawBalance");
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