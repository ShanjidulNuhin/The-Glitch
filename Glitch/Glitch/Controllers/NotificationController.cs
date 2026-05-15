using Glitch.Data;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Glitch.Controllers
{
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Read(int id)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null && notification.UserId.ToString() == userIdStr)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                
                if (!string.IsNullOrEmpty(notification.LinkUrl))
                {
                    return Redirect(notification.LinkUrl);
                }
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
