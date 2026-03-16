using System.ComponentModel.DataAnnotations;
using cateredByLetsuwi.Models.Enums;

namespace cateredByLetsuwi.Models.ViewModels
{
    public class RecordPaymentViewModel
    {
        public int BookingId { get; set; }

        [Display(Name = "Customer")]
        public string CustomerName { get; set; } = string.Empty;

        [Display(Name = "Service")]
        public string ServiceName { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        public decimal TotalPrice { get; set; }

        [Range(0, 1000000000)]
        [Display(Name = "Amount Paid")]
        [DataType(DataType.Currency)]
        public decimal AmountPaid { get; set; }

        [Display(Name = "Payment Status")]
        public PaymentStatus PaymentStatus { get; set; }

        [Display(Name = "Payment Method")]
        [StringLength(100)]
        public string? PaymentMethod { get; set; }

        [Display(Name = "Reference")]
        [StringLength(150)]
        public string? PaymentReference { get; set; }

        [Display(Name = "Payment Date")]
        [DataType(DataType.DateTime)]
        public DateTime? PaymentDate { get; set; }
    }
}
