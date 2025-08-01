using KeyboardMouseOdometer.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace KeyboardMouseOdometer.Core.Services;

/// <summary>
/// Service that aggregates input events and logs them to the database
/// Implements in-memory aggregation as per PROJECT_SPEC architecture
/// </summary>
public class DataLoggerService : IDisposable
{
    private readonly ILogger<DataLoggerService> _logger;
    private readonly DatabaseService _databaseService;
    private readonly Models.Configuration _configuration;
    private readonly Timer _saveTimer;
    private readonly Timer _midnightTimer;
    private readonly Timer _uiUpdateTimer;

    // In-memory aggregation of today's data
    private DailyStats _todayStats;
    private HourlyStats _currentHourStats;
    private readonly object _statsLock = new object();
    private string _lastKeyPressed = string.Empty;

    // Thread-safe collections for detailed tracking
    private readonly ConcurrentDictionary<string, int> _keyFrequency = new();
    private readonly ConcurrentQueue<KeyMouseEvent> _pendingRawEvents = new();

    // Performance throttling
    private DateTime _lastMouseMoveTime = DateTime.MinValue;
    private DateTime _lastUIUpdateTime = DateTime.MinValue;
    private volatile bool _statsChanged = false;

    public event EventHandler<DailyStats>? StatsUpdated;
    public event EventHandler<string>? LastKeyChanged;

    public DataLoggerService(
        ILogger<DataLoggerService> logger, 
        DatabaseService databaseService, 
        Models.Configuration configuration)
    {
        _logger = logger;
        _databaseService = databaseService;
        _configuration = configuration;

        // Initialize today's stats
        _todayStats = DailyStats.CreateForToday();
        _currentHourStats = HourlyStats.CreateEmpty(_todayStats.Date, DateTime.Now.Hour);
        
        // Set up periodic save timer
        _saveTimer = new Timer(SavePendingData, null, 
            TimeSpan.FromMilliseconds(configuration.DatabaseSaveIntervalMs), 
            TimeSpan.FromMilliseconds(configuration.DatabaseSaveIntervalMs));

        // Set up midnight reset timer
        _midnightTimer = new Timer(HandleMidnightReset, null, 
            GetTimeUntilMidnight(), 
            TimeSpan.FromDays(1));

        // Set up UI update timer for throttled updates
        _uiUpdateTimer = new Timer(TriggerUIUpdate, null,
            TimeSpan.FromMilliseconds(configuration.UIUpdateIntervalMs),
            TimeSpan.FromMilliseconds(configuration.UIUpdateIntervalMs));

        _logger.LogInformation("DataLoggerService initialized");
    }

    /// <summary>
    /// Initialize the service and load today's existing data
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Load existing data for today
            var existingStats = await _databaseService.GetDailyStatsAsync(_todayStats.Date);
            if (existingStats != null)
            {
                lock (_statsLock)
                {
                    _todayStats = existingStats;
                }
                _logger.LogInformation("Loaded existing stats for today: {KeyCount} keys, {MouseDistance:F2}m", 
                    _todayStats.KeyCount, _todayStats.MouseDistance);
            }

