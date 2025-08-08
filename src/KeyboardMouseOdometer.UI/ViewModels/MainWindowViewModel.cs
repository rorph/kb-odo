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
    
    // Data grid collections
    [ObservableProperty]
    private ObservableCollection<DailyStatsSummary> dailyStats = new();
    
    [ObservableProperty]
    private ObservableCollection<WeeklyStatsSummary> weeklyStats = new();
    
    [ObservableProperty]
    private ObservableCollection<MonthlyStatsSummary> monthlyStats = new();
    
    // Visualization collections (separate from grid data to prevent sorting issues)
    [ObservableProperty]
    private ObservableCollection<DailyStatsSummary> weeklyVisualizationData = new();
    
    [ObservableProperty]
    private ObservableCollection<DailyStatsSummary> monthlyVisualizationData = new();
    
    [ObservableProperty]
    private WeeklyStatsSummary? selectedWeeklyItem;
    
    [ObservableProperty]
    private MonthlyStatsSummary? selectedMonthlyItem;

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

    private string heatmapColorScheme = "Classic";
    
    public string HeatmapColorScheme
    {
        get => heatmapColorScheme;
        set
        {
            if (SetProperty(ref heatmapColorScheme, value))
            {
                _logger.LogInformation("Heatmap color scheme changed from {OldScheme} to {NewScheme}", heatmapColorScheme, value);
                
                // Update the HeatmapViewModel when the color scheme changes
                if (HeatmapViewModel != null)
                {
                    HeatmapViewModel.ColorScheme = value;
                    _logger.LogDebug("Updated HeatmapViewModel with new color scheme: {ColorScheme}", value);
                }
            }
        }
    }

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

    [ObservableProperty]
    private string applicationVersion = "";

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

        // Set application version
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        ApplicationVersion = $"v{version?.Major}.{version?.Minor}.{version?.Build}";

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
        HeatmapColorScheme = _configuration.HeatmapColorScheme;
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
            MarkerFill = lineColor,
            TrackerFormatString = "{0}\n{1:HH:mm}: {2:0.##} " + yAxisTitle
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
            MarkerFill = lineColor,
            TrackerFormatString = "{0}\n{1:MM/dd}: {2:0.##} " + yAxisTitle
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
            
            // Update weekly data grid and charts
            await UpdateWeeklyDataAsync();
            
            // Update monthly data grid and charts
            await UpdateMonthlyDataAsync();
            
            // Update daily data grid
            await UpdateDailyDataAsync();
            
            // Update weekly charts (legacy - keeping for backward compatibility)
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
            _configuration.HeatmapColorScheme = HeatmapColorScheme;
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
    /// Update weekly data grid and visualization
    /// </summary>
    private async Task UpdateWeeklyDataAsync()
    {
        try
        {
            var today = DateTime.Today;
            
            // Get last 8 weeks of data for weekly aggregation
            var startDate = today.AddDays(-(int)today.DayOfWeek).AddDays(-7 * 7); // 8 weeks back
            var endDate = today;
            
            var allData = await _databaseService.GetDailyStatsRangeAsync(startDate, endDate);
            
            // For current week visualization (daily data)
            var currentWeekStart = today.AddDays(-(int)today.DayOfWeek);
            var currentWeekEnd = currentWeekStart.AddDays(6);
            var currentWeekData = allData.Where(s => 
            {
                var date = DateTime.Parse(s.Date).Date;
                return date >= currentWeekStart && date <= currentWeekEnd;
            }).ToList();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                WeeklyStats.Clear();
                WeeklyVisualizationData.Clear();
                
                // Create weekly aggregated data for the table
                var weeklyGroups = allData
                    .Select(s => new { Data = s, Date = DateTime.Parse(s.Date).Date })
                    .GroupBy(x => new { Year = x.Date.Year, Week = GetWeekOfYear(x.Date) })
                    .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Week)
                    .Take(8) // Show last 8 weeks
                    .ToList();

                // Find max values for normalization across all weeks
                var allWeeklyTotals = weeklyGroups.Select(g => new
                {
                    KeyCount = g.Sum(x => x.Data.KeyCount),
                    MouseDistance = g.Sum(x => x.Data.MouseDistance),
                    ScrollDistance = g.Sum(x => x.Data.ScrollDistance)
                }).ToList();
                
                var maxKeys = allWeeklyTotals.Any() ? allWeeklyTotals.Max(w => w.KeyCount) : 1;
                var maxMouse = allWeeklyTotals.Any() ? allWeeklyTotals.Max(w => w.MouseDistance) : 1;
                var maxScroll = allWeeklyTotals.Any() ? allWeeklyTotals.Max(w => w.ScrollDistance) : 1;

                foreach (var weekGroup in weeklyGroups)
                {
                    var firstDayOfWeek = weekGroup.Min(x => x.Date);
                    // Adjust to get the start of the week (Sunday)
                    var weekStart = firstDayOfWeek.AddDays(-(int)firstDayOfWeek.DayOfWeek);
                    
                    var totalKeys = weekGroup.Sum(x => x.Data.KeyCount);
                    var totalMouseDistance = weekGroup.Sum(x => x.Data.MouseDistance);
                    var totalScrollDistance = weekGroup.Sum(x => x.Data.ScrollDistance);
                    var totalClicks = weekGroup.Sum(x => x.Data.LeftClicks + x.Data.RightClicks + x.Data.MiddleClicks);
                    
                    var weekSummary = new WeeklyStatsSummary
                    {
                        WeekStart = weekStart,
                        KeyCount = totalKeys,
                        MouseDistance = totalMouseDistance,
                        MouseDistanceDisplay = DistanceCalculator.FormatDistanceAutoScale(totalMouseDistance, DistanceUnit),
                        ScrollDistance = totalScrollDistance,
                        ScrollDistanceDisplay = DistanceCalculator.FormatDistanceAutoScale(totalScrollDistance, DistanceUnit),
                        TotalClicks = totalClicks,
                        KeyCountNormalized = maxKeys > 0 ? totalKeys / (double)maxKeys : 0,
                        MouseDistanceNormalized = maxMouse > 0 ? totalMouseDistance / maxMouse : 0,
                        ScrollDistanceNormalized = maxScroll > 0 ? totalScrollDistance / maxScroll : 0
                    };
                    
                    WeeklyStats.Add(weekSummary);
                }

                // Create daily visualization data for current week bar chart
                var maxDailyKeys = currentWeekData.Any() ? currentWeekData.Max(s => s.KeyCount) : 1;
                var maxDailyMouse = currentWeekData.Any() ? currentWeekData.Max(s => s.MouseDistance) : 1;
                var maxDailyScroll = currentWeekData.Any() ? currentWeekData.Max(s => s.ScrollDistance) : 1;
                
                for (var date = currentWeekStart; date <= currentWeekEnd; date = date.AddDays(1))
                {
                    var dayStats = currentWeekData.FirstOrDefault(s => DateTime.Parse(s.Date).Date == date.Date);
                    
                    var summary = new DailyStatsSummary
                    {
                        Date = date,
                        KeyCount = dayStats?.KeyCount ?? 0,
                        MouseDistance = dayStats?.MouseDistance ?? 0,
                        MouseDistanceDisplay = DistanceCalculator.FormatDistanceAutoScale(dayStats?.MouseDistance ?? 0, DistanceUnit),
                        ScrollDistance = dayStats?.ScrollDistance ?? 0,
                        ScrollDistanceDisplay = DistanceCalculator.FormatDistanceAutoScale(dayStats?.ScrollDistance ?? 0, DistanceUnit),
                        TotalClicks = (dayStats?.LeftClicks ?? 0) + (dayStats?.RightClicks ?? 0) + (dayStats?.MiddleClicks ?? 0),
                        KeyCountNormalized = maxDailyKeys > 0 ? (dayStats?.KeyCount ?? 0) / (double)maxDailyKeys : 0,
                        MouseDistanceNormalized = maxDailyMouse > 0 ? (dayStats?.MouseDistance ?? 0) / maxDailyMouse : 0,
                        ScrollDistanceNormalized = maxDailyScroll > 0 ? (dayStats?.ScrollDistance ?? 0) / maxDailyScroll : 0
                    };
                    
                    WeeklyVisualizationData.Add(summary);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update weekly data");
        }
    }

    private int GetWeekOfYear(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var calendar = culture.Calendar;
        var calendarWeekRule = culture.DateTimeFormat.CalendarWeekRule;
        var firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;
        return calendar.GetWeekOfYear(date, calendarWeekRule, firstDayOfWeek);
    }
    
    /// <summary>
    /// Update monthly data grid and visualization
    /// </summary>
    private async Task UpdateMonthlyDataAsync()
    {
        try
        {
            var today = DateTime.Today;
            
            // Get last 12 months of data for monthly aggregation
            var startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-11); // 12 months back
            var endDate = today;
            
            var allData = await _databaseService.GetDailyStatsRangeAsync(startDate, endDate);
            
            // For current month visualization (daily data)
            var currentMonthStart = new DateTime(today.Year, today.Month, 1);
            var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
            var currentMonthData = allData.Where(s => 
            {
                var date = DateTime.Parse(s.Date).Date;
                return date >= currentMonthStart && date <= currentMonthEnd;
            }).ToList();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                MonthlyStats.Clear();
                MonthlyVisualizationData.Clear();
                
                // Create monthly aggregated data for the table
                var monthlyGroups = allData
                    .Select(s => new { Data = s, Date = DateTime.Parse(s.Date).Date })
                    .GroupBy(x => new { x.Date.Year, x.Date.Month })
                    .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
                    .Take(12) // Show last 12 months
                    .ToList();

                // Find max values for normalization across all months
                var allMonthlyTotals = monthlyGroups.Select(g => new
                {
                    KeyCount = g.Sum(x => x.Data.KeyCount),
                    MouseDistance = g.Sum(x => x.Data.MouseDistance),
                    ScrollDistance = g.Sum(x => x.Data.ScrollDistance)
                }).ToList();
                
                var maxKeys = allMonthlyTotals.Any() ? allMonthlyTotals.Max(m => m.KeyCount) : 1;
                var maxMouse = allMonthlyTotals.Any() ? allMonthlyTotals.Max(m => m.MouseDistance) : 1;
                var maxScroll = allMonthlyTotals.Any() ? allMonthlyTotals.Max(m => m.ScrollDistance) : 1;

                foreach (var monthGroup in monthlyGroups)
                {
                    var monthStart = new DateTime(monthGroup.Key.Year, monthGroup.Key.Month, 1);
                    
                    var totalKeys = monthGroup.Sum(x => x.Data.KeyCount);
                    var totalMouseDistance = monthGroup.Sum(x => x.Data.MouseDistance);
                    var totalScrollDistance = monthGroup.Sum(x => x.Data.ScrollDistance);
                    var totalClicks = monthGroup.Sum(x => x.Data.LeftClicks + x.Data.RightClicks + x.Data.MiddleClicks);
                    
                    var monthSummary = new MonthlyStatsSummary
                    {
                        MonthStart = monthStart,
                        KeyCount = totalKeys,
                        MouseDistance = totalMouseDistance,
                        MouseDistanceDisplay = DistanceCalculator.FormatDistanceAutoScale(totalMouseDistance, DistanceUnit),
                        ScrollDistance = totalScrollDistance,
                        ScrollDistanceDisplay = DistanceCalculator.FormatDistanceAutoScale(totalScrollDistance, DistanceUnit),
                        TotalClicks = totalClicks,
                        KeyCountNormalized = maxKeys > 0 ? totalKeys / (double)maxKeys : 0,
                        MouseDistanceNormalized = maxMouse > 0 ? totalMouseDistance / maxMouse : 0,
                        ScrollDistanceNormalized = maxScroll > 0 ? totalScrollDistance / maxScroll : 0
                    };
                    
                    MonthlyStats.Add(monthSummary);
                }

                // Create daily visualization data for current month bar chart
                var maxDailyKeys = currentMonthData.Any() ? currentMonthData.Max(s => s.KeyCount) : 1;
                var maxDailyMouse = currentMonthData.Any() ? currentMonthData.Max(s => s.MouseDistance) : 1;
                var maxDailyScroll = currentMonthData.Any() ? currentMonthData.Max(s => s.ScrollDistance) : 1;
                
                for (var date = currentMonthStart; date <= currentMonthEnd && date <= today; date = date.AddDays(1))
                {
                    var dayStats = currentMonthData.FirstOrDefault(s => DateTime.Parse(s.Date).Date == date.Date);
                    
                    var summary = new DailyStatsSummary
                    {
                        Date = date,
                        KeyCount = dayStats?.KeyCount ?? 0,
                        MouseDistance = dayStats?.MouseDistance ?? 0,
                        MouseDistanceDisplay = DistanceCalculator.FormatDistanceAutoScale(dayStats?.MouseDistance ?? 0, DistanceUnit),
                        ScrollDistance = dayStats?.ScrollDistance ?? 0,
                        ScrollDistanceDisplay = DistanceCalculator.FormatDistanceAutoScale(dayStats?.ScrollDistance ?? 0, DistanceUnit),
                        TotalClicks = (dayStats?.LeftClicks ?? 0) + (dayStats?.RightClicks ?? 0) + (dayStats?.MiddleClicks ?? 0),
                        KeyCountNormalized = maxDailyKeys > 0 ? (dayStats?.KeyCount ?? 0) / (double)maxDailyKeys : 0,
                        MouseDistanceNormalized = maxDailyMouse > 0 ? (dayStats?.MouseDistance ?? 0) / maxDailyMouse : 0,
                        ScrollDistanceNormalized = maxDailyScroll > 0 ? (dayStats?.ScrollDistance ?? 0) / maxDailyScroll : 0
                    };
                    
                    MonthlyVisualizationData.Add(summary);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update monthly data");
        }
    }
    
    /// <summary>
    /// Update daily data grid with last 30 days
    /// </summary>
    private async Task UpdateDailyDataAsync()
    {
        try
        {
            var today = DateTime.Today;
            
            // Get last 30 days of data
            var startDate = today.AddDays(-29); // 30 days including today
            var endDate = today;
            
            var dailyData = await _databaseService.GetDailyStatsRangeAsync(startDate, endDate);
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                DailyStats.Clear();
                
                // Create data for each day, including days with no activity
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var dayStats = dailyData.FirstOrDefault(s => DateTime.Parse(s.Date).Date == date.Date);
                    
                    var summary = new DailyStatsSummary
                    {
                        Date = date,
                        KeyCount = dayStats?.KeyCount ?? 0,
                        MouseDistance = dayStats?.MouseDistance ?? 0,
                        MouseDistanceDisplay = DistanceCalculator.FormatDistanceAutoScale(dayStats?.MouseDistance ?? 0, DistanceUnit),
                        ScrollDistance = dayStats?.ScrollDistance ?? 0,
                        ScrollDistanceDisplay = DistanceCalculator.FormatDistanceAutoScale(dayStats?.ScrollDistance ?? 0, DistanceUnit),
                        TotalClicks = (dayStats?.LeftClicks ?? 0) + (dayStats?.RightClicks ?? 0) + (dayStats?.MiddleClicks ?? 0)
                    };
                    
                    DailyStats.Add(summary);
                }
                
                // Reverse the order so most recent day is first
                var reversedStats = DailyStats.Reverse().ToList();
                DailyStats.Clear();
                foreach (var stat in reversedStats)
                {
                    DailyStats.Add(stat);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update daily data");
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