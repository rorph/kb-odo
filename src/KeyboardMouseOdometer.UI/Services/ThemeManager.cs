using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;

namespace KeyboardMouseOdometer.UI.Services
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public class ThemeManager
    {
        private readonly ILogger<ThemeManager> _logger;
        private readonly Application _application;
        private AppTheme _currentTheme;
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";

        public event EventHandler<AppTheme>? ThemeChanged;

        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    ThemeChanged?.Invoke(this, value);
                }
            }
        }

        public ThemeManager(ILogger<ThemeManager> logger)
        {
            _logger = logger;
            _application = Application.Current;
            
            // Detect initial theme
            _currentTheme = DetectWindowsTheme();
            
            // Monitor for theme changes
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        /// <summary>
        /// Detect the current Windows theme from registry
        /// </summary>
        public AppTheme DetectWindowsTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
                if (key != null)
                {
                    var value = key.GetValue(RegistryValueName);
                    if (value is int intValue)
                    {
                        var theme = intValue == 0 ? AppTheme.Dark : AppTheme.Light;
                        _logger.LogInformation("Detected Windows theme: {Theme}", theme);
                        return theme;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to detect Windows theme from registry");
            }

            // Default to Light theme if detection fails
            _logger.LogWarning("Failed to detect theme, defaulting to Light");
            return AppTheme.Light;
        }

        /// <summary>
        /// Apply the specified theme to the application
        /// </summary>
        public void ApplyTheme(AppTheme theme)
        {
            try
            {
                _logger.LogInformation("Applying {Theme} theme", theme);

                // Remove existing theme dictionaries
                var existingTheme = _application.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme.xaml") == true);
                
                if (existingTheme != null)
                {
                    _application.Resources.MergedDictionaries.Remove(existingTheme);
                }

                // Add new theme dictionary
                var themeUri = new Uri($"pack://application:,,,/Themes/{theme}Theme.xaml", UriKind.Absolute);
                var themeDict = new ResourceDictionary { Source = themeUri };
                _application.Resources.MergedDictionaries.Insert(0, themeDict);

                CurrentTheme = theme;
                _logger.LogInformation("{Theme} theme applied successfully", theme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply {Theme} theme", theme);
            }
        }

        /// <summary>
        /// Initialize and apply the current Windows theme
        /// </summary>
        public void Initialize()
        {
            var theme = DetectWindowsTheme();
            ApplyTheme(theme);
        }

        /// <summary>
        /// Handle Windows theme changes
        /// </summary>
        private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General || 
                e.Category == UserPreferenceCategory.VisualStyle ||
                e.Category == UserPreferenceCategory.Color)
            {
                var newTheme = DetectWindowsTheme();
                if (newTheme != CurrentTheme)
                {
                    _logger.LogInformation("Windows theme changed to {Theme}", newTheme);
                    _application.Dispatcher.Invoke(() => ApplyTheme(newTheme));
                }
            }
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        }
    }
}