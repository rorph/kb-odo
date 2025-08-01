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
}