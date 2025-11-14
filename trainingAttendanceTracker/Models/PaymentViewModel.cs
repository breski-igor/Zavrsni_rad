using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace trainingAttendanceTracker.Models
{
    public class PaymentViewModel
    {
        public List<Payment> Payments { get; set; } = new List<Payment>();
        public decimal TotalExpenses { get; set; }
        public decimal NetBalance { get; set; }
        public decimal MembershipIncome { get; set; }
    }

    public class MonthlySummary
    {
        public int MonthNumber { get; set; }
        public string MonthName { get; set; }
        public decimal MembershipIncome { get; set; }
        public decimal OtherIncome { get; set; }
        public decimal Expenses { get; set; }
        public decimal Net { get; set; }
        public decimal PricePerMember { get; set; }
    }
    public class FinancialSummaryRangeViewModel
    {
        public DateTime StartDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime EndDate { get; set; } = DateTime.Now;
        public decimal TotalMembershipIncome { get; set; }
        public decimal TotalOtherIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetBalance { get; set; }
        public List<MonthlySummary> MonthlySummaries { get; set; } = new List<MonthlySummary>();
        public List<Payment> Payments { get; set; } = new List<Payment>();
    }

}