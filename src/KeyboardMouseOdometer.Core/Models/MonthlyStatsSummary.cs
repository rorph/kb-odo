using System;

namespace KeyboardMouseOdometer.Core.Models
{
    /// <summary>
    /// Summary of monthly statistics for display in data grid
    /// </summary>
    public class MonthlyStatsSummary
    {
        public DateTime MonthStart { get; set; }
        public string MonthDisplay => $"{MonthStart:MMMM yyyy}";
        public long KeyCount { get; set; }
        public double MouseDistance { get; set; }
        public string MouseDistanceDisplay { get; set; } = "0 m";
        public double ScrollDistance { get; set; }
        public string ScrollDistanceDisplay { get; set; } = "0 m";
        public long TotalClicks { get; set; }
        public double TotalActivity => KeyCount + (MouseDistance / 100) + (ScrollDistance / 100); // Normalized activity score
        
        // For bar chart visualization
        public double KeyCountNormalized { get; set; } // 0-1 value for bar height
        public double MouseDistanceNormalized { get; set; }
        public double ScrollDistanceNormalized { get; set; }
    }
}