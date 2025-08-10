using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Interfaces;
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
    private readonly IKeyCodeMapper _keyCodeMapper;
    private readonly Timer _saveTimer;
    private readonly Timer _midnightTimer;
    private readonly Timer _uiUpdateTimer;
    private readonly AppUsageService? _appUsageService;

    // In-memory aggregation of today's data
    private DailyStats _todayStats;
    private HourlyStats _currentHourStats;
    private readonly object _statsLock = new object();
    private string _lastKeyPressed = string.Empty;
    private bool _isRollingOver = false; // Prevent recursive rollover

    // Thread-safe collections for detailed tracking
    private readonly ConcurrentDictionary<string, int> _keyFrequency = new();
    private readonly ConcurrentDictionary<string, int> _currentHourKeyStats = new(); // For heatmap tracking
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
        Models.Configuration configuration,
        IKeyCodeMapper keyCodeMapper,
        ILogger<AppUsageService>? appUsageLogger = null)
    {
        _logger = logger;
        _databaseService = databaseService;
        _configuration = configuration;
        _keyCodeMapper = keyCodeMapper;
        
        // Initialize AppUsageService if app tracking is enabled
        if (_configuration.TrackApplicationUsage && appUsageLogger != null)
        {
            _appUsageService = new AppUsageService(appUsageLogger, databaseService, configuration);
        }

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
    /// Ensures stats objects are for the current date and hour
    /// </summary>
    private void EnsureCurrentDateStats()
    {
        // Prevent recursive rollover
        if (_isRollingOver)
        {
            _logger.LogDebug("Rollover already in progress, skipping");
            return;
        }
        
        var currentDate = DateTime.Today.ToString("yyyy-MM-dd");
        var currentHour = DateTime.Now.Hour;
        
        // Check if we need to roll over to a new day
        if (_todayStats.Date != currentDate)
        {
            _logger.LogInformation("Date changed from {OldDate} to {NewDate}, starting rollover", _todayStats.Date, currentDate);
            
            // Set flag to prevent recursive calls
            _isRollingOver = true;
            
            try
            {
                // Save the old day's stats WITHOUT calling GetCurrentStats (which would recurse)
                var statsToSave = new DailyStats
                {
                    Date = _todayStats.Date,
                    KeyCount = _todayStats.KeyCount,
                    MouseDistance = _todayStats.MouseDistance,
                    LeftClicks = _todayStats.LeftClicks,
                    RightClicks = _todayStats.RightClicks,
                    MiddleClicks = _todayStats.MiddleClicks,
                    ScrollDistance = _todayStats.ScrollDistance
                };
                
                _logger.LogInformation("Saving final stats for {Date}: {KeyCount} keys, {MouseDistance:F2}m", 
                    statsToSave.Date, statsToSave.KeyCount, statsToSave.MouseDistance);
                
                // Save directly without triggering recursive calls
                _databaseService.SaveDailyStatsAsync(statsToSave).GetAwaiter().GetResult();
                
                // Save any pending hourly stats for the old day
                if (_currentHourStats.KeyCount > 0 || _currentHourStats.MouseDistance > 0 || 
                    _currentHourStats.TotalClicks > 0 || _currentHourStats.ScrollDistance > 0)
                {
                    _logger.LogInformation("Saving final hourly stats for hour {Hour}", _currentHourStats.Hour);
                    _databaseService.SaveHourlyStatsAsync(_currentHourStats).GetAwaiter().GetResult();
                }
                
                // Save any pending key stats
                if (_currentHourKeyStats.Count > 0)
                {
                    var keyStatsList = _currentHourKeyStats.Select(kvp => new KeyStats
                    {
                        Date = _currentHourStats.Date,
                        Hour = _currentHourStats.Hour,
                        KeyCode = kvp.Key,
                        Count = kvp.Value
                    }).ToList();
                    
                    _logger.LogInformation("Saving {Count} key stats for final hour", keyStatsList.Count);
                    _databaseService.SaveKeyStatsBatchAsync(keyStatsList).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save data during day rollover");
            }
            finally
            {
                // Create new stats for today
                _todayStats = DailyStats.CreateForToday();
                _currentHourStats = HourlyStats.CreateEmpty(currentDate, currentHour);
                _keyFrequency.Clear();
                _currentHourKeyStats.Clear();
                _lastKeyPressed = string.Empty;
                
                _logger.LogInformation("Rollover complete, new day stats initialized for {Date}", currentDate);
                
                // Handle app usage day rollover
                if (_appUsageService != null)
                {
                    _ = Task.Run(async () => await _appUsageService.HandleDayRolloverAsync());
                }
                
                // Clear flag
                _isRollingOver = false;
            }
            
            // Clean up old data asynchronously (non-critical) - only if retention is enabled
            if (_configuration.DatabaseRetentionDays > 0)
            {
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await _databaseService.CleanupOldDataAsync(_configuration.DatabaseRetentionDays);
                        _logger.LogInformation("Old data cleanup completed");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to cleanup old data");
                    }
                });
            }
            else
            {
                _logger.LogDebug("Skipping data cleanup - retention disabled (DatabaseRetentionDays = {RetentionDays})", _configuration.DatabaseRetentionDays);
            }
        }
        
        // Check if we need to roll over to a new hour (only if not in day rollover)
        if (!_isRollingOver && _currentHourStats.Hour != currentHour)
        {
            _logger.LogInformation("Hour changed from {OldHour} to {NewHour}, rolling over hourly stats", _currentHourStats.Hour, currentHour);
            
            // Save current hour stats directly without recursion
            try
            {
                if (_currentHourStats.KeyCount > 0 || _currentHourStats.MouseDistance > 0 || 
                    _currentHourStats.TotalClicks > 0 || _currentHourStats.ScrollDistance > 0)
                {
                    _databaseService.SaveHourlyStatsAsync(_currentHourStats).GetAwaiter().GetResult();
                    _logger.LogDebug("Saved hourly stats for hour {Hour}", _currentHourStats.Hour);
                }
                
                // Save key stats for the hour
                if (_currentHourKeyStats.Count > 0)
                {
                    var keyStatsList = _currentHourKeyStats.Select(kvp => new KeyStats
                    {
                        Date = _currentHourStats.Date,
                        Hour = _currentHourStats.Hour,
                        KeyCode = kvp.Key,
                        Count = kvp.Value
                    }).ToList();
                    
                    _databaseService.SaveKeyStatsBatchAsync(keyStatsList).GetAwaiter().GetResult();
                    _logger.LogDebug("Saved {Count} key stats for hour {Hour}", keyStatsList.Count, _currentHourStats.Hour);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save hourly stats during hour rollover");
            }
            
            // Create new hour stats
            _currentHourStats = HourlyStats.CreateEmpty(currentDate, currentHour);
            _currentHourKeyStats.Clear();
            
            // Handle app usage hour rollover
            if (_appUsageService != null)
            {
                _ = Task.Run(async () => await _appUsageService.HandleHourRolloverAsync());
            }
        }
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
            
            // Start app usage tracking if enabled
            _appUsageService?.StartTracking();
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
            EnsureCurrentDateStats();
            _todayStats.KeyCount++;
            _currentHourStats.KeyCount++;
            _lastKeyPressed = keyCode;
        }

        // Track key frequency
        _keyFrequency.AddOrUpdate(keyCode, 1, (key, count) => count + 1);
        
        // Track key stats for heatmap (using human-readable key identifier)
        var keyIdentifier = _keyCodeMapper.GetKeyName(keyCode);
        _currentHourKeyStats.AddOrUpdate(keyIdentifier, 1, (key, count) => count + 1);
        
        // Debug logging for numpad keys
        if (keyCode.StartsWith("NumPad") || keyIdentifier.StartsWith("Num"))
        {
            _logger.LogDebug("NumPad key logged - Raw: {RawKey}, Identifier: {KeyId}", keyCode, keyIdentifier);
        }

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
    /// Log a keyboard event with key identifier (for heatmap feature)
    /// </summary>
    public void LogKeyPress(string keyCode, string keyIdentifier)
    {
        lock (_statsLock)
        {
            EnsureCurrentDateStats();
            _todayStats.KeyCount++;
            _currentHourStats.KeyCount++;
            _lastKeyPressed = keyCode;
        }

        // Track key frequency
        _keyFrequency.AddOrUpdate(keyCode, 1, (key, count) => count + 1);
        
        // Track key stats for heatmap using provided identifier
        _currentHourKeyStats.AddOrUpdate(keyIdentifier, 1, (key, count) => count + 1);

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
    /// Log a generic input event (for compatibility)
    /// </summary>
    public void LogInputEvent(InputEvent inputEvent)
    {
        switch (inputEvent.EventType)
        {
            case InputEventType.KeyDown: // Handles both KeyDown and KeyPress (alias)
                if (!string.IsNullOrEmpty(inputEvent.KeyIdentifier))
                {
                    LogKeyPress(inputEvent.KeyCode ?? inputEvent.KeyIdentifier, inputEvent.KeyIdentifier);
                }
                else if (!string.IsNullOrEmpty(inputEvent.KeyCode))
                {
                    LogKeyPress(inputEvent.KeyCode);
                }
                break;
            case InputEventType.MouseMove:
                // Calculate distance if coordinates are provided
                if (inputEvent.X != 0 || inputEvent.Y != 0)
                {
                    var distance = Math.Sqrt(inputEvent.X * inputEvent.X + inputEvent.Y * inputEvent.Y);
                    LogMouseMove(distance);
                }
                break;
            case InputEventType.MouseClick:
                if (inputEvent.MouseButton.HasValue)
                {
                    LogMouseClick(inputEvent.MouseButton.Value);
                }
                break;
            case InputEventType.MouseWheel:
                LogMouseScroll(inputEvent.WheelDelta);
                break;
        }
    }

    /// <summary>
    /// Flush pending data to database (for compatibility)
    /// </summary>
    public async Task FlushAsync()
    {
        await SavePendingDataAsync();
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
            EnsureCurrentDateStats();
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
            EnsureCurrentDateStats();
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
            EnsureCurrentDateStats();
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
            // Only ensure current date if not already rolling over
            if (!_isRollingOver)
            {
                EnsureCurrentDateStats();
            }
            
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
            // Skip if rollover is in progress
            if (_isRollingOver)
            {
                _logger.LogDebug("Skipping save during rollover");
                return;
            }
            
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
            _logger.LogInformation("Midnight timer triggered - checking for rollover");
            
            // Check if rollover is already in progress
            if (_isRollingOver)
            {
                _logger.LogWarning("Midnight rollover already in progress, skipping timer-triggered rollover");
                return;
            }

            var currentDate = DateTime.Today.ToString("yyyy-MM-dd");
            
            // Double-check if we actually need to roll over
            lock (_statsLock)
            {
                if (_todayStats.Date == currentDate)
                {
                    _logger.LogDebug("Date already matches current date {Date}, no rollover needed", currentDate);
                    return;
                }
            }

            _logger.LogInformation("Midnight rollover confirmed - transitioning from {OldDate} to {NewDate}", _todayStats.Date, currentDate);

            // The actual rollover will happen via EnsureCurrentDateStats on the next operation
            // Force it to happen now by triggering an update
            lock (_statsLock)
            {
                EnsureCurrentDateStats();
            }

            _keyFrequency.Clear();
            _currentHourKeyStats.Clear();

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
        Dictionary<string, int>? keyStatsToSave = null;
        
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
                
                // Get current key stats to save and clear for new hour
                if (_currentHourKeyStats.Any())
                {
                    keyStatsToSave = new Dictionary<string, int>(_currentHourKeyStats);
                    _currentHourKeyStats.Clear();
                }
                
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
                
                // Get current key stats snapshot to save
                if (_currentHourKeyStats.Any())
                {
                    keyStatsToSave = new Dictionary<string, int>(_currentHourKeyStats);
                }
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
        
        // Save key stats for heatmap
        if (keyStatsToSave != null && keyStatsToSave.Any())
        {
            await _databaseService.SaveKeyStatsAsync(hourStatsToSave.Date, hourStatsToSave.Hour, keyStatsToSave);
            _logger.LogDebug("Saved {KeyCount} individual key stats for hour {Hour}", keyStatsToSave.Count, hourStatsToSave.Hour);
        }
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
            // Use GetAwaiter().GetResult() instead of Wait() to avoid deadlock
            SavePendingDataAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving data during dispose");
        }
        
        _saveTimer?.Dispose();
        _midnightTimer?.Dispose();
        _uiUpdateTimer?.Dispose();
        _appUsageService?.Dispose();
        
        _logger.LogInformation("DataLoggerService disposed");
    }
}