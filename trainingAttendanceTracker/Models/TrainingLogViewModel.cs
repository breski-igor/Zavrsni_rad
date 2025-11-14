using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace trainingAttendanceTracker.Models
{
    public class TrainingLogViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime StartDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime EndDate { get; set; } = DateTime.Now;
        public int TotalTrainings { get; set; }
        public List<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public List<TrainingMonthlySummary> MonthlySummary { get; set; } = new List<TrainingMonthlySummary>();
    }

    public class AttendanceRecord
    {
        public DateTime AttendanceDate { get; set; }
        public string DayOfWeek { get; set; }
        public TimeSpan? TimeOfDay { get; set; }
    }

    public class TrainingMonthlySummary
    {
        public string MonthYear { get; set; }
        public int TrainingCount { get; set; }
    }

    public class TrainingLogFilterModel
    {
        public DateTime StartDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime EndDate { get; set; } = DateTime.Now;
    }
}