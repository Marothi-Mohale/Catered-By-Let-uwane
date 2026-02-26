using Microsoft.EntityFrameworkCore;
using cateredByLetsuwi.Models;

namespace cateredByLetsuwi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Service> Services { get; set; }

        
        public DbSet<Booking> Bookings { get; set; }
    }
}