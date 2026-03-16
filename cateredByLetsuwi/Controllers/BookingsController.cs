using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using cateredByLetsuwi.Data;
using cateredByLetsuwi.Models;
using cateredByLetsuwi.Models.Enums;

namespace cateredByLetsuwi.Controllers
{
    // ✅ Protect everything by default (Admin dashboard + status updates)
    [Authorize(Policy = "AdminOnly")] // or: [Authorize(Roles = "Admin")]
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

            // ===== Business Metrics (SQLite-safe because computed in memory) =====
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
        // PUBLIC BOOKING FORM - GET
        // =========================================
        [AllowAnonymous]
        public IActionResult Create()
        {
            PopulateServicesDropDownList();
            return View();
        }

        // =========================================
        // PUBLIC BOOKING FORM - POST
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Create(
            [Bind("CustomerName,Email,EventDate,NumberOfGuests,ServiceId")] Booking booking)
        {
            if (!ModelState.IsValid)
            {
                PopulateServicesDropDownList(booking.ServiceId);
                return View(booking);
            }

            var service = await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == booking.ServiceId);

            if (service == null)
            {
                ModelState.AddModelError(nameof(Booking.ServiceId), "Selected service does not exist.");
                PopulateServicesDropDownList();
                return View(booking);
            }

            // ==============================
            // BUSINESS LOGIC
            // ==============================
            booking.TotalPrice = service.Price * booking.NumberOfGuests;
            booking.BookingDate = DateTime.UtcNow;

            // Booking starts as pending until admin confirms / payment comes in
            booking.BookingStatus = BookingStatus.Pending;
            booking.PaymentStatus = PaymentStatus.Pending;

            // Payment fields start empty
            booking.PaymentDate = null;
            booking.PaymentReference = null;
            booking.PaymentMethod = null;

            _context.Add(booking);
            await _context.SaveChangesAsync();

            // ✅ Public users shouldn't be redirected into admin dashboard
            // You can change this to a "ThankYou" page later.
            return RedirectToAction(nameof(Create));
        }

        // =========================================
        // ADMIN - Update Status / Payment
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
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound();

            // Update payment status
            booking.PaymentStatus = paymentStatus;

            if (paymentStatus == PaymentStatus.Paid)
            {
                // If paid -> confirm booking automatically
                booking.BookingStatus = BookingStatus.Confirmed;

                booking.PaymentDate = DateTime.UtcNow;
                booking.PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? null : paymentMethod.Trim();
                booking.PaymentReference = string.IsNullOrWhiteSpace(paymentReference) ? null : paymentReference.Trim();
            }
            else
            {
                // Otherwise allow admin to set booking status manually
                booking.BookingStatus = bookingStatus;

                // Only clear payment details if it's NOT paid
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