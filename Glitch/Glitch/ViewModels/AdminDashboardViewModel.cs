namespace Glitch.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        // Total counts for stat cards
        public int TotalGames { get; set; }
        public int TotalUsers { get; set; }
        public int TotalPurchases { get; set; }
        public int BlockedUsers { get; set; }

        // Recent activity
        public List<RecentUserRow> RecentUsers { get; set; } = new();
        public List<RecentPurchaseRow> RecentPurchases { get; set; } = new();
    }

    public class RecentUserRow
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RecentPurchaseRow
    {
        public string Username { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public decimal PricePaid { get; set; }
        public DateTime PurchasedAt { get; set; }
    }
}