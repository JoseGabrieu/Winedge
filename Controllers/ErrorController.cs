using Microsoft.AspNetCore.Mvc;

namespace Winedge.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/Forbidden")]
        public IActionResult Forbidden()
        {
            return View();
        }
    }
}
