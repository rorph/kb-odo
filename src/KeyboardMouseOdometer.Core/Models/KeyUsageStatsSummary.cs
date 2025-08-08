namespace KeyboardMouseOdometer.Core.Models
{
    /// <summary>
    /// Summary of key usage statistics for display in top keys table
    /// </summary>
    public class KeyUsageStatsSummary
    {
        public string Key { get; set; } = "";
        public long PressCount { get; set; }
        public double Percentage { get; set; }
        public string FormattedPercentage => $"{Percentage:F1}%";
    }
}