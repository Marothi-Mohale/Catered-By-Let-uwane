using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using cateredByLetsuwi.Data;
using cateredByLetsuwi.Models;
using cateredByLetsuwi.Models.Enums;
using cateredByLetsuwi.Models.ViewModels;

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

            // Compute in-memory for SQLite-safe decimal handling.
            ViewBag.TotalRevenue = bookings.Sum(GetCollectedAmount);
            ViewBag.TotalBookings = bookings.Count;
            ViewBag.PendingBookings = bookings.Count(b => b.BookingStatus == BookingStatus.Pending);
            ViewBag.UnpaidBookings = bookings.Count(b => b.PaymentStatus != PaymentStatus.Paid);

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
            booking.AmountPaid = 0;
            booking.PaymentDate = null;
            booking.PaymentReference = null;
            booking.PaymentMethod = null;

            _context.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Create));
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> RecordPayment(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            var model = new RecordPaymentViewModel
            {
                BookingId = booking.Id,
                CustomerName = booking.CustomerName,
                ServiceName = booking.Service?.Name ?? "N/A",
                TotalPrice = booking.TotalPrice,
                AmountPaid = booking.AmountPaid,
                PaymentStatus = booking.PaymentStatus,
                PaymentMethod = booking.PaymentMethod,
                PaymentReference = booking.PaymentReference,
                PaymentDate = booking.PaymentDate
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> RecordPayment(RecordPaymentViewModel model)
        {
            var booking = await _context.Bookings
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (booking == null)
            {
                return NotFound();
            }

            if (model.AmountPaid < 0)
            {
                ModelState.AddModelError(nameof(model.AmountPaid), "Amount paid cannot be negative.");
            }

            if (model.AmountPaid > booking.TotalPrice)
            {
                ModelState.AddModelError(nameof(model.AmountPaid), "Amount paid cannot exceed total booking price.");
            }

            if (!ModelState.IsValid)
            {
                model.CustomerName = booking.CustomerName;
                model.ServiceName = booking.Service?.Name ?? "N/A";
                model.TotalPrice = booking.TotalPrice;
                return View(model);
            }

            booking.AmountPaid = model.AmountPaid;
            booking.PaymentMethod = string.IsNullOrWhiteSpace(model.PaymentMethod) ? null : model.PaymentMethod.Trim();
            booking.PaymentReference = string.IsNullOrWhiteSpace(model.PaymentReference) ? null : model.PaymentReference.Trim();
            booking.PaymentDate = model.PaymentDate;

            if (booking.AmountPaid >= booking.TotalPrice)
            {
                booking.PaymentStatus = PaymentStatus.Paid;
                booking.PaymentDate ??= DateTime.UtcNow;

                if (booking.BookingStatus == BookingStatus.Pending)
                {
                    booking.BookingStatus = BookingStatus.Confirmed;
                }
            }
            else
            {
                // Fully paid automatically sets Paid; otherwise keep admin-selected non-Paid status.
                booking.PaymentStatus = model.PaymentStatus == PaymentStatus.Paid
                    ? PaymentStatus.Pending
                    : model.PaymentStatus;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
            {
                return NotFound();
            }

            booking.BookingStatus = bookingStatus;
            booking.PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? booking.PaymentMethod : paymentMethod.Trim();
            booking.PaymentReference = string.IsNullOrWhiteSpace(paymentReference) ? booking.PaymentReference : paymentReference.Trim();

            if (paymentStatus == PaymentStatus.Paid)
            {
                booking.AmountPaid = booking.AmountPaid <= 0 ? booking.TotalPrice : booking.AmountPaid;
                if (booking.AmountPaid >= booking.TotalPrice)
                {
                    booking.PaymentStatus = PaymentStatus.Paid;
                    booking.PaymentDate ??= DateTime.UtcNow;
                }
                else
                {
                    booking.PaymentStatus = PaymentStatus.Pending;
                }
            }
            else
            {
                booking.PaymentStatus = paymentStatus;

                if (paymentStatus != PaymentStatus.Paid)
                {
                    booking.PaymentDate = paymentStatus == PaymentStatus.Pending ? booking.PaymentDate : null;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
