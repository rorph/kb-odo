namespace KeyboardMouseOdometer.Core.Configuration;

public class OdometerOptions
{
    public const string SectionName = "Odometer";

    public bool AutoStart { get; set; } = true;
    public int SaveInterval { get; set; } = 30; // seconds
    public bool StartMinimized { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public string Theme { get; set; } = "Light";
    public bool AutoSave { get; set; } = true;
    public int StatisticsUpdateInterval { get; set; } = 1000; // milliseconds
    public bool TrackDetailedStatistics { get; set; } = true;
    public int MaxSessionHistoryDays { get; set; } = 365;
    public string DataDirectory { get; set; } = string.Empty;
    public bool EnableHotkeys { get; set; } = true;
    public string StartStopHotkey { get; set; } = "Ctrl+Shift+F12";
    public string ResetHotkey { get; set; } = "Ctrl+Shift+F11";
}

public class LoggingOptions
{
    public const string SectionName = "Logging";

    public string LogLevel { get; set; } = "Information";
    public bool EnableFileLogging { get; set; } = true;
    public string LogDirectory { get; set; } = "Logs";
    public int MaxLogFileSizeMB { get; set; } = 10;
    public int MaxLogFiles { get; set; } = 5;
}