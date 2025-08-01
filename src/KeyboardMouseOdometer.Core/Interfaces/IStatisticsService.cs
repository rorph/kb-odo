using KeyboardMouseOdometer.Core.Models;

namespace KeyboardMouseOdometer.Core.Interfaces;

public interface IStatisticsService
{
    OdometerData CurrentData { get; }
    SessionStatistics CurrentSession { get; }
    
    event EventHandler<OdometerData>? DataUpdated;
    event EventHandler<SessionStatistics>? SessionUpdated;
    
    void ProcessInputEvent(InputEvent inputEvent);
    void StartNewSession();
    void EndCurrentSession();
    Task<List<DailyStatistics>> GetDailyStatisticsAsync(DateTime startDate, DateTime endDate);
    Task<SessionStatistics> GetSessionStatisticsAsync(DateTime sessionStart);
    Task SaveCurrentDataAsync();
    Task LoadDataAsync();
    void ResetStatistics();
}