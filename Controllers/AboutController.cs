using Microsoft.AspNetCore.Mvc;

namespace Winedge.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
