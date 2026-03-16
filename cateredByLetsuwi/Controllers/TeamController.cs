using Microsoft.AspNetCore.Mvc;

namespace cateredByLetsuwi.Controllers
{
    public class TeamController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
