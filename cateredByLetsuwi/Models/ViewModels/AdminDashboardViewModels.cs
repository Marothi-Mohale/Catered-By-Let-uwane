namespace cateredByLetsuwi.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalBookings { get; set; }
        public int UpcomingBookings { get; set; }
        public int UnpaidBookings { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal AverageBookingValue { get; set; }

        public string? MostPopularService { get; set; }
    }
}