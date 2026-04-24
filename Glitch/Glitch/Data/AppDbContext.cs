using Glitch.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glitch.Data
{
    public class AppDbContext : DbContext
    {
        // Constructor - receives database options from Program.cs
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Each DbSet = one table in the database
        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<AlternativeEmail> AlternativeEmails { get; set; }
        public DbSet<GameRating> GameRatings { get; set; }
        public DbSet<GameScreenshot> GameScreenshots { get; set; }

        // OnModelCreating = fine-tune table rules that
        // cannot be expressed with data annotations alone
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── User table rules ──────────────────────────────────
            modelBuilder.Entity<User>(entity =>
            {
                // Email must be unique across all users
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // ── Cart rules ────────────────────────────────────────
            modelBuilder.Entity<Cart>(entity =>
            {
                // One user can have many cart items
                entity.HasOne(c => c.User)
                      .WithMany(u => u.CartItems)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                // Cascade = if user is deleted, their cart is deleted too

                // One game can be in many carts
                entity.HasOne(c => c.Game)
                      .WithMany(g => g.CartItems)
                      .HasForeignKey(c => c.GameId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Same game cannot be added twice to the same user's cart
                entity.HasIndex(c => new { c.UserId, c.GameId }).IsUnique();
            });

            // ── Favorite rules ────────────────────────────────────
            modelBuilder.Entity<Favorite>(entity =>
            {
                entity.HasOne(f => f.User)
                      .WithMany(u => u.Favorites)
                      .HasForeignKey(f => f.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Game)
                      .WithMany(g => g.Favorites)
                      .HasForeignKey(f => f.GameId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Same game cannot be favorited twice by same user
                entity.HasIndex(f => new { f.UserId, f.GameId }).IsUnique();
            });

            // ── Purchase rules ────────────────────────────────────
            modelBuilder.Entity<Purchase>(entity =>
            {
                entity.HasOne(p => p.User)
                      .WithMany(u => u.Purchases)
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Game)
                      .WithMany(g => g.Purchases)
                      .HasForeignKey(p => p.GameId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── GameRating rules ──────────────────────────────────
            modelBuilder.Entity<GameRating>(entity =>
            {
                entity.HasOne(r => r.User)
                      .WithMany(u => u.Ratings)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Game)
                      .WithMany(g => g.Ratings)
                      .HasForeignKey(r => r.GameId)
                      .OnDelete(DeleteBehavior.Cascade);

                // A user can rate a specific game only once
                entity.HasIndex(r => new { r.UserId, r.GameId }).IsUnique();
            });

            // ── AlternativeEmail rules ────────────────────────────
            modelBuilder.Entity<AlternativeEmail>(entity =>
            {
                entity.HasOne(a => a.User)
                      .WithMany(u => u.AlternativeEmails)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Each alternative email must be unique
                entity.HasIndex(a => a.Email).IsUnique();
            });

            // ── GameScreenshot rules ──────────────────────────────
            modelBuilder.Entity<GameScreenshot>(entity =>
            {
                entity.HasOne(gs => gs.Game)
                      .WithMany(g => g.Screenshots)
                      .HasForeignKey(gs => gs.GameId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}