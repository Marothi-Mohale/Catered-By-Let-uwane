using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using cateredByLetsuwi.Data;
using cateredByLetsuwi.Models;
using cateredByLetsuwi.Models.Enums;

namespace cateredByLetsuwi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Service)
                .OrderByDescending(b => b.EventDate)
                .ToListAsync();

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

        [AllowAnonymous]
        public IActionResult Create()
        {
            PopulateServicesDropDownList();
            return View();
        }

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

            booking.TotalPrice = service.Price * booking.NumberOfGuests;
            booking.BookingDate = DateTime.UtcNow;
            booking.BookingStatus = BookingStatus.Pending;
            booking.PaymentStatus = PaymentStatus.Pending;
            booking.PaymentDate = null;
            booking.PaymentReference = null;
            booking.PaymentMethod = null;

            _context.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Create));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
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

            booking.PaymentStatus = paymentStatus;

            if (paymentStatus == PaymentStatus.Paid)
            {
                booking.BookingStatus = BookingStatus.Confirmed;
                booking.PaymentDate = DateTime.UtcNow;
                booking.PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? null : paymentMethod.Trim();
                booking.PaymentReference = string.IsNullOrWhiteSpace(paymentReference) ? null : paymentReference.Trim();
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
