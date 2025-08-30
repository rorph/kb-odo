using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using KeyboardMouseOdometer.Core.Models;

namespace KeyboardMouseOdometer.Core.Services;

/// <summary>
/// Service that tracks application usage by monitoring which application has focus
/// </summary>
public class AppUsageService : IDisposable
{
    private readonly ILogger<AppUsageService> _logger;
    private readonly DatabaseService _databaseService;
    private readonly Models.Configuration _configuration;
    private readonly Timer _trackingTimer;
    private readonly Timer _saveTimer;
    
    private string? _currentAppName;
    private DateTime _lastCheckTime;
    private readonly Dictionary<string, int> _pendingAppUsage = new();
    private readonly object _usageLock = new();
    private bool _isTracking = false;
    
    // Windows API imports
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowTextLength(IntPtr hWnd);
    
    public AppUsageService(
        ILogger<AppUsageService> logger,
        DatabaseService databaseService,
        Models.Configuration configuration)
    {
        _logger = logger;
        _databaseService = databaseService;
        _configuration = configuration;
        
        _lastCheckTime = DateTime.Now;
        
        // Check focus every second
        _trackingTimer = new Timer(TrackApplicationFocus, null, 
            TimeSpan.FromSeconds(1), 
            TimeSpan.FromSeconds(1));
        
        // Save pending data every 30 seconds
        _saveTimer = new Timer(SavePendingData, null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
        
        _logger.LogInformation("AppUsageService initialized");
    }
    
    /// <summary>
    /// Start tracking application usage
    /// </summary>
    public void StartTracking()
    {
        if (!_configuration.TrackApplicationUsage)
        {
            _logger.LogDebug("Application tracking is disabled in configuration");
            return;
        }
        
        _isTracking = true;
        _lastCheckTime = DateTime.Now;
        _logger.LogInformation("Started tracking application usage");
    }
    
    /// <summary>
    /// Stop tracking application usage
    /// </summary>
    public void StopTracking()
    {
        _isTracking = false;
        
        // Save any pending data
        SavePendingDataAsync().GetAwaiter().GetResult();
        
        _logger.LogInformation("Stopped tracking application usage");
    }
    
    /// <summary>
    /// Track which application currently has focus
    /// </summary>
    private void TrackApplicationFocus(object? state)
    {
        if (!_isTracking || !_configuration.TrackApplicationUsage)
            return;
        
        try
        {
            var currentApp = GetFocusedApplicationName();
            var now = DateTime.Now;
            
            // Always track time for the current app if we have one
            if (!string.IsNullOrEmpty(_currentAppName))
            {
                // Calculate elapsed time more precisely
                var elapsed = now - _lastCheckTime;
                var secondsElapsed = Math.Round(elapsed.TotalSeconds);
                
                if (secondsElapsed >= 1)
                {
                    // Add time to the current app
                    lock (_usageLock)
                    {
                        if (_pendingAppUsage.ContainsKey(_currentAppName))
                            _pendingAppUsage[_currentAppName] += (int)secondsElapsed;
                        else
                            _pendingAppUsage[_currentAppName] = (int)secondsElapsed;
                    }
                    
                    _logger.LogTrace("Added {Seconds}s to {App}", (int)secondsElapsed, _currentAppName);
                    _lastCheckTime = now;
                }
            }
            else
            {
                // No previous app, just update the time
                _lastCheckTime = now;
            }
            
            // Update current app if it changed
            if (currentApp != _currentAppName)
            {
                _logger.LogTrace("App focus changed from {OldApp} to {NewApp}", _currentAppName, currentApp);
                _currentAppName = currentApp;
                _lastCheckTime = now; // Reset timer for new app
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking application focus");
        }
    }
    
    /// <summary>
    /// Get the name of the currently focused application
    /// </summary>
    private string? GetFocusedApplicationName()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return null;
            
            // Get the process ID
            uint processId;
            GetWindowThreadProcessId(hwnd, out processId);
            
            if (processId == 0)
                return null;
            
            // Get the process
            var process = Process.GetProcessById((int)processId);
            
            // Get the main module name (executable name)
            string appName = process.ProcessName;
            
            // Special handling for ApplicationFrameHost (UWP apps)
            if (appName == "ApplicationFrameHost")
            {
                // Try to get the actual UWP app name from window title
                var length = GetWindowTextLength(hwnd);
                if (length > 0)
                {
                    var builder = new StringBuilder(length + 1);
                    if (GetWindowText(hwnd, builder, builder.Capacity) > 0)
                    {
                        var windowTitle = builder.ToString();
                        // Use window title for UWP apps, but clean it up
                        if (!string.IsNullOrWhiteSpace(windowTitle))
                        {
                            // Remove common suffixes
                            appName = windowTitle.Split('-')[0].Trim();
                            if (appName.Length > 50) // Truncate very long titles
                                appName = appName.Substring(0, 50);
                        }
                    }
                }
            }
            else
            {
                // Try to get the main module file name for better identification
                try
                {
                    if (process.MainModule != null)
                    {
                        appName = Path.GetFileNameWithoutExtension(process.MainModule.FileName) ?? process.ProcessName;
                    }
                }
                catch
                {
                    // Some system processes don't allow access to MainModule
                    // Use ProcessName as fallback
                }
            }
            
            return appName;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Could not get focused application name");
            return null;
        }
    }
    
    /// <summary>
    /// Save pending application usage data to database
    /// </summary>
    private void SavePendingData(object? state)
    {
        _ = SavePendingDataAsync();
    }
    
    /// <summary>
    /// Save pending application usage data to database
    /// </summary>
    public async Task SavePendingDataAsync()
    {
        Dictionary<string, int> dataToSave;
        
        lock (_usageLock)
        {
            if (_pendingAppUsage.Count == 0)
                return;
            
            dataToSave = new Dictionary<string, int>(_pendingAppUsage);
            _pendingAppUsage.Clear();
        }
        
        var date = DateTime.Today.ToString("yyyy-MM-dd");
        var hour = DateTime.Now.Hour;
        
        foreach (var kvp in dataToSave)
        {
            try
            {
                await _databaseService.SaveAppUsageStatsAsync(date, hour, kvp.Key, kvp.Value);
                _logger.LogDebug("Saved {Seconds}s for {App}", kvp.Value, kvp.Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save app usage for {App}", kvp.Key);
            }
        }
    }
    
    /// <summary>
    /// Handle hour rollover - save current data and reset for new hour
    /// </summary>
    public async Task HandleHourRolloverAsync()
    {
        // Save current pending data
        await SavePendingDataAsync();
        
        // Reset tracking for new hour
        _lastCheckTime = DateTime.Now;
        _currentAppName = null;
        
        _logger.LogDebug("Handled hour rollover for app usage tracking");
    }
    
    /// <summary>
    /// Handle day rollover - save current data and reset for new day
    /// </summary>
    public async Task HandleDayRolloverAsync()
    {
        // Save current pending data
        await SavePendingDataAsync();
        
        // Reset tracking for new day
        _lastCheckTime = DateTime.Now;
        _currentAppName = null;
        
        _logger.LogInformation("Handled day rollover for app usage tracking");
    }
    
    public void Dispose()
    {
        _isTracking = false;
        
        _trackingTimer?.Dispose();
        _saveTimer?.Dispose();
        
        // Save any remaining data
        try
        {
            SavePendingDataAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving data during dispose");
        }
        
        _logger.LogInformation("AppUsageService disposed");
    }
}