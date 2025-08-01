using KeyboardMouseOdometer.Core.Interfaces;
using KeyboardMouseOdometer.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace KeyboardMouseOdometer.Core.Services;

public class JsonDataStorageService : IDataStorageService
{
    private readonly ILogger<JsonDataStorageService> _logger;
    private readonly string _dataDirectory;
    private readonly string _odometerDataFile;
    private readonly string _sessionsDirectory;
    private readonly string _dailyStatsDirectory;

    public JsonDataStorageService(ILogger<JsonDataStorageService> logger)
    {
        _logger = logger;
        _dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KeyboardMouseOdometer");
        _odometerDataFile = Path.Combine(_dataDirectory, "odometer_data.json");
        _sessionsDirectory = Path.Combine(_dataDirectory, "sessions");
        _dailyStatsDirectory = Path.Combine(_dataDirectory, "daily_stats");

        EnsureDirectoriesExist();
    }

    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_dataDirectory);
        Directory.CreateDirectory(_sessionsDirectory);
        Directory.CreateDirectory(_dailyStatsDirectory);
    }

    public async Task SaveOdometerDataAsync(OdometerData data)
    {
        try
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            await File.WriteAllTextAsync(_odometerDataFile, json);
            _logger.LogDebug("Odometer data saved to {FilePath}", _odometerDataFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save odometer data");
            throw;
        }
    }

    public async Task<OdometerData?> LoadOdometerDataAsync()
    {
        try
        {
            if (!File.Exists(_odometerDataFile))
                return null;

            var json = await File.ReadAllTextAsync(_odometerDataFile);
            var data = JsonConvert.DeserializeObject<OdometerData>(json);
            _logger.LogDebug("Odometer data loaded from {FilePath}", _odometerDataFile);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load odometer data");
            return null;
        }
    }

    public async Task SaveSessionStatisticsAsync(SessionStatistics session)
    {
        try
        {
            var fileName = $"session_{session.SessionStart:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(_sessionsDirectory, fileName);
            
            var json = JsonConvert.SerializeObject(session, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json);
            _logger.LogDebug("Session statistics saved to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save session statistics");
            throw;
        }
    }

    public async Task<List<SessionStatistics>> LoadSessionStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        var sessions = new List<SessionStatistics>();

        try
        {
            if (!Directory.Exists(_sessionsDirectory))
                return sessions;

            var files = Directory.GetFiles(_sessionsDirectory, "session_*.json");
            
            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var session = JsonConvert.DeserializeObject<SessionStatistics>(json);
                    
                    if (session != null && session.SessionStart.Date >= startDate.Date && session.SessionStart.Date <= endDate.Date)
                    {
                        sessions.Add(session);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load session file {FilePath}", file);
                }
            }

            _logger.LogDebug("Loaded {Count} session statistics", sessions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load session statistics");
        }

        return sessions.OrderBy(s => s.SessionStart).ToList();
    }

    public async Task SaveDailyStatisticsAsync(DailyStatistics dailyStats)
    {
        try
        {
            var fileName = $"daily_{dailyStats.Date:yyyyMMdd}.json";
            var filePath = Path.Combine(_dailyStatsDirectory, fileName);
            
            var json = JsonConvert.SerializeObject(dailyStats, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json);
            _logger.LogDebug("Daily statistics saved to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save daily statistics");
            throw;
        }
    }

    public async Task<List<DailyStatistics>> LoadDailyStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        var dailyStats = new List<DailyStatistics>();

        try
        {
            if (!Directory.Exists(_dailyStatsDirectory))
                return dailyStats;

            var files = Directory.GetFiles(_dailyStatsDirectory, "daily_*.json");
            
            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var stats = JsonConvert.DeserializeObject<DailyStatistics>(json);
                    
                    if (stats != null && stats.Date.Date >= startDate.Date && stats.Date.Date <= endDate.Date)
                    {
                        dailyStats.Add(stats);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load daily stats file {FilePath}", file);
                }
            }

            _logger.LogDebug("Loaded {Count} daily statistics", dailyStats.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load daily statistics");
        }

        return dailyStats.OrderBy(s => s.Date).ToList();
    }

    public async Task<bool> DataExistsAsync()
    {
        return await Task.FromResult(File.Exists(_odometerDataFile));
    }

    public async Task ClearAllDataAsync()
    {
        try
        {
            if (File.Exists(_odometerDataFile))
                File.Delete(_odometerDataFile);

            if (Directory.Exists(_sessionsDirectory))
                Directory.Delete(_sessionsDirectory, true);

            if (Directory.Exists(_dailyStatsDirectory))
                Directory.Delete(_dailyStatsDirectory, true);

            EnsureDirectoriesExist();
            _logger.LogInformation("All data cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear all data");
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task ExportDataAsync(string filePath, DateTime startDate, DateTime endDate)
    {
        try
        {
            var exportData = new
            {
                ExportDate = DateTime.Now,
                StartDate = startDate,
                EndDate = endDate,
                OdometerData = await LoadOdometerDataAsync(),
                Sessions = await LoadSessionStatisticsAsync(startDate, endDate),
                DailyStatistics = await LoadDailyStatisticsAsync(startDate, endDate)
            };

            var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json);
            _logger.LogInformation("Data exported to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export data to {FilePath}", filePath);
            throw;
        }
    }

    public async Task ImportDataAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Import file not found: {filePath}");

            var json = await File.ReadAllTextAsync(filePath);
            var importData = JsonConvert.DeserializeAnonymousType(json, new
            {
                ExportDate = DateTime.MinValue,
                StartDate = DateTime.MinValue,
                EndDate = DateTime.MinValue,
                OdometerData = (OdometerData?)null,
                Sessions = new List<SessionStatistics>(),
                DailyStatistics = new List<DailyStatistics>()
            });

            if (importData != null)
            {
                if (importData.OdometerData != null)
                    await SaveOdometerDataAsync(importData.OdometerData);

                foreach (var session in importData.Sessions)
                    await SaveSessionStatisticsAsync(session);

                foreach (var dailyStat in importData.DailyStatistics)
                    await SaveDailyStatisticsAsync(dailyStat);

                _logger.LogInformation("Data imported from {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import data from {FilePath}", filePath);
            throw;
        }
    }
}