using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cateredByLetsuwi.Data;
using cateredByLetsuwi.Models.ViewModels;

namespace cateredByLetsuwi.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var bookings = await _context.Bookings
                .Include(b => b.Service)
                .ToListAsync();

            var totalBookings = bookings.Count;
            var upcomingBookings = bookings.Count(b => b.EventDate >= now);
            

            var totalRevenue = bookings.Sum(b => b.TotalPrice);
            var revenueThisMonth = bookings
                .Where(b => b.BookingDate >= startOfMonth)
                .Sum(b => b.TotalPrice);

            var averageBookingValue = totalBookings > 0
                ? totalRevenue / totalBookings
                : 0;

            var mostPopularService = bookings
                .GroupBy(b => b.Service!.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            var model = new AdminDashboardViewModel
            {
                TotalBookings = totalBookings,
                UpcomingBookings = upcomingBookings,
                
                TotalRevenue = totalRevenue,
                RevenueThisMonth = revenueThisMonth,
                AverageBookingValue = averageBookingValue,
                MostPopularService = mostPopularService
            };

            return View(model);
        }
    }
}