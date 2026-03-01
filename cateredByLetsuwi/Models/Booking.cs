using cateredByLetsuwi.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace cateredByLetsuwi.Models
{
    public class Booking
    {
        public int Id { get; set; }

        // =========================
        // CUSTOMER INFO
        // =========================

        [Required]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; }

        [Required]
        [Range(1, 10000)]
        [Display(Name = "Number of Guests")]
        public int NumberOfGuests { get; set; }

        // =========================
        // SERVICE RELATIONSHIP
        // =========================

        [Required]
        [Display(Name = "Service")]
        public int ServiceId { get; set; }

        public Service? Service { get; set; }

        // =========================
        // BUSINESS FINANCIAL LOGIC
        // =========================

        [Required]
        [DataType(DataType.Currency)]
        public decimal TotalPrice { get; set; }

        [DataType(DataType.Currency)]
        public decimal AmountPaid { get; set; } = 0;

        [DataType(DataType.DateTime)]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.DateTime)]
        public DateTime? PaymentDate { get; set; }

        // =========================
        // STATUS MANAGEMENT
        // =========================

        public BookingStatus BookingStatus { get; set; } = BookingStatus.Pending;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        // =========================
        // OPTIONAL FUTURE PAYMENT INFO
        // (For Stripe / PayFast later)
        // =========================

        public string? PaymentReference { get; set; }

        public string? PaymentMethod { get; set; }
    }
}
