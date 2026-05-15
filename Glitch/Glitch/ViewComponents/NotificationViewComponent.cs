using Glitch.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Glitch.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public NotificationViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return View("Default", new List<Models.Entities.Notification>());
            }

            int userId = int.Parse(userIdStr);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();

            return View("Default", notifications);
        }
    }
}
