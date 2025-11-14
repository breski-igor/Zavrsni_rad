using Microsoft.AspNetCore.Identity;

namespace trainingAttendanceTracker.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public DateTime DatumRodjenja { get; set; }
        public string Rang { get; set; }
        public DateTime DatumUclanjenja { get; set; }
        public string QrCodePath { get; set; }


    }
}