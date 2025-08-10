using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Services;
using KeyboardMouseOdometer.UI.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace KeyboardMouseOdometer.UI.ViewModels;

/// <summary>
/// ViewModel for Application Usage tab
/// </summary>
public partial class AppUsageViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly ThemeManager _themeManager;
    
    [ObservableProperty]
    private ObservableCollection<AppUsageStats> _todayApps = new();
    
    [ObservableProperty]
    private ObservableCollection<AppUsageStats> _weeklyApps = new();
    
    [ObservableProperty]
    private ObservableCollection<AppUsageStats> _monthlyApps = new();
    
    [ObservableProperty]
    private ObservableCollection<AppUsageStats> _lifetimeApps = new();
    
    [ObservableProperty]
    private PlotModel? _appUsageChart;
    
    [ObservableProperty]
    private string _selectedTimeRange = "Today";
    
    [ObservableProperty]
    private bool _isLoading;
    
    public ICommand RefreshCommand { get; }
    public ICommand TimeRangeChangedCommand { get; }
    public ICommand ChangeTimeRangeCommand { get; }
    
    public AppUsageViewModel(DatabaseService databaseService, ThemeManager themeManager)
    {
        _databaseService = databaseService;
        _themeManager = themeManager;
        
        RefreshCommand = new AsyncRelayCommand(RefreshDataAsync);
        TimeRangeChangedCommand = new AsyncRelayCommand<string>(OnTimeRangeChangedAsync);
        ChangeTimeRangeCommand = new AsyncRelayCommand<string>(OnTimeRangeChangedAsync);
        
        // Subscribe to theme changes
        _themeManager.ThemeChanged += OnThemeChanged;
        
        // Initialize chart
        CreateChart();
    }
    
    private void OnThemeChanged(object? sender, AppTheme e)
    {
        // Recreate chart with new theme colors
        CreateChart();
        _ = RefreshDataAsync();
    }
    
    /// <summary>
    /// Initialize the view model and load data
    /// </summary>
    public async Task InitializeAsync()
    {
        await RefreshDataAsync();
    }
    
    /// <summary>
    /// Refresh all application usage data
    /// </summary>
    private async Task RefreshDataAsync()
    {
        IsLoading = true;
        
        try
        {
            // Load data based on selected time range
            switch (SelectedTimeRange)
            {
                case "Today":
                    var todayData = await _databaseService.GetTodayAppUsageAsync();
                    TodayApps = new ObservableCollection<AppUsageStats>(todayData.Take(20)); // Top 20
                    UpdateChart(todayData.Take(10)); // Top 10 for chart
                    break;
                    
                case "Weekly":
                    var weeklyData = await _databaseService.GetWeeklyAppUsageAsync();
                    WeeklyApps = new ObservableCollection<AppUsageStats>(weeklyData.Take(20));
                    UpdateChart(weeklyData.Take(10));
                    break;
                    
                case "Monthly":
                    var monthlyData = await _databaseService.GetMonthlyAppUsageAsync();
                    MonthlyApps = new ObservableCollection<AppUsageStats>(monthlyData.Take(20));
                    UpdateChart(monthlyData.Take(10));
                    break;
                    
                case "Lifetime":
                    var lifetimeData = await _databaseService.GetLifetimeAppUsageAsync();
                    LifetimeApps = new ObservableCollection<AppUsageStats>(lifetimeData.Take(20));
                    UpdateChart(lifetimeData.Take(10));
                    break;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Handle time range selection change
    /// </summary>
    private async Task OnTimeRangeChangedAsync(string? timeRange)
    {
        if (string.IsNullOrEmpty(timeRange))
            return;
        
        SelectedTimeRange = timeRange;
        await RefreshDataAsync();
    }
    
    /// <summary>
    /// Create the initial chart
    /// </summary>
    private void CreateChart()
    {
        var isDarkTheme = _themeManager.CurrentTheme == AppTheme.Dark;
        var textColor = isDarkTheme ? OxyColor.FromRgb(204, 204, 204) : OxyColor.FromRgb(51, 51, 51);
        var gridLineColor = isDarkTheme ? OxyColor.FromRgb(63, 63, 63) : OxyColor.FromRgb(224, 224, 224);
        
        var model = new PlotModel
        {
            Title = "Top Applications by Usage Time",
            Background = OxyColors.Transparent,
            PlotAreaBackground = OxyColors.Transparent,
            TextColor = textColor,
            TitleFontSize = 14,
            TitleFontWeight = 700
        };
        
        // Category axis for app names
        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Left,
            TextColor = textColor,
            TicklineColor = gridLineColor,
            TitleColor = textColor
        };
        model.Axes.Add(categoryAxis);
        
        // Value axis for time
        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time (minutes)",
            MinimumPadding = 0,
            MaximumPadding = 0.1,
            AbsoluteMinimum = 0,
            TextColor = textColor,
            TicklineColor = gridLineColor,
            TitleColor = textColor,
            MajorGridlineColor = gridLineColor,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineColor = gridLineColor,
            MinorGridlineStyle = LineStyle.Dot
        };
        model.Axes.Add(valueAxis);
        
        AppUsageChart = model;
    }
    
    /// <summary>
    /// Update chart with new data
    /// </summary>
    private void UpdateChart(IEnumerable<AppUsageStats> apps)
    {
        if (AppUsageChart == null)
        {
            CreateChart();
            return;
        }
        
        var isDarkTheme = _themeManager.CurrentTheme == AppTheme.Dark;
        var textColor = isDarkTheme ? OxyColor.FromRgb(204, 204, 204) : OxyColor.FromRgb(51, 51, 51);
        var gridLineColor = isDarkTheme ? OxyColor.FromRgb(63, 63, 63) : OxyColor.FromRgb(224, 224, 224);
        
        var model = AppUsageChart;
        model.TextColor = textColor;
        
        // Update axes colors
        foreach (var axis in model.Axes)
        {
            axis.TextColor = textColor;
            axis.TicklineColor = gridLineColor;
            axis.TitleColor = textColor;
            if (axis is LinearAxis linearAxis)
            {
                linearAxis.MajorGridlineColor = gridLineColor;
                linearAxis.MinorGridlineColor = gridLineColor;
            }
        }
        
        model.Series.Clear();
        
        // Update category axis
        var categoryAxis = model.Axes.OfType<CategoryAxis>().FirstOrDefault();
        if (categoryAxis != null)
        {
            categoryAxis.Labels.Clear();
            foreach (var app in apps.Reverse()) // Reverse for correct display order
            {
                categoryAxis.Labels.Add(TruncateAppName(app.AppName, 20));
            }
        }
        
        // Create bar series
        var barSeries = new BarSeries
        {
            FillColor = OxyColor.FromRgb(33, 150, 243), // Material Blue
            StrokeColor = OxyColors.DarkBlue,
            StrokeThickness = 0.5,
            BarWidth = 1
        };
        
        foreach (var app in apps.Reverse())
        {
            barSeries.Items.Add(new BarItem(app.SecondsUsed / 60.0)); // Convert to minutes
        }
        
        model.Series.Add(barSeries);
        model.InvalidatePlot(true);
    }
    
    /// <summary>
    /// Truncate app name for display
    /// </summary>
    private string TruncateAppName(string appName, int maxLength)
    {
        if (appName.Length <= maxLength)
            return appName;
        
        return appName.Substring(0, maxLength - 3) + "...";
    }
}