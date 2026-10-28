using Microsoft.AspNetCore.Mvc;
using quanlykho.Models;
using System.Diagnostics;

namespace quanlykho.Controllers
{
    public class HomeController : Controller
    {
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
