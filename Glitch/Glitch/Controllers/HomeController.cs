using Microsoft.AspNetCore.Mvc;

namespace Glitch.Controllers
{
    public class HomeController : Controller
    {
        // GET: /Home/Index
        // This is the main landing page
        public IActionResult Index()
        {
            return View();
        }
    }
}