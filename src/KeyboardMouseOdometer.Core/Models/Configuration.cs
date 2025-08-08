using System.Text.Json;
using System.IO;

namespace KeyboardMouseOdometer.Core.Models;

/// <summary>
/// Application configuration matching PROJECT_SPEC requirements
/// </summary>
public class Configuration
{
    // Tracking Settings
    public bool TrackKeystrokes { get; set; } = true;
    public bool TrackMouseMovement { get; set; } = true;
    public bool TrackMouseClicks { get; set; } = true;
    public bool TrackScrollWheel { get; set; } = true;

    // System Integration
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowToolbar { get; set; } = true;
    public bool ToolbarAlwaysOnTop { get; set; } = true;

    // Database Settings
    public int DatabaseRetentionDays { get; set; } = 90; // Keep last 90 days as per spec
    public bool EnableRawEventLogging { get; set; } = false; // Optional detailed logging
    public string DatabasePath { get; set; } = "odometer.db";

    // UI Settings
    public bool ShowLastKeyPressed { get; set; } = true;
    public bool ShowDailyKeyCount { get; set; } = true;
    public bool ShowDailyMouseDistance { get; set; } = true;
    public string DistanceUnit { get; set; } = "metric"; // "metric", "imperial", or "pixels"
    
    // Toolbar Settings
    public double ToolbarWidth { get; set; } = 720;
    public double ToolbarHeight { get; set; } = 40;
    public double ToolbarLeft { get; set; } = -1; // -1 means center horizontally
    public double ToolbarTop { get; set; } = -1;  // -1 means position above taskbar
    public string ToolbarMonitorDeviceName { get; set; } = ""; // Monitor device name to restore position on correct screen
    
    // Main Window Settings
    public double MainWindowWidth { get; set; } = 800;
    public double MainWindowHeight { get; set; } = 600;
    
    // Heatmap Settings
    public string HeatmapColorScheme { get; set; } = "Classic"; // "Classic" or "FLIR"

    // Performance Settings
    public int StatisticsUpdateIntervalMs { get; set; } = 1000; // Update UI every second
    public int DatabaseSaveIntervalMs { get; set; } = 30000; // Save to DB every 30 seconds
    public int DataFlushIntervalSeconds { get; set; } = 30; // Data flush interval in seconds (for compatibility)
    public int MouseMovementThrottleMs { get; set; } = 100; // Process mouse movement at most every 100ms
    public int ChartUpdateIntervalSeconds { get; set; } = 30; // Update charts every 30 seconds
    public int UIUpdateIntervalMs { get; set; } = 500; // Throttle UI updates to 2 times per second

    // Privacy Settings
    public bool EnableDatabaseEncryption { get; set; } = false; // Optional as per spec
    public bool LogDetailedKeystrokes { get; set; } = false; // Don't log actual key content for privacy

    /// <summary>
    /// Creates default configuration
    /// </summary>
    public static Configuration CreateDefault()
    {
        return new Configuration();
    }

    /// <summary>
    /// Validates the configuration settings
    /// </summary>
    public bool IsValid()
    {
        return DatabaseRetentionDays >= 0  // 0 = never delete, > 0 = delete after N days
            && DatabaseRetentionDays <= 3650 
            && StatisticsUpdateIntervalMs >= 100 
            && DatabaseSaveIntervalMs >= 1000
            && MouseMovementThrottleMs >= 50
            && MouseMovementThrottleMs <= 1000
            && ChartUpdateIntervalSeconds >= 5
            && ChartUpdateIntervalSeconds <= 300
            && UIUpdateIntervalMs >= 100
            && UIUpdateIntervalMs <= 5000
            && !string.IsNullOrWhiteSpace(DatabasePath)
            && (DistanceUnit == "metric" || DistanceUnit == "imperial" || DistanceUnit == "pixels")
            && (HeatmapColorScheme == "Classic" || HeatmapColorScheme == "FLIR");
    }

    /// <summary>
    /// Validates the configuration and returns detailed validation messages
    /// </summary>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (DatabaseRetentionDays < 0)
            errors.Add("Database retention days must be 0 or greater (0 = never delete)");
        if (DatabaseRetentionDays > 3650)
            errors.Add("Database retention days must be 3650 or less");
        if (StatisticsUpdateIntervalMs < 100)
            errors.Add("Statistics update interval must be at least 100ms");
        if (DatabaseSaveIntervalMs < 1000)
            errors.Add("Database save interval must be at least 1000ms");
        if (MouseMovementThrottleMs < 50)
            errors.Add("Mouse movement throttle must be at least 50ms");
        if (MouseMovementThrottleMs > 1000)
            errors.Add("Mouse movement throttle must be 1000ms or less");
        if (ChartUpdateIntervalSeconds < 5)
            errors.Add("Chart update interval must be at least 5 seconds");
        if (ChartUpdateIntervalSeconds > 300)
            errors.Add("Chart update interval must be 300 seconds or less");
        if (UIUpdateIntervalMs < 100)
            errors.Add("UI update interval must be at least 100ms");
        if (UIUpdateIntervalMs > 5000)
            errors.Add("UI update interval must be 5000ms or less");
        if (string.IsNullOrWhiteSpace(DatabasePath))
            errors.Add("Database path cannot be empty");
        if (DistanceUnit != "metric" && DistanceUnit != "imperial" && DistanceUnit != "pixels")
            errors.Add($"Distance unit must be 'metric', 'imperial', or 'pixels', but was '{DistanceUnit}'");
        if (HeatmapColorScheme != "Classic" && HeatmapColorScheme != "FLIR")
            errors.Add($"Heatmap color scheme must be 'Classic' or 'FLIR', but was '{HeatmapColorScheme}'");

