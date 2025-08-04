using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Services;
using KeyboardMouseOdometer.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace KeyboardMouseOdometer.UI.ViewModels
{
    public enum HeatmapTimeRange
    {
        Today,
        Week,
        Month,
        Lifetime
    }

    public class HeatmapViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<HeatmapViewModel> _logger;
        private readonly DatabaseService _databaseService;
        private readonly DataLoggerService _dataLoggerService;
        private readonly IKeyCodeMapper _keyCodeMapper;
        private readonly DispatcherTimer _refreshTimer;
        
        private List<KeyboardKey> _keyboardLayout;
        private HeatmapTimeRange _selectedTimeRange = HeatmapTimeRange.Today;
        private long _totalKeyPresses;
        private string _mostUsedKey = "-";
        private long _mostUsedKeyCount;
        private double _typingSpeed;
        private bool _isLoading;

        public List<KeyboardKey> KeyboardLayout
        {
            get => _keyboardLayout;
            set
            {
                _keyboardLayout = value;
                OnPropertyChanged();
            }
        }

        public HeatmapTimeRange SelectedTimeRange
        {
            get => _selectedTimeRange;
            set
            {
                _selectedTimeRange = value;
                OnPropertyChanged();
                _ = LoadHeatmapDataAsync();
            }
        }

        public long TotalKeyPresses
        {
            get => _totalKeyPresses;
            set
            {
                _totalKeyPresses = value;
                OnPropertyChanged();
            }
        }

        public string MostUsedKey
        {
            get => _mostUsedKey;
            set
            {
                _mostUsedKey = value;
                OnPropertyChanged();
            }
        }

        public long MostUsedKeyCount
        {
            get => _mostUsedKeyCount;
            set
            {
                _mostUsedKeyCount = value;
                OnPropertyChanged();
            }
        }

        public double TypingSpeed
        {
            get => _typingSpeed;
            set
            {
                _typingSpeed = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ChangeTimeRangeCommand { get; }

        public HeatmapViewModel(ILogger<HeatmapViewModel> logger, DatabaseService databaseService, DataLoggerService dataLoggerService, IKeyCodeMapper keyCodeMapper)
        {
            _logger = logger;
            _databaseService = databaseService;
            _dataLoggerService = dataLoggerService;
            _keyCodeMapper = keyCodeMapper;
            
            // Initialize keyboard layout
            _keyboardLayout = Core.Models.KeyboardLayout.GetUSQwertyLayout();
            Core.Models.KeyboardLayout.PopulateKeyNames(_keyboardLayout, _keyCodeMapper);
            
            // Set up refresh timer for real-time updates
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _refreshTimer.Tick += async (s, e) => await RefreshHeatmapAsync();
            _refreshTimer.Start();
            
            // Commands
            RefreshCommand = new RelayCommand(async () => await RefreshHeatmapAsync());
            ChangeTimeRangeCommand = new RelayCommand<HeatmapTimeRange>(range => SelectedTimeRange = range);
            
            // Initial load
            _ = LoadHeatmapDataAsync();
        }

        private async Task RefreshHeatmapAsync()
        {
            // Flush pending data to database first
            await _dataLoggerService.FlushAsync();
            // Then load the updated data
            await LoadHeatmapDataAsync();
        }

        private async Task LoadHeatmapDataAsync()
        {
            if (IsLoading) return;
            
            try
            {
                IsLoading = true;
                
                Dictionary<string, long> keyStats = SelectedTimeRange switch
                {
                    HeatmapTimeRange.Today => await _databaseService.GetTodayKeyStatsAsync(),
                    HeatmapTimeRange.Week => await _databaseService.GetWeeklyKeyStatsAsync(),
                    HeatmapTimeRange.Month => await _databaseService.GetMonthlyKeyStatsAsync(),
                    HeatmapTimeRange.Lifetime => await _databaseService.GetLifetimeKeyStatsAsync(),
                    _ => new Dictionary<string, long>()
                };

                if (keyStats != null && keyStats.Any())
                {
                    // Debug logging for numpad keys
                    var numpadKeys = keyStats.Where(k => k.Key.StartsWith("Num") || k.Key.Contains("Pad"));
                    foreach (var npKey in numpadKeys)
                    {
                        _logger.LogDebug("Heatmap loading numpad key: {Key} = {Count}", npKey.Key, npKey.Value);
                        System.Console.WriteLine($"[HEATMAP DEBUG] Database key: {npKey.Key} = {npKey.Value}");
                    }
                    
                    // Also log all keys starting with Num for debugging
                    var allNumKeys = keyStats.Where(k => k.Key.StartsWith("Num"));
                    System.Console.WriteLine($"[HEATMAP DEBUG] Total keys starting with 'Num': {allNumKeys.Count()}");
                    foreach (var numKey in allNumKeys)
                    {
                        System.Console.WriteLine($"[HEATMAP DEBUG] Key: '{numKey.Key}' = {numKey.Value}");
                    }
                    
                    // Update keyboard layout with heat data
                    var layoutCopy = Core.Models.KeyboardLayout.GetUSQwertyLayout();
                    Core.Models.KeyboardLayout.PopulateKeyNames(layoutCopy, _keyCodeMapper);
                    KeyboardLayout = StatisticsService.CalculateHeatmapData(keyStats, layoutCopy);
                    
                    // Update statistics
                    TotalKeyPresses = keyStats.Values.Sum();
                    
                    var topKey = keyStats.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
                    MostUsedKey = topKey.Key ?? "-";
                    MostUsedKeyCount = topKey.Value;
                    
                    // Calculate typing speed based on time range
                    var duration = GetTimeRangeDuration();
                    TypingSpeed = StatisticsService.CalculateTypingSpeed(TotalKeyPresses, duration);
                }
                else
                {
                    // No data - reset display
                    KeyboardLayout = Core.Models.KeyboardLayout.GetUSQwertyLayout();
                    TotalKeyPresses = 0;
                    MostUsedKey = "-";
                    MostUsedKeyCount = 0;
                    TypingSpeed = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load heatmap data");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private TimeSpan GetTimeRangeDuration()
        {
            return SelectedTimeRange switch
            {
                HeatmapTimeRange.Today => DateTime.Now - DateTime.Today,
                HeatmapTimeRange.Week => TimeSpan.FromDays(7),
                HeatmapTimeRange.Month => TimeSpan.FromDays(30),
                HeatmapTimeRange.Lifetime => TimeSpan.FromDays(365), // Approximate
                _ => TimeSpan.FromDays(1)
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;
        public void Execute(object parameter) => _execute((T)parameter);
    }
}