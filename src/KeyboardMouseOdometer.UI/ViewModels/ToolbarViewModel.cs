using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Services;
using KeyboardMouseOdometer.Core.Utils;
using KeyboardMouseOdometer.UI.Services;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace KeyboardMouseOdometer.UI.ViewModels;

public partial class ToolbarViewModel : ObservableObject
{
    private readonly ILogger<ToolbarViewModel> _logger;
    private readonly DataLoggerService _dataLoggerService;
    private readonly GlobalHookService _hookService;
    private readonly Core.Models.Configuration _configuration;

    [ObservableProperty]
    private string todayKeyCount = "0";

    [ObservableProperty]
    private string todayMouseDistance = "0 m";

    [ObservableProperty]
    private string todayScrollDistance = "0 m";

    [ObservableProperty]
    private string lastKeyPressed = "";


    public ToolbarViewModel(
        ILogger<ToolbarViewModel> logger,
        DataLoggerService dataLoggerService,
        GlobalHookService hookService,
        Core.Models.Configuration configuration)
    {
        _logger = logger;
        _dataLoggerService = dataLoggerService;
        _hookService = hookService;
        _configuration = configuration;

        // Subscribe to data updates
        _dataLoggerService.StatsUpdated += OnStatsUpdated;
        _dataLoggerService.LastKeyChanged += OnLastKeyChanged;

        // Load initial data
        LoadInitialData();
    }

    private void LoadInitialData()
    {
        var todayStats = _dataLoggerService.GetCurrentStats();
        OnStatsUpdated(null, todayStats);

        var lastKey = _dataLoggerService.GetLastKeyPressed();
        OnLastKeyChanged(null, lastKey);
    }

    private void OnStatsUpdated(object? sender, DailyStats stats)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            TodayKeyCount = FormatUtils.FormatNumber(stats.KeyCount);
            TodayMouseDistance = DistanceCalculator.FormatDistanceAutoScale(stats.MouseDistance, _configuration.DistanceUnit);
            TodayScrollDistance = DistanceCalculator.FormatDistanceAutoScale(stats.ScrollDistance, _configuration.DistanceUnit);
        });
    }

    private void OnLastKeyChanged(object? sender, string keyCode)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LastKeyPressed = string.IsNullOrEmpty(keyCode) ? "-" : FormatUtils.FormatKeyCode(keyCode);
        });
    }

    [RelayCommand]
    private void OpenDashboard()
    {
        try
        {
            // Find and show the main window
            var mainWindow = Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.GetType().Name == "MainWindow");
            
            if (mainWindow != null)
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open dashboard");
        }
    }

}