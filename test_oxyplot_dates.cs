using System;
using OxyPlot.Axes;

class TestOxyPlotDates
{
    static void Main()
    {
        // Test dates in August 2025
        var dates = new[] {
            "2025-08-01",
            "2025-08-05", 
            "2025-08-09",
            "2025-08-15",
            "2025-08-31"
        };
        
        foreach (var dateStr in dates)
        {
            var date = DateTime.ParseExact(dateStr, "yyyy-MM-dd", null);
            var oxyDouble = DateTimeAxis.ToDouble(date);
            var backToDate = DateTimeAxis.ToDateTime(oxyDouble);
            
            Console.WriteLine($"Original: {dateStr}");
            Console.WriteLine($"  Parsed DateTime: {date:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  OxyPlot double: {oxyDouble}");
            Console.WriteLine($"  Back to DateTime: {backToDate:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  Display as MM/dd: {backToDate:MM/dd}");
            Console.WriteLine();
        }
    }
}