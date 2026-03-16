using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cateredByLetsuwi.Data;
using cateredByLetsuwi.Models;
using cateredByLetsuwi.Models.Enums;
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

            // In-memory aggregation keeps SQLite decimal behavior predictable.
            var totalRevenue = bookings.Sum(GetCollectedAmount);
            var revenueThisMonth = bookings
                .Where(b => b.BookingDate >= startOfMonth)
                .Sum(GetCollectedAmount);

            var totalBookings = bookings.Count;
            var upcomingBookings = bookings.Count(b => b.EventDate >= now);
            var unpaidBookings = bookings.Count(b => b.PaymentStatus != PaymentStatus.Paid);
            var averageBookingValue = totalBookings > 0 ? totalRevenue / totalBookings : 0;

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

        private static decimal GetCollectedAmount(Booking booking)
        {
            if (booking.AmountPaid > 0)
            {
                return booking.AmountPaid;
            }

            return booking.PaymentStatus == PaymentStatus.Paid
                ? booking.TotalPrice
                : 0m;
        }
    }
}
