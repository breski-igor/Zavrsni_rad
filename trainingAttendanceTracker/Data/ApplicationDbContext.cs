using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using trainingAttendanceTracker.Models;

namespace trainingAttendanceTracker.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<EvidencijaDolazaka> Evidencija { get; set; }
        public DbSet<MembershipFee> MembershipFees { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<MembershipPrice> MembershipPrices { get; set; }
    }
}