        return errors;
    }

    /// <summary>
    /// Gets the default configuration file path
    /// </summary>
    public static string GetConfigFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "KeyboardMouseOdometer");
        Directory.CreateDirectory(appFolder);
        return Path.Combine(appFolder, "config.json");
    }

    /// <summary>
    /// Loads configuration from file, or creates default if file doesn't exist
    /// </summary>
    public static Configuration LoadFromFile()
    {
        try
        {
            var configPath = GetConfigFilePath();
            System.Diagnostics.Debug.WriteLine($"Loading configuration from: {configPath}");
            System.Diagnostics.Trace.WriteLine($"[CONFIG] Loading configuration from: {configPath}");
            
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                System.Diagnostics.Debug.WriteLine($"Configuration file size: {json.Length} characters");
                
                var config = JsonSerializer.Deserialize<Configuration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                });
                
                if (config != null && config.IsValid())
                {
                    System.Diagnostics.Debug.WriteLine("Configuration loaded and validated successfully");
                    System.Diagnostics.Trace.WriteLine($"[CONFIG] Configuration loaded successfully. HeatmapColorScheme: {config.HeatmapColorScheme}");
                    return config;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Configuration file exists but is invalid, using defaults");
                    System.Diagnostics.Trace.WriteLine("[CONFIG] Configuration invalid, using defaults");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Configuration file does not exist, creating default");
            }
        }
        catch (Exception ex)
        {
            // Log error if possible, but don't throw - fall back to default
            System.Diagnostics.Debug.WriteLine($"Failed to load configuration: {ex.Message}");
            System.Diagnostics.Trace.WriteLine($"[CONFIG] ERROR loading configuration: {ex}");
        }

        // Return default configuration if loading failed
        var defaultConfig = CreateDefault();
        System.Diagnostics.Debug.WriteLine("Using default configuration");
        System.Diagnostics.Trace.WriteLine($"[CONFIG] Using default configuration. HeatmapColorScheme: {defaultConfig.HeatmapColorScheme}");
        return defaultConfig;
    }

    /// <summary>
    /// Saves configuration to file
    /// </summary>
    public void SaveToFile()
    {
        try
        {
            if (!IsValid())
            {
                throw new InvalidOperationException("Cannot save invalid configuration");
            }

            var configPath = GetConfigFilePath();
            System.Diagnostics.Debug.WriteLine($"Saving configuration to: {configPath}");
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                System.Diagnostics.Debug.WriteLine($"Created directory: {directory}");
            }
            
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(configPath, json);
            System.Diagnostics.Debug.WriteLine($"Configuration saved successfully. File size: {new FileInfo(configPath).Length} bytes");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save configuration: {ex}");
            throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates this configuration with values from another configuration
    /// </summary>
    public void UpdateFrom(Configuration other)
    {
        if (other == null) return;

        TrackKeystrokes = other.TrackKeystrokes;
        TrackMouseMovement = other.TrackMouseMovement;
        TrackMouseClicks = other.TrackMouseClicks;
        TrackScrollWheel = other.TrackScrollWheel;
        StartWithWindows = other.StartWithWindows;
        MinimizeToTray = other.MinimizeToTray;
        ShowToolbar = other.ShowToolbar;
        ToolbarAlwaysOnTop = other.ToolbarAlwaysOnTop;
        DatabaseRetentionDays = other.DatabaseRetentionDays;
        EnableRawEventLogging = other.EnableRawEventLogging;
        DatabasePath = other.DatabasePath;
        ShowLastKeyPressed = other.ShowLastKeyPressed;
        ShowDailyKeyCount = other.ShowDailyKeyCount;
        ShowDailyMouseDistance = other.ShowDailyMouseDistance;
        DistanceUnit = other.DistanceUnit;
        ToolbarWidth = other.ToolbarWidth;
        ToolbarHeight = other.ToolbarHeight;
        ToolbarLeft = other.ToolbarLeft;
        ToolbarTop = other.ToolbarTop;
        ToolbarMonitorDeviceName = other.ToolbarMonitorDeviceName;
        MainWindowWidth = other.MainWindowWidth;
        MainWindowHeight = other.MainWindowHeight;
        StatisticsUpdateIntervalMs = other.StatisticsUpdateIntervalMs;
        DatabaseSaveIntervalMs = other.DatabaseSaveIntervalMs;
        MouseMovementThrottleMs = other.MouseMovementThrottleMs;
        ChartUpdateIntervalSeconds = other.ChartUpdateIntervalSeconds;
        UIUpdateIntervalMs = other.UIUpdateIntervalMs;
        EnableDatabaseEncryption = other.EnableDatabaseEncryption;
        LogDetailedKeystrokes = other.LogDetailedKeystrokes;
        HeatmapColorScheme = other.HeatmapColorScheme;
    }
}