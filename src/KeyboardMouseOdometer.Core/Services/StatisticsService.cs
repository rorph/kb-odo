using KeyboardMouseOdometer.Core.Interfaces;
using KeyboardMouseOdometer.Core.Models;
using Microsoft.Extensions.Logging;

namespace KeyboardMouseOdometer.Core.Services;

public class StatisticsService : IStatisticsService
{
    private readonly ILogger<StatisticsService> _logger;
    private readonly IDataStorageService _dataStorage;
    private OdometerData _currentData;
    private SessionStatistics _currentSession;
    private MousePosition? _lastMousePosition;
    private DateTime _sessionStartTime;

    public OdometerData CurrentData => _currentData;
    public SessionStatistics CurrentSession => _currentSession;

    public event EventHandler<OdometerData>? DataUpdated;
    public event EventHandler<SessionStatistics>? SessionUpdated;

    public StatisticsService(ILogger<StatisticsService> logger, IDataStorageService dataStorage)
    {
        _logger = logger;
        _dataStorage = dataStorage;
        _currentData = new OdometerData { Timestamp = DateTime.Now };
        _currentSession = new SessionStatistics { SessionStart = DateTime.Now };
        _sessionStartTime = DateTime.Now;
    }

    public void ProcessInputEvent(InputEvent inputEvent)
    {
        try
        {
            _currentData.Timestamp = inputEvent.Timestamp;

            switch (inputEvent.EventType)
            {
                case InputEventType.KeyDown:
                    ProcessKeyEvent(inputEvent);
                    break;
                case InputEventType.MouseMove:
                    ProcessMouseMove(inputEvent);
                    break;
                case InputEventType.MouseClick:
                    ProcessMouseClick(inputEvent);
                    break;
                case InputEventType.MouseWheel:
                    ProcessMouseWheel(inputEvent);
                    break;
            }

            UpdateSessionDuration();
            DataUpdated?.Invoke(this, _currentData);
            SessionUpdated?.Invoke(this, _currentSession);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing input event: {EventType}", inputEvent.EventType);
        }
    }

    private void ProcessKeyEvent(InputEvent inputEvent)
    {
        _currentData.Keystrokes++;
        _currentSession.TotalKeystrokes++;

        if (!string.IsNullOrEmpty(inputEvent.KeyCode))
        {
            _currentData.KeyFrequency[inputEvent.KeyCode] = _currentData.KeyFrequency.GetValueOrDefault(inputEvent.KeyCode, 0) + 1;
        }
    }

    private void ProcessMouseMove(InputEvent inputEvent)
    {
        var currentPosition = new MousePosition { X = inputEvent.X, Y = inputEvent.Y, Timestamp = inputEvent.Timestamp };

        if (_lastMousePosition != null)
        {
            var distance = currentPosition.DistanceTo(_lastMousePosition);
            _currentData.MouseDistance += distance;
            _currentSession.TotalMouseDistance += distance;
        }

        _lastMousePosition = currentPosition;
    }

    private void ProcessMouseClick(InputEvent inputEvent)
    {
        _currentData.MouseClicks++;
        _currentSession.TotalMouseClicks++;

        var buttonName = inputEvent.MouseButton?.ToString() ?? "Unknown";
        _currentData.MouseButtonFrequency[buttonName] = _currentData.MouseButtonFrequency.GetValueOrDefault(buttonName, 0) + 1;
        _currentSession.MouseButtonStats[buttonName] = _currentSession.MouseButtonStats.GetValueOrDefault(buttonName, 0) + 1;
    }

    private void ProcessMouseWheel(InputEvent inputEvent)
    {
        _currentData.ScrollWheelTicks++;
        _currentSession.TotalScrollTicks++;
    }

    private void UpdateSessionDuration()
    {
        var duration = DateTime.Now - _sessionStartTime;
        _currentData.SessionDuration = duration;
        _currentSession.Duration = duration;

        // Calculate rates
        var minutes = duration.TotalMinutes;
        if (minutes > 0)
        {
            _currentSession.AverageKeystrokesPerMinute = _currentSession.TotalKeystrokes / minutes;
            _currentSession.AverageMouseClicksPerMinute = _currentSession.TotalMouseClicks / minutes;
        }
    }

    public void StartNewSession()
    {
        _sessionStartTime = DateTime.Now;
        _currentSession = new SessionStatistics
        {
            SessionStart = _sessionStartTime,
            SessionEnd = _sessionStartTime
        };
        
        _logger.LogInformation("New session started at {StartTime}", _sessionStartTime);
    }

