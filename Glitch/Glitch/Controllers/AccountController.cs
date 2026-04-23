using Glitch.Data;
using Glitch.Helpers;
using Glitch.Models.Entities;
using Glitch.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Glitch.Controllers
{
    public class AccountController : Controller
    {
        // _context gives us access to the database
        private readonly AppDbContext _context;

        // Constructor - ASP.NET automatically injects the database context
        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // ── REGISTER ─────────────────────────────────────────────

        // GET: /Account/Register
        // Shows the registration form
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            // Check if any users exist in the database
            var userExists = await _context.Users.AnyAsync();

            // Create the form model
            var model = new RegisterViewModel
            {
                // If no users exist, this is the first user
                // Show role selection dropdown
                IsFirstUser = !userExists
            };

            return View(model);
        }

        // POST: /Account/Register
        // Processes the registration form when submitted
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Check if any admin exists in the database
            var adminExists = await _context.Users
                .AnyAsync(u => u.Role == "Admin");

            // Check if any users exist at all
            var userExists = await _context.Users.AnyAsync();

            // Set IsFirstUser so view renders correctly if we return it
            model.IsFirstUser = !userExists;

            // If form data is invalid, return form with error messages
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ── Email uniqueness check ────────────────────────────
            // Check if email is already registered
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == model.Email);

            if (emailExists)
            {
                // Add error message under email field
                ModelState.AddModelError("Email", "This email is already registered");
                return View(model);
            }

            // ── Username uniqueness check (Customers only) ────────
            // Admin and Customer can share usernames
            // But two customers cannot have the same username
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == model.Username
                          && u.Role == "Customer");

            if (usernameExists)
            {
                // Add error message under username field
                ModelState.AddModelError("Username",
                    "This username already exists, try another one");
                return View(model);
            }

            // ── Role assignment logic ─────────────────────────────
            string assignedRole;

            if (!userExists)
            {
                // No users exist = first user, let them choose role
                assignedRole = model.Role;
            }
            else if (!adminExists)
            {
                // Users exist but no admin yet
                // Next person can still become admin
                assignedRole = model.Role;
            }
            else
            {
                // Admin already exists
                // Force all new registrations to be Customer
                assignedRole = "Customer";
            }

            // ── Create new user ───────────────────────────────────
            var user = new User
            {
                Username = model.Username,
                Email = model.Email,

                // Hash the password before saving - never store plain text
                PasswordHash = PasswordHelper.HashPassword(model.Password),

                Role = assignedRole,
                IsBlocked = false,
                CreatedAt = DateTime.Now
            };

            // Add user to database
            _context.Users.Add(user);

            // Save changes to database
            await _context.SaveChangesAsync();

            // Redirect to login page after successful registration
            TempData["Success"] = "Registration successful! Please login.";
            return RedirectToAction("Login");
        }

        // ── LOGIN ─────────────────────────────────────────────────

        // GET: /Account/Login
        // Shows the login form
        [HttpGet]
        public IActionResult Login()
        {
            // If user is already logged in, redirect them
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        // Processes the login form when submitted
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // If form data is invalid, return form with errors
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find user by email in database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            // If no user found with that email
            if (user == null)
            {
                ModelState.AddModelError("",
                    "Invalid email or password");
                return View(model);
            }

            // Check if password matches
            var passwordMatch = PasswordHelper
                .VerifyPassword(model.Password, user.PasswordHash);

            if (!passwordMatch)
            {
                ModelState.AddModelError("",
                    "Invalid email or password");
                return View(model);
            }

            // Check if user is blocked by admin
            if (user.IsBlocked)
            {
                ModelState.AddModelError("",
                    "Your account has been blocked. Please contact support.");
                return View(model);
            }

            // ── Create session ────────────────────────────────────
            // Store user info in session so we know who is logged in
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("Email", user.Email);
            if (!string.IsNullOrEmpty(user.ProfileImage))
            {
                HttpContext.Session.SetString("ProfileImage", user.ProfileImage);
            }

            // Redirect based on role
            if (user.Role == "Admin")
            {
                // Admin goes to admin dashboard
                return RedirectToAction("Index", "Admin");
            }
            else
            {
                // Customer goes to home page
                return RedirectToAction("Index", "Home");
            }
        }

        // ── LOGOUT ───────────────────────────────────────────────

        // POST: /Account/Logout
        public IActionResult Logout()
        {
            // Clear all session data
            HttpContext.Session.Clear();

            // Redirect to login page
            return RedirectToAction("Login");
        }
    }
}