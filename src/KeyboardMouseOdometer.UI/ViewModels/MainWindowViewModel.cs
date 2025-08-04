using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Services;
using KeyboardMouseOdometer.Core.Utils;
using KeyboardMouseOdometer.UI.Services;
using Microsoft.Extensions.Logging;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace KeyboardMouseOdometer.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly DatabaseService _databaseService;
    private readonly DataLoggerService _dataLoggerService;
    private readonly Core.Models.Configuration _configuration;
    private readonly ThemeManager _themeManager;
    private readonly Timer _chartUpdateTimer;
    private DateTime _lastChartUpdate = DateTime.MinValue;

    [ObservableProperty]
    private string todayKeyCount = "0";

    [ObservableProperty]
    private string todayMouseDistance = "0 m";

    [ObservableProperty]
    private string todayClickCount = "0";

    [ObservableProperty]
    private string todayScrollDistance = "0 m";

    [ObservableProperty]
    private string lastKeyPressed = "";

    [ObservableProperty]
    private PlotModel keysPerHourChart = new();

    [ObservableProperty]
    private PlotModel mouseDistancePerHourChart = new();

    [ObservableProperty]
    private PlotModel weeklyKeysChart = new();

    [ObservableProperty]
    private PlotModel weeklyMouseChart = new();

    [ObservableProperty]
    private PlotModel monthlyKeysChart = new();

    [ObservableProperty]
    private PlotModel monthlyMouseChart = new();

    [ObservableProperty]
    private PlotModel scrollDistancePerHourChart = new();

    [ObservableProperty]
    private PlotModel weeklyScrollChart = new();

    [ObservableProperty]
    private PlotModel monthlyScrollChart = new();

    // Settings properties
    [ObservableProperty]
    private bool trackKeystrokes;

    [ObservableProperty]
    private bool trackMouseMovement;

    [ObservableProperty]
    private bool trackMouseClicks;

    [ObservableProperty]
    private bool trackScrollWheel;

    [ObservableProperty]
    private bool showToolbar;

    [ObservableProperty]
    private bool minimizeToTray;

    [ObservableProperty]
    private bool startWithWindows;

    [ObservableProperty]
    private string distanceUnit = "metric";

    [ObservableProperty]
    private int databaseRetentionDays;

    // Lifetime stats properties
    [ObservableProperty]
    private string lifetimeKeyCount = "0";

    [ObservableProperty]
    private string lifetimeMouseDistance = "0 m";

    [ObservableProperty]
    private string lifetimeLeftClicks = "0";

    [ObservableProperty]
    private string lifetimeRightClicks = "0";

    [ObservableProperty]
    private string lifetimeMiddleClicks = "0";

    [ObservableProperty]
    private string lifetimeTotalClicks = "0";

    [ObservableProperty]
    private string lifetimeScrollDistance = "0 m";

    [ObservableProperty]
    private string trackingPeriod = "No data available";

    [ObservableProperty]
    private HeatmapViewModel? heatmapViewModel;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        DatabaseService databaseService,
        DataLoggerService dataLoggerService,
        Core.Models.Configuration configuration,
        HeatmapViewModel heatmapViewModel,
        ThemeManager themeManager)
    {
        _logger = logger;
        _databaseService = databaseService;
        _dataLoggerService = dataLoggerService;
        _configuration = configuration;
        _themeManager = themeManager;
        HeatmapViewModel = heatmapViewModel;

        // Load settings
        LoadSettings();

        // Subscribe to data updates
        _dataLoggerService.StatsUpdated += OnStatsUpdated;
        _dataLoggerService.LastKeyChanged += OnLastKeyChanged;
        
        // Subscribe to theme changes
        _themeManager.ThemeChanged += OnThemeChanged;

        // Initialize charts
        InitializeCharts();

        // Set up chart update timer
        _chartUpdateTimer = new Timer(UpdateChartsCallback, null,
            TimeSpan.FromSeconds(configuration.ChartUpdateIntervalSeconds),
            TimeSpan.FromSeconds(configuration.ChartUpdateIntervalSeconds));

        // Load initial data
        _ = Task.Run(LoadInitialDataAsync);
        
        // Load lifetime stats
        _ = Task.Run(LoadLifetimeStatsAsync);
    }

    private void LoadSettings()
    {
        _logger.LogInformation("Loading settings from configuration");
        
        TrackKeystrokes = _configuration.TrackKeystrokes;
        TrackMouseMovement = _configuration.TrackMouseMovement;
        TrackMouseClicks = _configuration.TrackMouseClicks;
        TrackScrollWheel = _configuration.TrackScrollWheel;
        ShowToolbar = _configuration.ShowToolbar;
        MinimizeToTray = _configuration.MinimizeToTray;
        StartWithWindows = _configuration.StartWithWindows;
        DistanceUnit = _configuration.DistanceUnit;
        DatabaseRetentionDays = _configuration.DatabaseRetentionDays;
        
        _logger.LogInformation($"Settings loaded - TrackKeystrokes: {TrackKeystrokes}, ShowToolbar: {ShowToolbar}, MinimizeToTray: {MinimizeToTray}");
    }

    private void OnStatsUpdated(object? sender, DailyStats stats)
    {
        // Keep basic stats updating in real-time
        Application.Current.Dispatcher.Invoke(() =>
        {
            TodayKeyCount = FormatUtils.FormatNumber(stats.KeyCount);
            TodayMouseDistance = DistanceCalculator.FormatDistanceAutoScale(stats.MouseDistance, DistanceUnit);
            TodayClickCount = FormatUtils.FormatNumber(stats.TotalClicks);
            TodayScrollDistance = DistanceCalculator.FormatDistanceAutoScale(stats.ScrollDistance, DistanceUnit);
        });

        // Charts are updated on a separate timer for performance
    }

    private void OnLastKeyChanged(object? sender, string keyCode)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LastKeyPressed = FormatUtils.FormatKeyCode(keyCode);
        });
    }

    private void InitializeCharts()
    {
        KeysPerHourChart = CreateHourlyChart("Keys per Hour", "Keys");
        MouseDistancePerHourChart = CreateHourlyChart("Mouse Distance per Hour", "Distance (m)");
        ScrollDistancePerHourChart = CreateHourlyChart("Scroll Distance per Hour", "Distance (m)");
        WeeklyKeysChart = CreateDailyChart("Keys This Week", "Keys");
        WeeklyMouseChart = CreateDailyChart("Mouse Distance This Week", "Distance (m)");
        WeeklyScrollChart = CreateDailyChart("Scroll Distance This Week", "Distance (m)");
        MonthlyKeysChart = CreateDailyChart("Keys This Month", "Keys");
        MonthlyMouseChart = CreateDailyChart("Mouse Distance This Month", "Distance (m)");
        MonthlyScrollChart = CreateDailyChart("Scroll Distance This Month", "Distance (m)");
    }

    private PlotModel CreateHourlyChart(string title, string yAxisTitle)
    {
        var plotModel = new PlotModel 
        { 
            Title = title,
            Background = OxyColors.Transparent,
            PlotAreaBackground = OxyColors.Transparent
        };
        
        // Get theme-appropriate colors
        var isDarkTheme = _themeManager.CurrentTheme == AppTheme.Dark;
        var textColor = isDarkTheme ? OxyColor.FromRgb(204, 204, 204) : OxyColor.FromRgb(51, 51, 51);
        var gridLineColor = isDarkTheme ? OxyColor.FromRgb(63, 63, 63) : OxyColor.FromRgb(224, 224, 224);
        var lineColor = isDarkTheme ? OxyColor.FromRgb(64, 160, 255) : OxyColor.FromRgb(0, 120, 212);
        
        plotModel.TextColor = textColor;
        plotModel.PlotAreaBorderColor = gridLineColor;
        
        plotModel.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "HH:mm",
            Title = "Time",
            TextColor = textColor,
            TicklineColor = gridLineColor,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = gridLineColor,
            MinorGridlineStyle = LineStyle.None
        });
        
        plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = yAxisTitle,
            TextColor = textColor,
            TicklineColor = gridLineColor,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = gridLineColor,
            MinorGridlineStyle = LineStyle.None
        });

        var series = new LineSeries
        {
            Title = yAxisTitle,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            Color = lineColor,
            StrokeThickness = 2,
            MarkerFill = lineColor
        };
        
        plotModel.Series.Add(series);
        return plotModel;
    }

    private PlotModel CreateDailyChart(string title, string yAxisTitle)
    {
        var plotModel = new PlotModel 
        { 
            Title = title,
            Background = OxyColors.Transparent,
            PlotAreaBackground = OxyColors.Transparent
        };
        
        // Get theme-appropriate colors
        var isDarkTheme = _themeManager.CurrentTheme == AppTheme.Dark;
        var textColor = isDarkTheme ? OxyColor.FromRgb(204, 204, 204) : OxyColor.FromRgb(51, 51, 51);
        var gridLineColor = isDarkTheme ? OxyColor.FromRgb(63, 63, 63) : OxyColor.FromRgb(224, 224, 224);
        var lineColor = isDarkTheme ? OxyColor.FromRgb(64, 160, 255) : OxyColor.FromRgb(0, 120, 212);
        
        plotModel.TextColor = textColor;
        plotModel.PlotAreaBorderColor = gridLineColor;
        
        plotModel.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "MM/dd",
            Title = "Date",
            TextColor = textColor,
            TicklineColor = gridLineColor,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = gridLineColor,
            MinorGridlineStyle = LineStyle.None
        });
        
        plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = yAxisTitle,
            TextColor = textColor,
            TicklineColor = gridLineColor,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = gridLineColor,
            MinorGridlineStyle = LineStyle.None
        });

        var series = new LineSeries
        {
            Title = yAxisTitle,
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            Color = lineColor,
            StrokeThickness = 2,
            MarkerFill = lineColor
        };
        
        plotModel.Series.Add(series);
        return plotModel;
    }

    private async Task LoadInitialDataAsync()
    {
        try
        {
            var todayStats = _dataLoggerService.GetCurrentStats();
            OnStatsUpdated(null, todayStats);

            var lastKey = _dataLoggerService.GetLastKeyPressed();
            OnLastKeyChanged(null, lastKey);

            // Initial chart update
            await UpdateChartsAsync();
            _lastChartUpdate = DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load initial data");
        }
    }

    /// <summary>
    /// Timer callback for chart updates
    /// </summary>
    private void UpdateChartsCallback(object? state)
    {
        _ = Task.Run(UpdateChartsAsync);
    }

    private async Task UpdateChartsAsync()
    {
        try
        {
            var today = DateTime.Today;
            
            // Update hourly charts for today
            var todayHourlyStats = await _databaseService.GetHourlyStatsAsync(today.ToString("yyyy-MM-dd"));
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateHourlyCharts(todayHourlyStats);
            });
            
            // Update weekly charts
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var weekEnd = weekStart.AddDays(6);
            var weeklyStats = await _databaseService.GetDailyStatsRangeAsync(weekStart, weekEnd);
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateWeeklyCharts(weeklyStats);
            });

            // Update monthly charts
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var monthlyStats = await _databaseService.GetDailyStatsRangeAsync(monthStart, monthEnd);
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateMonthlyCharts(monthlyStats);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update charts");
        }
    }

    private void UpdateWeeklyCharts(List<DailyStats> stats)
    {
        // Update weekly keys chart
        var keySeries = (LineSeries)WeeklyKeysChart.Series[0];
        keySeries.Points.Clear();
        
        foreach (var stat in stats)
        {
            var date = DateTime.ParseExact(stat.Date, "yyyy-MM-dd", null);
            keySeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(date), stat.KeyCount));
        }
        
        WeeklyKeysChart.InvalidatePlot(true);

        // Update weekly mouse chart
        var mouseSeries = (LineSeries)WeeklyMouseChart.Series[0];
        mouseSeries.Points.Clear();
        
        foreach (var stat in stats)
        {
            var date = DateTime.ParseExact(stat.Date, "yyyy-MM-dd", null);
            mouseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(date), stat.MouseDistance));
        }
        
        WeeklyMouseChart.InvalidatePlot(true);

        // Update weekly scroll chart
        var weeklyScrollSeries = (LineSeries)WeeklyScrollChart.Series[0];
        weeklyScrollSeries.Points.Clear();
        
        foreach (var stat in stats)
        {
            var date = DateTime.ParseExact(stat.Date, "yyyy-MM-dd", null);
            weeklyScrollSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(date), stat.ScrollDistance));
        }
        
        WeeklyScrollChart.InvalidatePlot(true);
    }

    private void UpdateMonthlyCharts(List<DailyStats> stats)
    {
        // Update monthly keys chart
        var keySeries = (LineSeries)MonthlyKeysChart.Series[0];
        keySeries.Points.Clear();
        
        foreach (var stat in stats)
        {
            var date = DateTime.ParseExact(stat.Date, "yyyy-MM-dd", null);
            keySeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(date), stat.KeyCount));
        }
        
        MonthlyKeysChart.InvalidatePlot(true);

        // Update monthly mouse chart
        var mouseSeries = (LineSeries)MonthlyMouseChart.Series[0];
        mouseSeries.Points.Clear();
        
        foreach (var stat in stats)
        {
            var date = DateTime.ParseExact(stat.Date, "yyyy-MM-dd", null);
            mouseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(date), stat.MouseDistance));
        }
        
        MonthlyMouseChart.InvalidatePlot(true);

        // Update monthly scroll chart
        var monthlyScrollSeries = (LineSeries)MonthlyScrollChart.Series[0];
        monthlyScrollSeries.Points.Clear();
        
        foreach (var stat in stats)
        {
            var date = DateTime.ParseExact(stat.Date, "yyyy-MM-dd", null);
            monthlyScrollSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(date), stat.ScrollDistance));
        }
        
        MonthlyScrollChart.InvalidatePlot(true);
    }

    private void UpdateHourlyCharts(List<HourlyStats> hourlyStats)
    {
        // Update keys per hour chart
        var keySeries = (LineSeries)KeysPerHourChart.Series[0];
        keySeries.Points.Clear();
        
        // Create a full 24-hour range with zero values for missing hours
        var hourlyData = new Dictionary<int, HourlyStats>();
        foreach (var stat in hourlyStats)
        {
            hourlyData[stat.Hour] = stat;
        }
        
        for (int hour = 0; hour < 24; hour++)
        {
            var hourDateTime = DateTime.Today.AddHours(hour);
            var keyCount = hourlyData.ContainsKey(hour) ? hourlyData[hour].KeyCount : 0;
            keySeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(hourDateTime), keyCount));
        }
        
        KeysPerHourChart.InvalidatePlot(true);

        // Update mouse distance per hour chart
        var mouseSeries = (LineSeries)MouseDistancePerHourChart.Series[0];
        mouseSeries.Points.Clear();
        
        for (int hour = 0; hour < 24; hour++)
        {
            var hourDateTime = DateTime.Today.AddHours(hour);
            var mouseDistance = hourlyData.ContainsKey(hour) ? hourlyData[hour].MouseDistance : 0;
            mouseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(hourDateTime), mouseDistance));
        }
        
        MouseDistancePerHourChart.InvalidatePlot(true);

        // Update scroll distance per hour chart
        var scrollSeries = (LineSeries)ScrollDistancePerHourChart.Series[0];
        scrollSeries.Points.Clear();
        
        for (int hour = 0; hour < 24; hour++)
        {
            var hourDateTime = DateTime.Today.AddHours(hour);
            var scrollDistance = hourlyData.ContainsKey(hour) ? hourlyData[hour].ScrollDistance : 0;
            scrollSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(hourDateTime), scrollDistance));
        }
        
        ScrollDistancePerHourChart.InvalidatePlot(true);
    }

    [RelayCommand]
    private async Task ResetTodayStatsAsync()
    {
        var result = MessageBox.Show(
            "Are you sure you want to reset today's statistics?",
            "Reset Statistics",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _dataLoggerService.ResetStatsAsync();
        }
    }

    [RelayCommand]
    private void ExportData()
    {
        // TODO: Implement data export functionality
        MessageBox.Show("Export functionality will be implemented in a future version.", "Export Data", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            _logger.LogInformation("Attempting to save settings...");
            
            // Update configuration with current UI values
            _configuration.TrackKeystrokes = TrackKeystrokes;
            _configuration.TrackMouseMovement = TrackMouseMovement;
            _configuration.TrackMouseClicks = TrackMouseClicks;
            _configuration.TrackScrollWheel = TrackScrollWheel;
            _configuration.ShowToolbar = ShowToolbar;
            _configuration.MinimizeToTray = MinimizeToTray;
            _configuration.StartWithWindows = StartWithWindows;
            _configuration.DistanceUnit = DistanceUnit;
            _configuration.DatabaseRetentionDays = DatabaseRetentionDays;

            _logger.LogInformation($"Configuration values before validation: DistanceUnit='{DistanceUnit}', DatabaseRetentionDays={DatabaseRetentionDays}");

            // Validate configuration with detailed errors
            var validationErrors = _configuration.GetValidationErrors();
            if (validationErrors.Any())
            {
                var errorMessage = "Settings validation failed:\n\n" + string.Join("\n", validationErrors);
                _logger.LogWarning($"Configuration validation failed: {errorMessage}");
                MessageBox.Show(errorMessage, "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save configuration to file
            _configuration.SaveToFile();
            
            _logger.LogInformation("Settings saved successfully to file");
            MessageBox.Show("Settings saved successfully!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Load lifetime statistics from database
    /// </summary>
    private async Task LoadLifetimeStatsAsync()
    {
        try
        {
            var lifetimeStats = await _databaseService.GetLifetimeStatsAsync();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                LifetimeKeyCount = FormatUtils.FormatNumber(lifetimeStats.TotalKeys);
                LifetimeMouseDistance = DistanceCalculator.FormatDistanceAutoScale(lifetimeStats.TotalMouseDistance, DistanceUnit);
                LifetimeLeftClicks = FormatUtils.FormatNumber(lifetimeStats.TotalLeftClicks);
                LifetimeRightClicks = FormatUtils.FormatNumber(lifetimeStats.TotalRightClicks);
                LifetimeMiddleClicks = FormatUtils.FormatNumber(lifetimeStats.TotalMiddleClicks);
                LifetimeTotalClicks = FormatUtils.FormatNumber(lifetimeStats.TotalClicks);
                LifetimeScrollDistance = DistanceCalculator.FormatDistanceAutoScale(lifetimeStats.TotalScrollDistance, DistanceUnit);
                TrackingPeriod = lifetimeStats.GetTrackingPeriod();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load lifetime statistics");
        }
    }

    /// <summary>
    /// Refresh lifetime statistics (can be called when tab is selected)
    /// </summary>
    [RelayCommand]
    private async Task RefreshLifetimeStatsAsync()
    {
        // Flush pending data to database first
        await _dataLoggerService.FlushAsync();
        await LoadLifetimeStatsAsync();
    }

    private void OnThemeChanged(object? sender, AppTheme newTheme)
    {
        // Reinitialize charts with new theme colors
        Application.Current.Dispatcher.Invoke(() =>
        {
            InitializeCharts();
        });
    }

    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        _chartUpdateTimer?.Dispose();
        
        if (_dataLoggerService != null)
        {
            _dataLoggerService.StatsUpdated -= OnStatsUpdated;
            _dataLoggerService.LastKeyChanged -= OnLastKeyChanged;
        }
        
        if (_themeManager != null)
        {
            _themeManager.ThemeChanged -= OnThemeChanged;
        }
    }
}