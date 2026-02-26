using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using cateredByLetsuwi.Data;
using cateredByLetsuwi.Models;
using cateredByLetsuwi.Models.Enums;

namespace cateredByLetsuwi.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================
        // ADMIN DASHBOARD - List All Bookings
        // =========================================
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Service)
                .OrderByDescending(b => b.EventDate)
                .ToListAsync();

            // ===== Business Metrics (SQLite-safe) =====
            ViewBag.TotalRevenue = bookings
                .Where(b => b.PaymentStatus == PaymentStatus.Paid)
                .Sum(b => b.TotalPrice);

            ViewBag.TotalBookings = bookings.Count;

            ViewBag.PendingBookings = bookings
                .Count(b => b.BookingStatus == BookingStatus.Pending);

            ViewBag.UnpaidBookings = bookings
                .Count(b => b.PaymentStatus == PaymentStatus.Pending);

            return View(bookings);
        }

        // =========================================
        // GET: Bookings/Create
        // =========================================
        public IActionResult Create()
        {
            PopulateServicesDropDownList();
            return View();
        }

        // =========================================
        // POST: Bookings/Create
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CustomerName,Email,EventDate,NumberOfGuests,ServiceId")] Booking booking)
        {
            if (!ModelState.IsValid)
            {
                PopulateServicesDropDownList(booking.ServiceId);
                return View(booking);
            }

            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == booking.ServiceId);

            if (service == null)
            {
                ModelState.AddModelError("", "Selected service does not exist.");
                PopulateServicesDropDownList();
                return View(booking);
            }

            // ==============================
            // BUSINESS LOGIC
            // ==============================
            booking.TotalPrice = service.Price * booking.NumberOfGuests;
            booking.BookingDate = DateTime.UtcNow;
            booking.BookingStatus = BookingStatus.Pending;
            booking.PaymentStatus = PaymentStatus.Pending;
            booking.PaymentDate = null;
            booking.PaymentReference = null;
            booking.PaymentMethod = null;

            _context.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // POST: Bookings/UpdateStatus
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            BookingStatus bookingStatus,
            PaymentStatus paymentStatus,
            string? paymentMethod,
            string? paymentReference)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
                return NotFound();

            booking.PaymentStatus = paymentStatus;

            // If marked as Paid → auto-confirm booking
            if (paymentStatus == PaymentStatus.Paid)
            {
                booking.BookingStatus = BookingStatus.Confirmed;
                booking.PaymentDate = DateTime.UtcNow;
                booking.PaymentMethod = paymentMethod;
                booking.PaymentReference = paymentReference;
            }
            else
            {
                booking.BookingStatus = bookingStatus;
                booking.PaymentDate = null;
                booking.PaymentMethod = null;
                booking.PaymentReference = null;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // Helper: Populate Service Dropdown
        // =========================================
        private void PopulateServicesDropDownList(object? selectedService = null)
        {
            var servicesQuery = _context.Services
                .OrderBy(s => s.Name)
                .AsNoTracking();

            ViewData["ServiceId"] = new SelectList(
                servicesQuery,
                "Id",
                "Name",
                selectedService);
        }
    }
}