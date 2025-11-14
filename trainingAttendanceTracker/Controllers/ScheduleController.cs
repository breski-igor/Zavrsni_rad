using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace trainingAttendanceTracker.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ScheduleController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Upload(IFormFile scheduleImage)
        {
            if (scheduleImage == null || scheduleImage.Length == 0)
            {
                ViewBag.Error = "Molimo odaberite sliku za upload.";
                return View();
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(scheduleImage.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ViewBag.Error = "Samo JPG, PNG i GIF formati su dozvoljeni.";
                return View();
            }

            try
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "images", "schedule");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var oldFiles = Directory.GetFiles(uploadsPath, "training-schedule.*");
                foreach (var oldFile in oldFiles)
                {
                    System.IO.File.Delete(oldFile);
                }

                var fileName = "training-schedule" + fileExtension;
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await scheduleImage.CopyToAsync(stream);
                }

                ViewBag.Success = "Slika rasporeda je uspješno uploadana!";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Greška pri uploadu: {ex.Message}";
                return View();
            }
        }
    }
}