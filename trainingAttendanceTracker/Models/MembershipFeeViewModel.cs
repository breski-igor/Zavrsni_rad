using System.Collections.Generic;

namespace trainingAttendanceTracker.Models
{
    public class MembershipFeeViewModel
    {
        public List<MemberFeeStatus> Members { get; set; } = new List<MemberFeeStatus>();
        public int Year { get; set; } = DateTime.Now.Year;
    }

    public class MemberFeeStatus
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool[] MonthlyPayments { get; set; } = new bool[12];
    }

    public class BalanceViewModel
    {
        public int Year { get; set; }
        public List<MonthlyBalance> MonthlyBalances { get; set; } = new List<MonthlyBalance>();
        public decimal TotalBalance { get; set; }
    }

    public class MonthlyBalance
    {
        public string MonthName { get; set; }
        public int MonthNumber { get; set; }
        public int PaidCount { get; set; }
        public decimal Amount { get; set; }
        public decimal PricePerMember { get; set; }
    }
   
}