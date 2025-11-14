using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace trainingAttendanceTracker.Controllers
{
    public class GalleryController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public GalleryController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            var images = GetGalleryImages();
            return View(images);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Upload(List<IFormFile> galleryImages)
        {
            if (galleryImages == null || !galleryImages.Any())
            {
                ViewBag.Error = "Molimo odaberite barem jednu sliku za upload.";
                return View();
            }

            var uploadedCount = 0;
            var errors = new List<string>();

            foreach (var image in galleryImages)
            {
                if (image.Length == 0) continue;

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(image.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    errors.Add($"{image.FileName}: Samo JPG, PNG i GIF formati su dozvoljeni.");
                    continue;
                }

                if (image.Length > 5 * 1024 * 1024)
                {
                    errors.Add($"{image.FileName}: Veličina fajla mora biti manja od 5MB.");
                    continue;
                }

                try
                {
                    var uploadsPath = Path.Combine(_environment.WebRootPath, "images", "gallery");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    var fileName = Guid.NewGuid().ToString() + fileExtension;
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    uploadedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{image.FileName}: Greška pri uploadu - {ex.Message}");
                }
            }

            if (uploadedCount > 0)
            {
                ViewBag.Success = $"Uspješno uploadano {uploadedCount} slika.";
            }

            if (errors.Any())
            {
                ViewBag.Error = string.Join(" ", errors);
            }

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string imageName)
        {
            try
            {
                var imagePath = Path.Combine(_environment.WebRootPath, "images", "gallery", imageName);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                    TempData["Success"] = "Slika je uspješno obrisana.";
                }
                else
                {
                    TempData["Error"] = "Slika nije pronađena.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Greška pri brisanju: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        private List<string> GetGalleryImages()
        {
            var galleryPath = Path.Combine(_environment.WebRootPath, "images", "gallery");
            if (!Directory.Exists(galleryPath))
            {
                return new List<string>();
            }

            var images = Directory.GetFiles(galleryPath)
                .Where(f => f.ToLower().EndsWith(".jpg") ||
                           f.ToLower().EndsWith(".jpeg") ||
                           f.ToLower().EndsWith(".png") ||
                           f.ToLower().EndsWith(".gif"))
                .Select(f => Path.GetFileName(f))
                .ToList();

            return images;
        }
    }
}