    public void EndCurrentSession()
    {
        _currentSession.SessionEnd = DateTime.Now;
        _currentSession.Duration = _currentSession.SessionEnd - _currentSession.SessionStart;

        // Calculate top keys
        _currentSession.TopKeys = _currentData.KeyFrequency
            .OrderByDescending(kv => kv.Value)
            .Take(10)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        _logger.LogInformation("Session ended. Duration: {Duration}, Keystrokes: {Keystrokes}, Mouse clicks: {Clicks}",
            _currentSession.Duration, _currentSession.TotalKeystrokes, _currentSession.TotalMouseClicks);
    }

    public async Task<List<DailyStatistics>> GetDailyStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        return await _dataStorage.LoadDailyStatisticsAsync(startDate, endDate);
    }

    public async Task<SessionStatistics> GetSessionStatisticsAsync(DateTime sessionStart)
    {
        var sessions = await _dataStorage.LoadSessionStatisticsAsync(sessionStart, sessionStart.AddDays(1));
        return sessions.FirstOrDefault(s => s.SessionStart.Date == sessionStart.Date) ?? new SessionStatistics();
    }

    public async Task SaveCurrentDataAsync()
    {
        try
        {
            await _dataStorage.SaveOdometerDataAsync(_currentData);
            await _dataStorage.SaveSessionStatisticsAsync(_currentSession);
            _logger.LogDebug("Current data saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save current data");
        }
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var loadedData = await _dataStorage.LoadOdometerDataAsync();
            if (loadedData != null)
            {
                _currentData = loadedData;
                _logger.LogInformation("Data loaded successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load data");
        }
    }

    public void ResetStatistics()
    {
        _currentData = new OdometerData { Timestamp = DateTime.Now };
        _currentSession = new SessionStatistics { SessionStart = DateTime.Now };
        _lastMousePosition = null;
        _sessionStartTime = DateTime.Now;
        
        DataUpdated?.Invoke(this, _currentData);
        SessionUpdated?.Invoke(this, _currentSession);
        
        _logger.LogInformation("Statistics reset");
    }

    /// <summary>
    /// Calculate heatmap data from key statistics with normalized heat levels
    /// </summary>
    public static List<KeyboardKey> CalculateHeatmapData(Dictionary<string, long> keyStats, List<KeyboardKey> keyboardLayout)
    {
        if (keyStats == null || keyStats.Count == 0 || keyboardLayout == null)
        {
            return keyboardLayout ?? new List<KeyboardKey>();
        }

        // Find max count for normalization (excluding outliers)
        var sortedCounts = keyStats.Values.Where(v => v > 0).OrderBy(v => v).ToList();
        if (sortedCounts.Count == 0)
        {
            return keyboardLayout;
        }

        // Use 95th percentile as max to handle outliers (like Space key)
        var percentileIndex = Math.Min((int)(sortedCounts.Count * 0.95), sortedCounts.Count - 1);
        var maxCount = Math.Max(sortedCounts[percentileIndex], 1);

        // Create a lookup for quick access using KeyName for database matching
        var keyLookup = keyboardLayout.ToDictionary(k => k.KeyName, k => k);

        // Also create alternative lookups for better key matching
        var alternativeKeyLookup = new Dictionary<string, KeyboardKey>();
        foreach (var kbKey in keyboardLayout)
        {
            // Add alternative names for numpad keys
            if (kbKey.KeyName == "Num0") alternativeKeyLookup["NumPad0"] = kbKey;
            if (kbKey.KeyName == "Num1") alternativeKeyLookup["NumPad1"] = kbKey;
            if (kbKey.KeyName == "Num2") alternativeKeyLookup["NumPad2"] = kbKey;
            if (kbKey.KeyName == "Num3") alternativeKeyLookup["NumPad3"] = kbKey;
            if (kbKey.KeyName == "Num4") alternativeKeyLookup["NumPad4"] = kbKey;
            if (kbKey.KeyName == "Num5") alternativeKeyLookup["NumPad5"] = kbKey;
            if (kbKey.KeyName == "Num6") alternativeKeyLookup["NumPad6"] = kbKey;
            if (kbKey.KeyName == "Num7") alternativeKeyLookup["NumPad7"] = kbKey;
            if (kbKey.KeyName == "Num8") alternativeKeyLookup["NumPad8"] = kbKey;
            if (kbKey.KeyName == "Num9") alternativeKeyLookup["NumPad9"] = kbKey;
            if (kbKey.KeyName == "Num+") alternativeKeyLookup["Add"] = kbKey;
            if (kbKey.KeyName == "Num-") alternativeKeyLookup["Subtract"] = kbKey;
            if (kbKey.KeyName == "Num*") alternativeKeyLookup["Multiply"] = kbKey;
            if (kbKey.KeyName == "Num/") alternativeKeyLookup["Divide"] = kbKey;
            if (kbKey.KeyName == "Num.") alternativeKeyLookup["Decimal"] = kbKey;
        }

        // Update key press counts and calculate heat levels
        foreach (var kvp in keyStats)
        {
            KeyboardKey? key = null;
            
            // Debug logging for numpad keys
            if (kvp.Key.StartsWith("Num"))
            {
                Console.WriteLine($"[DEBUG] Processing key stat: {kvp.Key} = {kvp.Value}");
            }
            
            // Try primary lookup first
            if (keyLookup.TryGetValue(kvp.Key, out key))
            {
                key.PressCount = kvp.Value;
                // Use logarithmic scale for better heat distribution
                key.HeatLevel = CalculateLogHeatLevel(kvp.Value, maxCount);
                
                if (kvp.Key.StartsWith("Num"))
                {
                    Console.WriteLine($"[DEBUG] Found key in primary lookup: {kvp.Key} -> {key.DisplayText}");
                }
            }
            // Try alternative lookup if primary fails
            else if (alternativeKeyLookup.TryGetValue(kvp.Key, out key))
            {
                key.PressCount = kvp.Value;
                // Use logarithmic scale for better heat distribution
                key.HeatLevel = CalculateLogHeatLevel(kvp.Value, maxCount);
                
                if (kvp.Key.StartsWith("Num"))
                {
                    Console.WriteLine($"[DEBUG] Found key in alternative lookup: {kvp.Key} -> {key.DisplayText}");
                }
            }
            else if (kvp.Key.StartsWith("Num"))
            {
                Console.WriteLine($"[DEBUG] Key not found in any lookup: {kvp.Key}");
                Console.WriteLine($"[DEBUG] Available keys in keyLookup: {string.Join(", ", keyLookup.Keys.Where(k => k.StartsWith("Num")))}");
                Console.WriteLine($"[DEBUG] Available keys in alternativeKeyLookup: {string.Join(", ", alternativeKeyLookup.Keys.Where(k => k.StartsWith("Num")))}");
            }
        }

        return keyboardLayout;
    }

    /// <summary>
    /// Calculate logarithmic heat level for better visual distribution
    /// </summary>
    private static double CalculateLogHeatLevel(long count, long maxCount)
    {
        if (count <= 0) return 0.0;
        if (count >= maxCount) return 1.0;

        // Use log scale: log(count + 1) / log(maxCount + 1)
        var logCount = Math.Log(count + 1);
        var logMax = Math.Log(maxCount + 1);
        
        return Math.Min(1.0, logCount / logMax);
    }

    /// <summary>
    /// Calculate color from heat level using gradient (Blue -> Cyan -> Green -> Yellow -> Orange -> Red)
    /// </summary>
    public static HeatmapColor CalculateHeatColor(double heatLevel)
    {
        return HeatmapColor.CalculateHeatColor(heatLevel);
    }

    /// <summary>
    /// Get top N most frequently used keys with percentages
    /// </summary>
    public static List<(string Key, long Count, double Percentage)> GetTopKeysWithPercentage(
        Dictionary<string, long> keyStats, int topN = 10)
    {
        if (keyStats == null || keyStats.Count == 0)
            return new List<(string, long, double)>();

        var totalCount = keyStats.Values.Sum();
        if (totalCount == 0)
            return new List<(string, long, double)>();

        return keyStats
            .OrderByDescending(kvp => kvp.Value)
            .Take(topN)
            .Select(kvp => (
                Key: kvp.Key,
                Count: kvp.Value,
                Percentage: (double)kvp.Value / totalCount * 100
            ))
            .ToList();
    }

    /// <summary>
    /// Calculate typing speed (keys per minute) for a time period
    /// </summary>
    public static double CalculateTypingSpeed(long keyCount, TimeSpan duration)
    {
        if (duration.TotalMinutes <= 0)
            return 0;

        return keyCount / duration.TotalMinutes;
    }
}