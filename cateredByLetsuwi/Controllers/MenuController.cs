using Microsoft.AspNetCore.Mvc;
using cateredByLetsuwi.Data;
using Microsoft.EntityFrameworkCore;

namespace cateredByLetsuwi.Controllers
{
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MenuController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var services = await _context.Services.ToListAsync();
            return View(services);
        }
    }
}