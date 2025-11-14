using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trainingAttendanceTracker.Data;
using trainingAttendanceTracker.Models;
using Microsoft.AspNetCore.Identity;

namespace trainingAttendanceTracker.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AttendanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Scan() => View();

        public IActionResult Calendar(int? year, int? month)
        {
            var currentDate = DateTime.Now;
            var viewYear = year ?? currentDate.Year;
            var viewMonth = month ?? currentDate.Month;

            var attendanceCounts = _context.Evidencija
                .Where(e => e.DatumDolaska.Year == viewYear && e.DatumDolaska.Month == viewMonth)
                .GroupBy(e => e.DatumDolaska.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Date, x => x.Count);

            var firstDayOfMonth = new DateTime(viewYear, viewMonth, 1);
            var daysInMonth = DateTime.DaysInMonth(viewYear, viewMonth);

            var model = new CalendarViewModel
            {
                Year = viewYear,
                Month = viewMonth,
                MonthName = new DateTime(viewYear, viewMonth, 1).ToString("MMMM"),
                AttendanceCounts = attendanceCounts,
                FirstDayOfMonth = firstDayOfMonth,
                DaysInMonth = daysInMonth
            };

            return View(model);
        }

        public IActionResult DayDetails(int year, int month, int day)
        {
            var date = new DateTime(year, month, day);
            var attendances = _context.Evidencija
                .Include(e => e.User)
                .Where(e => e.DatumDolaska.Date == date.Date)
                .OrderBy(e => e.DatumDolaska)
                .ToList();

            ViewBag.SelectedDate = date.ToString("dd.MM.yyyy");
            return View(attendances);
        }

        [HttpPost]
        public IActionResult MarkAttendance([FromBody] QRScanModel model)
        {
            try
            {
                var parts = model.QrData.Split('|');
                if (parts.Length < 3)
                {
                    return Json(new { success = false, message = "Nevažeći QR kod" });
                }

                var userId = parts[2];
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);

                if (user == null)
                    return Json(new { success = false, message = "Korisnik nije pronađen!" });

                bool postoji = _context.Evidencija
                    .Any(e => e.UserId == user.Id && e.DatumDolaska.Date == DateTime.Today);

                if (!postoji)
                {
                    _context.Evidencija.Add(new EvidencijaDolazaka
                    {
                        UserId = user.Id,
                        DatumDolaska = DateTime.Now
                    });
                    _context.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        clan = $"{user.Ime} {user.Prezime}",
                        message = "Dolazak uspješno evidentiran"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Već ste evidentirani danas"
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Greška: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public IActionResult MarkAttendanceWithDate([FromBody] ManualAttendanceWithDateModel model)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Id == model.UserId);
                if (user == null)
                    return Json(new { success = false, message = "Korisnik nije pronađen!" });

                var localTime = model.AttendanceDate;
                if (localTime.Kind == DateTimeKind.Utc)
                {
                    localTime = localTime.ToLocalTime();
                }

                bool postoji = _context.Evidencija
                    .Any(e => e.UserId == user.Id && e.DatumDolaska.Date == localTime.Date);

                if (!postoji)
                {
                    _context.Evidencija.Add(new EvidencijaDolazaka
                    {
                        UserId = user.Id,
                        DatumDolaska = localTime
                    });
                    _context.SaveChanges();

                    string dan = localTime.Day.ToString("00");
                    string mjesec = localTime.Month.ToString("00");
                    string godina = localTime.Year.ToString();
                    string sati = localTime.Hour.ToString("00");
                    string minute = localTime.Minute.ToString("00");

                    return Json(new
                    {
                        success = true,
                        message = $"Dolazak evidentiran za: {user.Ime} {user.Prezime} na datum {dan}/{mjesec}/{godina} {sati}:{minute}"
                    });
                }
                else
                {
                    string dan = localTime.Day.ToString("00");
                    string mjesec = localTime.Month.ToString("00");
                    string godina = localTime.Year.ToString();

                    return Json(new
                    {
                        success = false,
                        message = $"{user.Ime} {user.Prezime} je već evidentiran na datum {dan}/{mjesec}/{godina}"
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Greška: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public IActionResult SearchUsers(string term)
        {
            var users = _context.Users
                .Where(u => u.Ime.Contains(term) || u.Prezime.Contains(term) || u.Email.Contains(term))
                .Select(u => new {
                    id = u.Id,
                    label = $"{u.Ime} {u.Prezime} ({u.Email})",
                    value = $"{u.Ime} {u.Prezime}"
                })
                .Take(10)
                .ToList();

            return Json(users);
        }

        [HttpPost]
        public IActionResult MarkManualAttendance([FromBody] ManualAttendanceModel model)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == model.UserId);
            if (user == null)
                return Json(new { success = false, message = "Korisnik nije pronađen!" });

            bool postoji = _context.Evidencija
                .Any(e => e.UserId == user.Id && e.DatumDolaska.Date == DateTime.Today);

            if (!postoji)
            {
                _context.Evidencija.Add(new EvidencijaDolazaka
                {
                    UserId = user.Id,
                    DatumDolaska = DateTime.Now
                });
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Dolazak evidentiran za: {user.Ime} {user.Prezime}"
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = $"{user.Ime} {user.Prezime} je već evidentiran danas"
                });
            }
        }

        public IActionResult List()
        {
            var evidencija = _context.Evidencija
                .Include(e => e.User)
                .OrderByDescending(e => e.DatumDolaska)
                .ToList();

            return View(evidencija);
        }

        public async Task<IActionResult> TrainingLog(
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? startDay = null, int? startMonth = null, int? startYear = null,
            int? endDay = null, int? endMonth = null, int? endYear = null)
        {
            if (startDay.HasValue && startMonth.HasValue && startYear.HasValue)
            {
                startDate = new DateTime(startYear.Value, startMonth.Value, startDay.Value);
            }

            if (endDay.HasValue && endMonth.HasValue && endYear.HasValue)
            {
                endDate = new DateTime(endYear.Value, endMonth.Value, endDay.Value);
            }

            startDate ??= new DateTime(DateTime.Now.Year, 1, 1);
            endDate ??= DateTime.Now;

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var attendanceRecords = await _context.Evidencija
                .Where(e => e.UserId == userId &&
                           e.DatumDolaska >= startDate &&
                           e.DatumDolaska <= endDate.Value.AddDays(1).AddSeconds(-1))
                .OrderBy(e => e.DatumDolaska)
                .ToListAsync();

            var monthlySummary = attendanceRecords
                .GroupBy(e => new { e.DatumDolaska.Year, e.DatumDolaska.Month })
                .Select(g => new TrainingMonthlySummary
                {
                    MonthYear = $"{g.Key.Month:00}/{g.Key.Year}",
                    TrainingCount = g.Count()
                })
                .OrderBy(m => m.MonthYear)
                .ToList();

            var detailedRecords = attendanceRecords.Select(e => new AttendanceRecord
            {
                AttendanceDate = e.DatumDolaska,
                DayOfWeek = e.DatumDolaska.ToString("dddd")
            }).ToList();

            var model = new TrainingLogViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                AttendanceRecords = detailedRecords,
                MonthlySummary = monthlySummary,
                TotalTrainings = attendanceRecords.Count
            };

            return View(model);
        }
    }

    public class QRScanModel
    {
        public string QrData { get; set; }
    }

    public class ManualAttendanceModel
    {
        public string UserId { get; set; }
    }

    public class ManualAttendanceWithDateModel
    {
        public string UserId { get; set; }
        public DateTime AttendanceDate { get; set; }
    }
}