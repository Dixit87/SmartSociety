using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using System.Diagnostics;

namespace SmartSociety.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ChangeRole(string role)
        {
            Response.Cookies.Append("MockRole", role, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(1)
            });
            var referer = Request.Headers["Referer"].ToString();
            return Redirect(!string.IsNullOrEmpty(referer) ? referer : "/");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