            StatsUpdated?.Invoke(this, _todayStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize DataLoggerService");
            throw;
        }
    }

    /// <summary>
    /// Log a keyboard event
    /// </summary>
    public void LogKeyPress(string keyCode)
    {
        lock (_statsLock)
        {
            _todayStats.KeyCount++;
            _currentHourStats.KeyCount++;
            _lastKeyPressed = keyCode;
        }

        // Track key frequency
        _keyFrequency.AddOrUpdate(keyCode, 1, (key, count) => count + 1);

        // Log raw event if enabled
        if (_configuration.EnableRawEventLogging)
        {
            _pendingRawEvents.Enqueue(new KeyMouseEvent
            {
                Timestamp = DateTime.Now,
                EventType = "key_down",
                Key = _configuration.LogDetailedKeystrokes ? keyCode : "key" // Privacy consideration
            });
        }

        LastKeyChanged?.Invoke(this, keyCode);
        _statsChanged = true;
        
        // Removed trace logging to reduce CPU usage
    }

    /// <summary>
    /// Log mouse movement with throttling to prevent UI freezing
    /// </summary>
    public void LogMouseMove(double distancePixels)
    {
        if (distancePixels <= 0) return;

        var now = DateTime.Now;
        // Throttle mouse movement processing to prevent overwhelming the system
        if ((now - _lastMouseMoveTime).TotalMilliseconds < _configuration.MouseMovementThrottleMs)
        {
            return;
        }
        _lastMouseMoveTime = now;

        // Convert pixels to meters using more realistic calculation
        // Default mouse sensitivity factor of 2.0 provides reasonable scaling for typical usage
        double distanceMeters = Utils.DistanceCalculator.CalculateRealisticMouseTravelMeters(
            distancePixels, 
            mouseSensitivityFactor: 2.0); // Higher sensitivity = less physical movement needed

        lock (_statsLock)
        {
            _todayStats.MouseDistance += distanceMeters;
            _currentHourStats.MouseDistance += distanceMeters;
        }

        // Log raw event if enabled
        if (_configuration.EnableRawEventLogging)
        {
            _pendingRawEvents.Enqueue(new KeyMouseEvent
            {
                Timestamp = now,
                EventType = "mouse_move",
                MouseDx = distancePixels,
                MouseDy = 0 // We're storing total distance, not individual dx/dy
            });
        }

        _statsChanged = true;
        // Removed trace logging to reduce CPU usage
    }

    /// <summary>
    /// Log mouse click
    /// </summary>
    public void LogMouseClick(MouseButton button)
    {
        lock (_statsLock)
        {
            switch (button)
            {
                case MouseButton.Left:
                    _todayStats.LeftClicks++;
                    _currentHourStats.LeftClicks++;
                    break;
                case MouseButton.Right:
                    _todayStats.RightClicks++;
                    _currentHourStats.RightClicks++;
                    break;
                case MouseButton.Middle:
                    _todayStats.MiddleClicks++;
                    _currentHourStats.MiddleClicks++;
                    break;
            }
        }

        // Log raw event if enabled
        if (_configuration.EnableRawEventLogging)
        {
            _pendingRawEvents.Enqueue(new KeyMouseEvent
            {
                Timestamp = DateTime.Now,
                EventType = "mouse_click",
                MouseButton = button.ToString()
            });
        }

        _statsChanged = true;
        // Removed trace logging to reduce CPU usage
    }

    /// <summary>
    /// Log mouse scroll wheel activity
    /// </summary>
    public void LogMouseScroll(int wheelDelta)
    {
        // Convert scroll wheel delta to estimated distance using realistic calculation
        // Uses 0.8cm per line (more realistic than 1.5cm), 3 lines per notch = 2.4cm per notch
        double scrollDistance = Utils.DistanceCalculator.CalculateScrollDistanceMeters(
            wheelDelta, 
            scrollLinesPerNotch: 3, 
            averageLineHeightCm: 0.8); // More realistic line height

        lock (_statsLock)
        {
            _todayStats.ScrollDistance += scrollDistance;
            _currentHourStats.ScrollDistance += scrollDistance;
        }

        // Log raw event if enabled
        if (_configuration.EnableRawEventLogging)
        {
            _pendingRawEvents.Enqueue(new KeyMouseEvent
            {
                Timestamp = DateTime.Now,
                EventType = "mouse_scroll",
                WheelDelta = wheelDelta
            });
        }

        _statsChanged = true;
        // Removed trace logging to reduce CPU usage
    }

    /// <summary>
    /// Get current statistics (thread-safe copy)
    /// </summary>
    public DailyStats GetCurrentStats()
    {
        lock (_statsLock)
        {
            return new DailyStats
            {
                Date = _todayStats.Date,
                KeyCount = _todayStats.KeyCount,
                MouseDistance = _todayStats.MouseDistance,
                LeftClicks = _todayStats.LeftClicks,
                RightClicks = _todayStats.RightClicks,
                MiddleClicks = _todayStats.MiddleClicks,
                ScrollDistance = _todayStats.ScrollDistance
            };
        }
    }

    /// <summary>
    /// Get the last key pressed
    /// </summary>
    public string GetLastKeyPressed()
    {
        lock (_statsLock)
        {
            return _lastKeyPressed;
        }
    }

    /// <summary>
    /// Reset statistics (for testing or manual reset)
    /// </summary>
    public async Task ResetStatsAsync()
    {
        lock (_statsLock)
        {
            _todayStats = DailyStats.CreateForToday();
            _lastKeyPressed = string.Empty;
        }

        _keyFrequency.Clear();

        await _databaseService.SaveDailyStatsAsync(_todayStats);
        StatsUpdated?.Invoke(this, _todayStats);
        LastKeyChanged?.Invoke(this, string.Empty);

        _logger.LogInformation("Statistics reset");
    }

    /// <summary>
    /// Periodic callback to save data to database (now properly async)
    /// </summary>
    private void SavePendingData(object? state)
    {
        _ = SavePendingDataAsync();
    }

    /// <summary>
    /// Asynchronously save pending data to database
    /// </summary>
    private async Task SavePendingDataAsync()
    {
        try
        {
            DailyStats currentStats;
            lock (_statsLock)
            {
                currentStats = GetCurrentStats();
            }

            // Check if we need to save hourly stats and roll to next hour
            await CheckAndSaveHourlyStatsAsync();

            // Save daily stats
            await _databaseService.SaveDailyStatsAsync(currentStats);

            // Save raw events in batch if any
            var eventsBatch = new List<KeyMouseEvent>();
            while (_pendingRawEvents.TryDequeue(out var keyMouseEvent))
            {
                eventsBatch.Add(keyMouseEvent);
            }

            if (eventsBatch.Count > 0)
            {
                await _databaseService.SaveKeyMouseEventsBatchAsync(eventsBatch);
                _logger.LogDebug("Saved {EventCount} raw events to database in batch", eventsBatch.Count);
            }

            // Reduced verbose logging to improve performance
            _logger.LogDebug("Saved daily stats: {KeyCount} keys, {MouseDistance:F2}m", 
                currentStats.KeyCount, currentStats.MouseDistance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save pending data");
        }
    }

    /// <summary>
    /// Throttled UI update trigger
    /// </summary>
    private void TriggerUIUpdate(object? state)
    {
        if (_statsChanged)
        {
            _statsChanged = false;
            StatsUpdated?.Invoke(this, GetCurrentStats());
        }
    }

    /// <summary>
    /// Handle midnight rollover to new day
    /// </summary>
    private void HandleMidnightReset(object? state)
    {
        _ = HandleMidnightResetAsync();
    }

    /// <summary>
    /// Asynchronously handle midnight rollover to new day
    /// </summary>
    private async Task HandleMidnightResetAsync()
    {
        try
        {
            _logger.LogInformation("Midnight rollover - creating new daily stats");

            // Save final stats for the day that just ended
            await SavePendingDataAsync();

            // Reset for new day
            lock (_statsLock)
            {
                _todayStats = DailyStats.CreateForToday();
                _currentHourStats = HourlyStats.CreateEmpty(_todayStats.Date, DateTime.Now.Hour);
                _lastKeyPressed = string.Empty;
            }

            _keyFrequency.Clear();

            // Clean up old data if needed
            if (_configuration.DatabaseRetentionDays > 0)
            {
                await _databaseService.CleanupOldDataAsync(_configuration.DatabaseRetentionDays);
            }

            StatsUpdated?.Invoke(this, _todayStats);
            LastKeyChanged?.Invoke(this, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle midnight reset");
        }
    }

    /// <summary>
    /// Check if we need to save current hour stats and roll to next hour
    /// </summary>
    private async Task CheckAndSaveHourlyStatsAsync()
    {
        var currentHour = DateTime.Now.Hour;
        
        HourlyStats hourStatsToSave;
        lock (_statsLock)
        {
            // If hour changed, save current hour stats and create new hour stats
            if (_currentHourStats.Hour != currentHour)
            {
                hourStatsToSave = new HourlyStats
                {
                    Date = _currentHourStats.Date,
                    Hour = _currentHourStats.Hour,
                    KeyCount = _currentHourStats.KeyCount,
                    MouseDistance = _currentHourStats.MouseDistance,
                    LeftClicks = _currentHourStats.LeftClicks,
                    RightClicks = _currentHourStats.RightClicks,
                    MiddleClicks = _currentHourStats.MiddleClicks,
                    ScrollDistance = _currentHourStats.ScrollDistance
                };
                
                // Create new hour stats
                _currentHourStats = HourlyStats.CreateEmpty(_todayStats.Date, currentHour);
            }
            else
            {
                // Same hour, just save current stats
                hourStatsToSave = new HourlyStats
                {
                    Date = _currentHourStats.Date,
                    Hour = _currentHourStats.Hour,
                    KeyCount = _currentHourStats.KeyCount,
                    MouseDistance = _currentHourStats.MouseDistance,
                    LeftClicks = _currentHourStats.LeftClicks,
                    RightClicks = _currentHourStats.RightClicks,
                    MiddleClicks = _currentHourStats.MiddleClicks,
                    ScrollDistance = _currentHourStats.ScrollDistance
                };
            }
        }

        // Save hourly stats (convert to DailyStats format for now)
        var dailyStatsFormat = new DailyStats
        {
            Date = hourStatsToSave.Date,
            KeyCount = hourStatsToSave.KeyCount,
            MouseDistance = hourStatsToSave.MouseDistance,
            LeftClicks = hourStatsToSave.LeftClicks,
            RightClicks = hourStatsToSave.RightClicks,
            MiddleClicks = hourStatsToSave.MiddleClicks,
            ScrollDistance = hourStatsToSave.ScrollDistance
        };
        
        await _databaseService.SaveHourlyStatsAsync(hourStatsToSave.Date, hourStatsToSave.Hour, dailyStatsFormat);
    }

    /// <summary>
    /// Calculate time until next midnight
    /// </summary>
    private TimeSpan GetTimeUntilMidnight()
    {
        var now = DateTime.Now;
        var midnight = now.Date.AddDays(1);
        return midnight - now;
    }

    public void Dispose()
    {
        // Save any pending data before disposing
        try
        {
            SavePendingDataAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving data during dispose");
        }
        
        _saveTimer?.Dispose();
        _midnightTimer?.Dispose();
        _uiUpdateTimer?.Dispose();
        
        _logger.LogInformation("DataLoggerService disposed");
    }
}