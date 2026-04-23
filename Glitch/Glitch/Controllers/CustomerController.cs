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
        private bool IsCustomer() =>
            HttpContext.Session.GetString("Role") == "Customer";

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

            var model = new CustomerProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                ProfileImage = user.ProfileImage
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

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
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