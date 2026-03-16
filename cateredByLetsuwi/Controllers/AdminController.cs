using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cateredByLetsuwi.Data;
using cateredByLetsuwi.Models.ViewModels;

namespace cateredByLetsuwi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = await BuildDashboardModelAsync();
            return View(model);
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = await BuildDashboardModelAsync();
            return View(model);
        }

        private async Task<AdminDashboardViewModel> BuildDashboardModelAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var bookings = await _context.Bookings
                .Include(b => b.Service)
                .ToListAsync();

            var totalBookings = bookings.Count;
            var upcomingBookings = bookings.Count(b => b.EventDate >= now);
            var unpaidBookings = bookings.Count(b => b.PaymentStatus != Models.Enums.PaymentStatus.Paid);

            var totalRevenue = bookings.Sum(b => b.TotalPrice);
            var revenueThisMonth = bookings
                .Where(b => b.BookingDate >= startOfMonth)
                .Sum(b => b.TotalPrice);

            var averageBookingValue = totalBookings > 0
                ? totalRevenue / totalBookings
                : 0;

            var mostPopularService = bookings
                .Where(b => b.Service != null)
                .GroupBy(b => b.Service!.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            return new AdminDashboardViewModel
            {
                TotalBookings = totalBookings,
                UpcomingBookings = upcomingBookings,
                UnpaidBookings = unpaidBookings,
                TotalRevenue = totalRevenue,
                RevenueThisMonth = revenueThisMonth,
                AverageBookingValue = averageBookingValue,
                MostPopularService = mostPopularService
            };
        }
    }
}
