using Glitch.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────

// Adds MVC (Controllers + Views) support
builder.Services.AddControllersWithViews();

// Connects to SQL Server using connection string from appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration
        .GetConnectionString("The-Glitch")));

// Allows us to access HttpContext anywhere in the app
builder.Services.AddHttpContextAccessor();

// Enables session - used for login system
builder.Services.AddSession(options =>
{
    // Session expires after 30 minutes of inactivity
    options.IdleTimeout = TimeSpan.FromMinutes(30);

    // Cookie cannot be accessed by JavaScript - security protection
    options.Cookie.HttpOnly = true;

    // Session cookie is always created even without user consent
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────
// Order matters here! Do not change the order

if (!app.Environment.IsDevelopment())
{
    // Shows a friendly error page in production
    app.UseExceptionHandler("/Home/Error");

    // Forces HTTPS in production
    app.UseHsts();
}

// Redirects HTTP requests to HTTPS
app.UseHttpsRedirection();

// Serves static files like CSS, JS, images from wwwroot folder
app.UseStaticFiles();

// Figures out which controller/action to call
app.UseRouting();

// Enables session MUST be after UseRouting and before UseAuthorization
app.UseSession();

// Checks if user is allowed to access a resource
app.UseAuthorization();

// Maps static assets (new .NET 10 feature)
app.MapStaticAssets();

// Sets the default route: Home controller, Index action
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();