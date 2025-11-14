using System;
using System.Collections.Generic;

namespace trainingAttendanceTracker.Models
{
    public class CalendarViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public Dictionary<DateTime, int> AttendanceCounts { get; set; }
        public DateTime FirstDayOfMonth { get; set; }
        public int DaysInMonth { get; set; }
    }
}