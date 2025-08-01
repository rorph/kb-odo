using KeyboardMouseOdometer.Core.Models;

namespace KeyboardMouseOdometer.Core.Interfaces;

public interface IDataStorageService
{
    Task SaveOdometerDataAsync(OdometerData data);
    Task<OdometerData?> LoadOdometerDataAsync();
    Task SaveSessionStatisticsAsync(SessionStatistics session);
    Task<List<SessionStatistics>> LoadSessionStatisticsAsync(DateTime startDate, DateTime endDate);
    Task SaveDailyStatisticsAsync(DailyStatistics dailyStats);
    Task<List<DailyStatistics>> LoadDailyStatisticsAsync(DateTime startDate, DateTime endDate);
    Task<bool> DataExistsAsync();
    Task ClearAllDataAsync();
    Task ExportDataAsync(string filePath, DateTime startDate, DateTime endDate);
    Task ImportDataAsync(string filePath);
}