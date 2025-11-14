using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trainingAttendanceTracker.Data;
using trainingAttendanceTracker.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace trainingAttendanceTracker.Controllers
{
    public class MembershipController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MembershipController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Management(int? year)
        {
            var currentYear = year ?? DateTime.Now.Year;
            var members = await _context.Users
                .OrderBy(u => u.Prezime)
                .ThenBy(u => u.Ime)
                .ToListAsync();

            var viewModel = new MembershipFeeViewModel
            {
                Year = currentYear,
                Members = new List<MemberFeeStatus>()
            };

            foreach (var member in members)
            {
                var memberStatus = new MemberFeeStatus
                {
                    UserId = member.Id,
                    FullName = $"{member.Ime} {member.Prezime}",
                    Email = member.Email,
                    MonthlyPayments = new bool[12]
                };

                var existingPayments = await _context.MembershipFees
                    .Where(mf => mf.UserId == member.Id && mf.Year == currentYear)
                    .ToListAsync();

                for (int month = 1; month <= 12; month++)
                {
                    var payment = existingPayments.FirstOrDefault(mf => mf.Month == month);
                    memberStatus.MonthlyPayments[month - 1] = payment?.IsPaid ?? false;
                }

                viewModel.Members.Add(memberStatus);
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePayment([FromBody] UpdatePaymentModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.UserId))
                {
                    return Json(new { success = false, error = "UserId is required" });
                }

                if (model.Year < 2020 || model.Year > 2030)
                {
                    return Json(new { success = false, error = "Invalid year" });
                }

                if (model.Month < 1 || model.Month > 12)
                {
                    return Json(new { success = false, error = "Invalid month" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == model.UserId);
                if (user == null)
                {
                    return Json(new { success = false, error = $"User with ID {model.UserId} not found" });
                }

                var existingFee = await _context.MembershipFees
                    .FirstOrDefaultAsync(mf => mf.UserId == model.UserId && mf.Year == model.Year && mf.Month == model.Month);

                if (existingFee != null)
                {
                    existingFee.IsPaid = model.IsPaid;
                    existingFee.PaymentDate = model.IsPaid ? DateTime.Now : null;
                }
                else
                {
                    var newFee = new MembershipFee
                    {
                        UserId = model.UserId,
                        Year = model.Year,
                        Month = model.Month,
                        IsPaid = model.IsPaid,
                        PaymentDate = model.IsPaid ? DateTime.Now : null,
                        CreatedAt = DateTime.Now
                    };
                    _context.MembershipFees.Add(newFee);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Payment updated successfully" });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                return Json(new { success = false, error = $"Database error: {innerMessage}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"General error: {ex.Message}" });
            }
        }

        public async Task<IActionResult> Balance(int? year)
        {
            var currentYear = year ?? DateTime.Now.Year;
            var monthlyBalances = new List<MonthlyBalance>();

            for (int month = 1; month <= 12; month++)
            {
                var monthDate = new DateTime(currentYear, month, 1);
                var price = GetMembershipPriceForDate(monthDate);
                var paidCount = await _context.MembershipFees
                    .CountAsync(mf => mf.Year == currentYear && mf.Month == month && mf.IsPaid);

                var monthlyBalance = new MonthlyBalance
                {
                    MonthNumber = month,
                    MonthName = monthDate.ToString("MMMM"),
                    PaidCount = paidCount,
                    Amount = paidCount * price,
                    PricePerMember = price
                };

                monthlyBalances.Add(monthlyBalance);
            }

            var viewModel = new BalanceViewModel
            {
                Year = currentYear,
                MonthlyBalances = monthlyBalances,
                TotalBalance = monthlyBalances.Sum(mb => mb.Amount)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Payments(
            int? startDay = null, int? startMonth = null, int? startYear = null,
            int? endDay = null, int? endMonth = null, int? endYear = null)
        {
            DateTime startDate, endDate;

            if (startDay.HasValue && startMonth.HasValue && startYear.HasValue)
            {
                startDate = new DateTime(startYear.Value, startMonth.Value, startDay.Value);
            }
            else
            {
                startDate = new DateTime(DateTime.Now.Year, 1, 1);
            }

            if (endDay.HasValue && endMonth.HasValue && endYear.HasValue)
            {
                endDate = new DateTime(endYear.Value, endMonth.Value, endDay.Value);
            }
            else
            {
                endDate = DateTime.Now;
            }

            var payments = await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            decimal membershipIncome = 0;

            var currentDate = startDate;
            while (currentDate <= endDate)
            {
                var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                if (monthStart <= endDate)
                {
                    var price = GetMembershipPriceForDate(monthStart);
                    var paidCount = await _context.MembershipFees
                        .CountAsync(mf => mf.Year == currentDate.Year &&
                                         mf.Month == currentDate.Month &&
                                         mf.IsPaid);

                    membershipIncome += paidCount * price;
                }
                currentDate = currentDate.AddMonths(1);
            }

            var totalExpenses = Math.Abs(payments.Where(p => p.PaymentType == PaymentType.Expense).Sum(p => p.Amount));
            var totalOtherIncome = payments.Where(p => p.PaymentType == PaymentType.Income).Sum(p => p.Amount);
            var netBalance = membershipIncome + totalOtherIncome - totalExpenses;

            var viewModel = new PaymentViewModel
            {
                Payments = payments,
                TotalExpenses = totalExpenses,
                NetBalance = netBalance,
                MembershipIncome = membershipIncome
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, error = string.Join(", ", errors) });
                }

                var payment = new Payment
                {
                    Description = model.Description,
                    Amount = model.PaymentType == PaymentType.Income ? Math.Abs(model.Amount) : -Math.Abs(model.Amount),
                    PaymentType = model.PaymentType,
                    PaymentDate = model.PaymentDate,
                    Category = model.Category,
                    Notes = model.Notes,
                    CreatedAt = DateTime.Now
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Payment created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"Error creating payment: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePayment(int id)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);
                if (payment == null)
                {
                    return Json(new { success = false, error = "Payment not found" });
                }

                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Payment deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"Error deleting payment: {ex.Message}" });
            }
        }

        public async Task<IActionResult> FinancialSummaryRange(
    int? startDay, int? startMonth, int? startYear,
    int? endDay, int? endMonth, int? endYear)
        {
            DateTime startDate, endDate;

            if (startDay.HasValue && startMonth.HasValue && startYear.HasValue)
            {
                startDate = new DateTime(startYear.Value, startMonth.Value, startDay.Value);
            }
            else
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }

            if (endDay.HasValue && endMonth.HasValue && endYear.HasValue)
            {
                endDate = new DateTime(endYear.Value, endMonth.Value, endDay.Value);
            }
            else
            {
                endDate = DateTime.Now;
            }

            if (endDate < startDate)
            {
                endDate = startDate;
            }

            var monthlySummaries = new List<MonthlySummary>();
            var paymentsInRange = await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var currentDate = startDate;
            while (currentDate <= endDate)
            {
                var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var effectiveStart = monthStart < startDate ? startDate : monthStart;
                var effectiveEnd = monthEnd > endDate ? endDate : monthEnd;

                var price = GetMembershipPriceForDate(monthStart);
                var membershipIncome = await _context.MembershipFees
                    .CountAsync(mf => mf.Year == currentDate.Year &&
                                     mf.Month == currentDate.Month &&
                                     mf.IsPaid) * price;

                var expenses = paymentsInRange
                    .Where(p => p.PaymentDate.Year == currentDate.Year &&
                               p.PaymentDate.Month == currentDate.Month &&
                               p.PaymentType == PaymentType.Expense)
                    .Sum(p => Math.Abs(p.Amount));

                var otherIncome = paymentsInRange
                    .Where(p => p.PaymentDate.Year == currentDate.Year &&
                               p.PaymentDate.Month == currentDate.Month &&
                               p.PaymentType == PaymentType.Income)
                    .Sum(p => p.Amount);

                var monthlySummary = new MonthlySummary
                {
                    MonthNumber = currentDate.Month,
                    MonthName = currentDate.ToString("MMMM yyyy"),
                    MembershipIncome = membershipIncome,
                    OtherIncome = otherIncome,
                    Expenses = expenses,
                    Net = membershipIncome + otherIncome - expenses,
                    PricePerMember = price
                };

                monthlySummaries.Add(monthlySummary);

                currentDate = currentDate.AddMonths(1);
                if (currentDate.Day != 1)
                    currentDate = new DateTime(currentDate.Year, currentDate.Month, 1);
            }

            var totalMembershipIncome = monthlySummaries.Sum(m => m.MembershipIncome);
            var totalOtherIncome = monthlySummaries.Sum(m => m.OtherIncome);
            var totalExpenses = monthlySummaries.Sum(m => m.Expenses);
            var netBalance = totalMembershipIncome + totalOtherIncome - totalExpenses;

            var viewModel = new FinancialSummaryRangeViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalMembershipIncome = totalMembershipIncome,
                TotalOtherIncome = totalOtherIncome,
                TotalExpenses = totalExpenses,
                NetBalance = netBalance,
                MonthlySummaries = monthlySummaries,
                Payments = paymentsInRange
            };

            return View(viewModel);
        }

        public async Task<IActionResult> PriceManagement()
        {
            var prices = await _context.MembershipPrices
                .OrderByDescending(p => p.EffectiveFrom)
                .ToListAsync();

            return View(prices);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePrice([FromBody] CreatePriceModel model)
        {
            try
            {
                var previousPrices = await _context.MembershipPrices
                    .Where(p => p.IsActive)
                    .ToListAsync();

                foreach (var price in previousPrices)
                {
                    price.IsActive = false;
                }

                var newPrice = new MembershipPrice
                {
                    Price = model.Price,
                    EffectiveFrom = model.EffectiveFrom,
                    Description = model.Description,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.MembershipPrices.Add(newPrice);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Price created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"Error creating price: {ex.Message}" });
            }
        }

        private decimal GetMembershipPriceForDate(DateTime date)
        {
            var price = _context.MembershipPrices
                .Where(p => p.EffectiveFrom <= date)
                .OrderByDescending(p => p.EffectiveFrom)
                .FirstOrDefault();

            return price?.Price ?? 40.00m;
        }
    }

    public class UpdatePaymentModel
    {
        public string UserId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public bool IsPaid { get; set; }
    }

    public class CreatePaymentModel
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public PaymentType PaymentType { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Category { get; set; }
        public string Notes { get; set; }
    }

    public class DateRangeModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public DateRangeModel()
        {
            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDate = DateTime.Now;
        }
    }

    public class CreatePriceModel
    {
        public decimal Price { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public string Description { get; set; }
    }
}