using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using KeyboardMouseOdometer.Core.Services;
using KeyboardMouseOdometer.Core.Models;
using KeyboardMouseOdometer.Core.Interfaces;
using KeyboardMouseOdometer.UI.ViewModels;
using KeyboardMouseOdometer.UI.Views;
using KeyboardMouseOdometer.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Hardcodet.Wpf.TaskbarNotification;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Serilog.Events;

namespace KeyboardMouseOdometer.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private TaskbarIcon? _taskbarIcon;
    private MainWindow? _mainWindow;
    private ToolbarWindow? _toolbarWindow;
    private GlobalHookService? _hookService;
    private DataLoggerService? _dataLoggerService;
    private ThemeManager? _themeManager;
    private ILogger<App>? _logger;
    private readonly StringBuilder _startupLog = new();
    private bool _isDiagnosticMode = false;
    private bool _isTestMode = false;
    
    // Menu item references for dynamic updates
    private System.Windows.Controls.MenuItem? _openDashboardMenuItem;
    private System.Windows.Controls.MenuItem? _showToolbarMenuItem;
    
    // Windows Event Log API
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern IntPtr RegisterEventSource(string? lpUNCServerName, string lpSourceName);
    
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool ReportEvent(
        IntPtr hEventLog,
        ushort wType,
        ushort wCategory,
        uint dwEventID,
        IntPtr lpUserSid,
        ushort wNumStrings,
        uint dwDataSize,
        string[] lpStrings,
        IntPtr lpRawData);
    
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DeregisterEventSource(IntPtr hEventSource);
    
    private const ushort EVENTLOG_ERROR_TYPE = 1;
    private const ushort EVENTLOG_WARNING_TYPE = 2;
    private const ushort EVENTLOG_INFORMATION_TYPE = 4;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Set up global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        
        // Check for command line arguments
        _isDiagnosticMode = e.Args.Contains("--diagnostic") || e.Args.Contains("-d");
        _isTestMode = e.Args.Contains("--test") || e.Args.Contains("-t");
        
        base.OnStartup(e);
        
        LogStartupStep("Application startup initiated");
        
        if (_isTestMode)
        {
            ShowTestModeDialog();
            return;
        }

        try
        {
            LogStartupStep("Building dependency injection host");
            // Build the host for dependency injection with detailed error handling
            _host = CreateHostBuilder().Build();
            _logger = _host.Services.GetService<ILogger<App>>();
            LogStartupStep("Host built successfully");

            LogStartupStep("Initializing core services");
            // Initialize services with error isolation
            await InitializeServicesAsync();
            LogStartupStep("Core services initialized");

            LogStartupStep("Setting up system tray icon");
            // Setup system tray icon with error handling
            await SetupSystemTrayAsync();
            LogStartupStep("System tray setup complete");

            LogStartupStep("Initializing main window");
            // Initialize main window but don't show it
            _mainWindow = _host.Services.GetRequiredService<MainWindow>();
            
            // Set up event handlers for window state changes
            if (_mainWindow != null)
            {
                _mainWindow.IsVisibleChanged += MainWindow_IsVisibleChanged;
                _mainWindow.StateChanged += MainWindow_StateChanged;
            }
            
            LogStartupStep("Main window initialized");

            LogStartupStep("Checking toolbar configuration");
            // Show toolbar if enabled
            var configuration = _host.Services.GetRequiredService<Configuration>();
            if (configuration.ShowToolbar)
            {
                ShowToolbar();
                LogStartupStep("Toolbar shown");
                UpdateToolbarMenuText(true);
            }
            else
            {
                UpdateToolbarMenuText(false);
            }

            // Delay hook initialization to ensure WPF is fully loaded
            LogStartupStep("Scheduling hook initialization");
            _ = Dispatcher.BeginInvoke(async () => await InitializeHooksAsync(), System.Windows.Threading.DispatcherPriority.Loaded);

            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            LogStartupStep("Application startup completed successfully");
            
            if (_isDiagnosticMode)
            {
                ShowDiagnosticInfo();
            }
        }
        catch (Exception ex)
        {
            await HandleStartupError(ex);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            LogStartupStep("Application shutdown started");
            
            // Save configuration before exiting
            try
            {
                var config = _host?.Services.GetService<Core.Models.Configuration>();
                if (config != null)
                {
                    config.SaveToFile();
                    LogStartupStep("Configuration saved successfully on exit");
                }
                else
                {
                    LogStartupStep("Warning: Configuration service not available during exit");
                }
            }
            catch (Exception ex)
            {
                LogStartupStep($"Failed to save configuration on exit: {ex.Message}");
                WriteToEventLog($"Failed to save configuration on exit: {ex}", EVENTLOG_WARNING_TYPE);
            }

            // Cleanup
            _hookService?.StopHooks();
            _hookService?.Dispose();
            _taskbarIcon?.Dispose();
            _toolbarWindow?.Close();
            _mainWindow?.Close();

            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }

            LogStartupStep("Application shutdown completed");
        }
        catch (Exception ex)
        {
            LogStartupStep($"Error during shutdown: {ex.Message}");
        }

        base.OnExit(e);
    }

    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Configuration - Load from file or create default
                var config = Core.Models.Configuration.LoadFromFile();
                services.AddSingleton(config);

                // Core services
                services.AddSingleton<DatabaseService>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<DatabaseService>>();
                    var databasePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "KeyboardMouseOdometer",
                        "odometer.db");
                    return new DatabaseService(logger, databasePath);
                });
                services.AddSingleton<InputMonitoringService>();
                services.AddSingleton<DataLoggerService>();

                // Key code mapping
                services.AddSingleton<IKeyCodeMapper, WpfKeyCodeMapper>();

                // UI services
                services.AddSingleton<GlobalHookService>();
                services.AddSingleton<ThemeManager>();

                // ViewModels
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<ToolbarViewModel>();
                services.AddTransient<HeatmapViewModel>();

                // Views
                services.AddTransient<MainWindow>();
                services.AddTransient<ToolbarWindow>();

                // Logging configuration with Serilog
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KeyboardMouseOdometer",
                    "logs");
                Directory.CreateDirectory(logPath);
                
                var logFile = Path.Combine(logPath, $"app_{DateTime.Now:yyyyMMdd}.log");
                
                // Configure Serilog
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(
                        logFile,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                        buffered: false,  // Write immediately for crash debugging
                        flushToDiskInterval: TimeSpan.FromSeconds(1))
                    .CreateLogger();
                
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog(Log.Logger, dispose: true);
                });
            });
    }

    private async Task InitializeServicesAsync()
    {
        if (_host == null) 
        {
            throw new InvalidOperationException("Host is null during service initialization");
        }

        try
        {
            LogStartupStep("Initializing database service");
            // Initialize database with detailed error handling
            var databaseService = _host.Services.GetRequiredService<DatabaseService>();
            await databaseService.InitializeAsync();
            LogStartupStep("Database service initialized");

            LogStartupStep("Initializing data logger service");
            // Initialize data logger
            _dataLoggerService = _host.Services.GetRequiredService<DataLoggerService>();
            await _dataLoggerService.InitializeAsync();
            LogStartupStep("Data logger service initialized");

            LogStartupStep("Initializing theme manager");
            // Initialize theme manager
            _themeManager = _host.Services.GetRequiredService<ThemeManager>();
            _themeManager.Initialize();
            LogStartupStep($"Theme manager initialized with {_themeManager.CurrentTheme} theme");
        }
        catch (Exception ex)
        {
            LogStartupStep($"Service initialization failed: {ex.Message}");
            WriteToEventLog($"Service initialization failed: {ex}", EVENTLOG_ERROR_TYPE);
            throw new InvalidOperationException("Failed to initialize core services", ex);
        }
    }
    
    private async Task SetupSystemTrayAsync()
    {
        try
        {
            LogStartupStep("Creating system tray icon on UI thread");
            
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                await Dispatcher.InvokeAsync(async () => await SetupSystemTrayAsync());
                return;
            }
            
            // Create system tray icon on UI thread (required for WPF resources)
            _taskbarIcon = new TaskbarIcon();
            
            LogStartupStep("Setting tray icon properties");
            _taskbarIcon.ToolTipText = "Keyboard + Mouse Odometer";
            
            // Try to load the icon with multiple fallback approaches
            bool iconLoaded = false;
            try
            {
                // First try: Pack URI (most common approach)
                var iconUri = new Uri("pack://application:,,,/Resources/app.ico");
                LogStartupStep($"Attempting to load icon from pack URI: {iconUri}");
                
                var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = iconUri;
                bitmapImage.EndInit();
                _taskbarIcon.IconSource = bitmapImage;
                iconLoaded = true;
                LogStartupStep("Icon loaded successfully from pack URI");
            }
            catch (Exception iconEx1)
            {
                LogStartupStep($"Pack URI icon loading failed: {iconEx1.Message}");
                LogStartupStep($"Stack trace: {iconEx1.StackTrace}");
                
                try
                {
                    // Second try: Use embedded resource stream
                    LogStartupStep("Attempting to load icon from embedded resource stream");
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    var resourceName = "KeyboardMouseOdometer.UI.Resources.app.ico";
                    
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            _taskbarIcon.Icon = new System.Drawing.Icon(stream);
                            iconLoaded = true;
                            LogStartupStep($"Icon loaded from embedded resource: {resourceName}");
                        }
                        else
                        {
                            LogStartupStep($"Embedded resource not found: {resourceName}");
                            LogStartupStep($"Available embedded resources: {string.Join(", ", assembly.GetManifestResourceNames())}");
                        }
                    }
                }
                catch (Exception iconEx2)
                {
                    LogStartupStep($"Embedded resource icon loading failed: {iconEx2.Message}");
                    
                    try
                    {
                        // Third try: Load from file system as fallback
                        LogStartupStep("Attempting to load icon from file system");
                        var appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        if (!string.IsNullOrEmpty(appPath))
                        {
                            var iconPath = System.IO.Path.Combine(appPath, "Resources", "app.ico");
                            LogStartupStep($"Looking for icon at: {iconPath}");
                            
                            if (File.Exists(iconPath))
                            {
                                _taskbarIcon.Icon = new System.Drawing.Icon(iconPath);
                                iconLoaded = true;
                                LogStartupStep($"Icon loaded from file system: {iconPath}");
                            }
                            else
                            {
                                LogStartupStep($"Icon file not found at: {iconPath}");
                                // Try alternate path in output directory
                                var altIconPath = System.IO.Path.Combine(appPath, "app.ico");
                                if (File.Exists(altIconPath))
                                {
                                    _taskbarIcon.Icon = new System.Drawing.Icon(altIconPath);
                                    iconLoaded = true;
                                    LogStartupStep($"Icon loaded from alternate path: {altIconPath}");
                                }
                                else
                                {
                                    LogStartupStep($"Icon file not found at alternate path: {altIconPath}");
                                }
                            }
                        }
                    }
                    catch (Exception iconEx3)
                    {
                        LogStartupStep($"File system icon loading failed: {iconEx3.Message}");
                    }
                }
                
                if (!iconLoaded)
                {
                    LogStartupStep("All icon loading methods failed - using default system icon");
                    // Try to create a simple default icon
                    try
                    {
                        // Create a simple 16x16 bitmap as fallback
                        var bitmap = new System.Drawing.Bitmap(16, 16);
                        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                        {
                            graphics.FillRectangle(System.Drawing.Brushes.Blue, 0, 0, 16, 16);
                            graphics.DrawString("K", new System.Drawing.Font("Arial", 8), System.Drawing.Brushes.White, 2, 2);
                        }
                        var iconHandle = bitmap.GetHicon();
                        _taskbarIcon.Icon = System.Drawing.Icon.FromHandle(iconHandle);
                        iconLoaded = true;
                        LogStartupStep("Created simple fallback icon");
                    }
                    catch (Exception fallbackEx)
                    {
                        LogStartupStep($"Fallback icon creation failed: {fallbackEx.Message}");
                    }
                }
            }
            
            // Configure interaction behavior
            _taskbarIcon.MenuActivation = Hardcodet.Wpf.TaskbarNotification.PopupActivationMode.RightClick;
            _taskbarIcon.LeftClickCommand = new KeyboardMouseOdometer.UI.ViewModels.RelayCommand(() => OpenDashboard_Click(this, new RoutedEventArgs()));
            
            // Get context menu resource
            try
            {
                var contextMenu = (System.Windows.Controls.ContextMenu)FindResource("AppTaskbarContextMenu");
                _taskbarIcon.ContextMenu = contextMenu;
                
                // Store references to menu items for dynamic updates
                foreach (var item in contextMenu.Items)
                {
                    if (item is System.Windows.Controls.MenuItem menuItem)
                    {
                        if (menuItem.Header?.ToString() == "Open Dashboard")
                        {
                            _openDashboardMenuItem = menuItem;
                        }
                        else if (menuItem.Header?.ToString() == "Show Toolbar")
                        {
                            _showToolbarMenuItem = menuItem;
                        }
                    }
                }
                
                LogStartupStep("Context menu attached successfully");
            }
            catch (Exception menuEx)
            {
                LogStartupStep($"Context menu setup failed: {menuEx.Message}");
                // Create a simple context menu as fallback
                try
                {
                    var simpleMenu = new System.Windows.Controls.ContextMenu();
                    
                    _openDashboardMenuItem = new System.Windows.Controls.MenuItem { Header = "Open Dashboard" };
                    _openDashboardMenuItem.Click += OpenDashboard_Click;
                    
                    _showToolbarMenuItem = new System.Windows.Controls.MenuItem { Header = "Show Toolbar" };
                    _showToolbarMenuItem.Click += ShowToolbar_Click;
                    
                    var pauseItem = new System.Windows.Controls.MenuItem { Header = "Pause Tracking" };
                    pauseItem.Click += PauseTracking_Click;
                    var resetItem = new System.Windows.Controls.MenuItem { Header = "Reset Statistics" };
                    resetItem.Click += ResetStats_Click;
                    var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
                    exitItem.Click += Exit_Click;
                    
                    simpleMenu.Items.Add(_openDashboardMenuItem);
                    simpleMenu.Items.Add(_showToolbarMenuItem);
                    simpleMenu.Items.Add(new System.Windows.Controls.Separator());
                    simpleMenu.Items.Add(pauseItem);
                    simpleMenu.Items.Add(resetItem);
                    simpleMenu.Items.Add(new System.Windows.Controls.Separator());
                    simpleMenu.Items.Add(exitItem);
                    
                    _taskbarIcon.ContextMenu = simpleMenu;
                    LogStartupStep("Fallback context menu created");
                }
                catch (Exception fallbackMenuEx)
                {
                    LogStartupStep($"Fallback context menu creation failed: {fallbackMenuEx.Message}");
                }
            }
            
            // Make the tray icon visible
            _taskbarIcon.Visibility = Visibility.Visible;
            LogStartupStep("System tray icon created and set to visible");
            
            // Additional diagnostics
            LogStartupStep($"TaskbarIcon properties: Visibility={_taskbarIcon.Visibility}, Icon={_taskbarIcon.Icon != null}, IconSource={_taskbarIcon.IconSource != null}");
            
            // Small delay to ensure tray icon is properly registered with the system
            await Task.Delay(500);
            
            // Force refresh by toggling visibility
            try
            {
                _taskbarIcon.Visibility = Visibility.Hidden;
                await Task.Delay(100);
                _taskbarIcon.Visibility = Visibility.Visible;
                LogStartupStep("Performed visibility refresh cycle");
            }
            catch (Exception refreshEx)
            {
                LogStartupStep($"Visibility refresh failed: {refreshEx.Message}");
            }
            
            // Verify the tray icon is actually visible
            if (_taskbarIcon.Visibility == Visibility.Visible)
            {
                LogStartupStep("System tray icon setup completed successfully - icon should now be visible");
                
                // Show a test balloon tip to confirm it's working
                try
                {
                    _taskbarIcon.ShowBalloonTip("Keyboard Mouse Odometer", "Application started successfully!", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    LogStartupStep("Test balloon tip displayed");
                }
                catch (Exception balloonEx)
                {
                    LogStartupStep($"Test balloon tip failed: {balloonEx.Message}");
                }
            }
            else
            {
                LogStartupStep("Warning: System tray icon visibility check failed");
            }
        }
        catch (Exception ex)
        {
            LogStartupStep($"System tray setup failed: {ex.Message}");
            LogStartupStep($"Stack trace: {ex.StackTrace}");
            WriteToEventLog($"System tray setup failed: {ex}", EVENTLOG_WARNING_TYPE);
            // Don't throw - system tray is not critical for core functionality
        }
    }
    
    private async Task InitializeHooksAsync()
    {
        try
        {
            LogStartupStep("Starting global hook initialization");
            if (_host == null)
            {
                throw new InvalidOperationException("Host is null during hook initialization");
            }
            
            _hookService = _host.Services.GetRequiredService<GlobalHookService>();
            LogStartupStep("Hook service retrieved from DI container");
            
            // Start hooks on UI thread (hooks require UI thread for proper operation)
            LogStartupStep("Testing hook creation");
            _hookService.StartHooks();
            LogStartupStep("Global hooks started successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            LogStartupStep($"Hook initialization failed - Access denied: {ex.Message}");
            WriteToEventLog($"Global hooks failed due to insufficient permissions: {ex}", EVENTLOG_ERROR_TYPE);
            await ShowPermissionError();
        }
        catch (Exception ex)
        {
            LogStartupStep($"Hook initialization failed: {ex.Message}");
            WriteToEventLog($"Global hook initialization failed: {ex}", EVENTLOG_ERROR_TYPE);
            await ShowHookError(ex);
        }
    }
    
    private async Task HandleStartupError(Exception ex)
    {
        var errorMessage = $"Application startup failed: {ex.Message}";
        var detailedError = $"{errorMessage}\n\nDetailed Error:\n{ex}";
        var startupLogText = _startupLog.ToString();
        
        LogStartupStep($"FATAL ERROR: {ex.Message}");
        WriteToEventLog($"Application startup failed: {ex}", EVENTLOG_ERROR_TYPE);
        
        // Show comprehensive error dialog
        var result = MessageBox.Show(
            $"{errorMessage}\n\nStartup Log:\n{startupLogText}\n\nWould you like to see detailed error information?",
            "Keyboard Mouse Odometer - Startup Error",
            MessageBoxButton.YesNo,
            MessageBoxImage.Error);
            
        if (result == MessageBoxResult.Yes)
        {
            MessageBox.Show(detailedError, "Detailed Error Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        // Try to write error log to file
        try
        {
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KeyboardMouseOdometer", "startup-error.log");
            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            await File.WriteAllTextAsync(logPath, $"Startup Error Log - {DateTime.Now}\n\n{startupLogText}\n\nDetailed Error:\n{detailedError}");
            MessageBox.Show($"Error details have been saved to:\n{logPath}", "Error Log Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch
        {
            // If we can't even write to file, just ignore
        }
        
        Shutdown();
    }
    
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        var message = $"CRITICAL: Unhandled exception - IsTerminating: {e.IsTerminating}";
        
        try
        {
            Log.Fatal(exception, message);
            _logger?.LogCritical(exception, message);
        }
        catch { }
        
        LogToEventLog($"{message}\n{exception}", EVENTLOG_ERROR_TYPE);
        
        if (e.IsTerminating)
        {
            SaveCrashDump(exception);
        }
    }
    
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            Log.Error(e.Exception, "Unobserved task exception");
            _logger?.LogError(e.Exception, "Unobserved task exception");
        }
        catch { }
        
        e.SetObserved(); // Prevent process termination
    }
    
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            Log.Error(e.Exception, "Dispatcher unhandled exception");
            _logger?.LogError(e.Exception, "Dispatcher unhandled exception");
        }
        catch { }
        
        // Try to recover
        e.Handled = true;
        
        // Show error to user
        MessageBox.Show(
            $"An error occurred: {e.Exception.Message}\n\nThe application will try to continue.",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
    
    private void SaveCrashDump(Exception? exception)
    {
        try
        {
            var crashDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KeyboardMouseOdometer",
                "crashes");
            Directory.CreateDirectory(crashDir);
            
            var crashFile = Path.Combine(crashDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            var crashInfo = new StringBuilder();
            crashInfo.AppendLine($"Crash Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            crashInfo.AppendLine($"Application Version: {GetApplicationVersion()}");
            crashInfo.AppendLine($"OS: {Environment.OSVersion}");
            crashInfo.AppendLine($".NET Version: {Environment.Version}");
            crashInfo.AppendLine();
            crashInfo.AppendLine("Exception:");
            crashInfo.AppendLine(exception?.ToString() ?? "No exception information available");
            
            File.WriteAllText(crashFile, crashInfo.ToString());
        }
        catch { }
    }
    
    private string GetApplicationVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
    
    private void LogToEventLog(string message, ushort eventType)
    {
        try
        {
            var eventSource = RegisterEventSource(null, "KeyboardMouseOdometer");
            if (eventSource != IntPtr.Zero)
            {
                var messages = new string[] { message };
                ReportEvent(eventSource, eventType, 0, 0, IntPtr.Zero, 1, 0, messages, IntPtr.Zero);
                DeregisterEventSource(eventSource);
            }
        }
        catch
        {
            // Silently fail if we can't write to event log
        }
    }
    
    private void LogStartupStep(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {message}";
        _startupLog.AppendLine(logEntry);
        
        // Also log to debug output
        Debug.WriteLine(logEntry);
        
        // Log to logger if available
        _logger?.LogInformation(message);
        
        // In diagnostic mode, also write to console
        if (_isDiagnosticMode)
        {
            Console.WriteLine(logEntry);
        }
    }
    
    private void WriteToEventLog(string message, ushort eventType)
    {
        try
        {
            var eventSource = RegisterEventSource(null, "KeyboardMouseOdometer");
            if (eventSource != IntPtr.Zero)
            {
                string[] messages = { message };
                ReportEvent(eventSource, eventType, 0, 1000, IntPtr.Zero, 1, 0, messages, IntPtr.Zero);
                DeregisterEventSource(eventSource);
            }
        }
        catch
        {
            // If we can't write to event log, just continue
        }
    }
    
    private void ShowTestModeDialog()
    {
        MessageBox.Show(
            "Keyboard Mouse Odometer - Test Mode\n\n" +
            "This confirms the application can start and show dialogs.\n" +
            "WPF initialization: OK\n" +
            ".NET Runtime: OK\n" +
            "Windows Integration: OK\n\n" +
            "If you see this message, the basic application framework is working.\n" +
            "The issue is likely with global hooks or service initialization.",
            "Test Mode - Application Working",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        Shutdown();
    }
    
    private void ShowDiagnosticInfo()
    {
        var diagnosticInfo = $"Diagnostic Information:\n\n" +
            $"Startup Log:\n{_startupLog}\n\n" +
            $"OS Version: {Environment.OSVersion}\n" +
            $".NET Version: {Environment.Version}\n" +
            $"Working Directory: {Environment.CurrentDirectory}\n" +
            $"User: {Environment.UserName}\n" +
            $"Machine: {Environment.MachineName}\n" +
            $"Is Admin: {IsRunningAsAdmin()}";
            
        MessageBox.Show(diagnosticInfo, "Diagnostic Mode", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private Task ShowPermissionError()
    {
        var message = "Global keyboard/mouse hooks require elevated permissions or may be blocked by security software.\n\n" +
            "Solutions:\n" +
            "1. Run as Administrator\n" +
            "2. Add exception in antivirus software\n" +
            "3. Check Windows Defender SmartScreen settings\n\n" +
            "Would you like to continue in limited mode (no global tracking)?";  
            
        var result = MessageBox.Show(message, "Permission Required", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        
        if (result == MessageBoxResult.Yes)
        {
            // Continue without hooks
            LogStartupStep("Continuing in limited mode without global hooks");
        }
        else
        {
            Shutdown();
        }
        
        return Task.CompletedTask;
    }
    
    private Task ShowHookError(Exception ex)
    {
        var message = $"Failed to initialize global input hooks: {ex.Message}\n\n" +
            "This may be caused by:\n" +
            "• Security software blocking the application\n" +
            "• Windows Defender SmartScreen\n" +
            "• Missing Visual C++ redistributables\n" +
            "• Corrupted Windows system files\n\n" +
            "Would you like to continue in limited mode?";  
            
        var result = MessageBox.Show(message, "Hook Initialization Failed", MessageBoxButton.YesNo, MessageBoxImage.Error);
        
        if (result == MessageBoxResult.No)
        {
            Shutdown();
        }
        
        return Task.CompletedTask;
    }
    
    private static bool IsRunningAsAdmin()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private void ShowToolbar()
    {
        if (_toolbarWindow == null)
        {
            _toolbarWindow = _host?.Services.GetRequiredService<ToolbarWindow>();
            
            // Set up event handlers for toolbar window state changes
            if (_toolbarWindow != null)
            {
                _toolbarWindow.IsVisibleChanged += ToolbarWindow_IsVisibleChanged;
            }
        }
        _toolbarWindow?.Show();
    }

    // System tray menu event handlers
    private void OpenDashboard_Click(object sender, RoutedEventArgs e)
    {
        if (_mainWindow == null)
        {
            _mainWindow = _host?.Services.GetRequiredService<MainWindow>();
        }
        
        if (_mainWindow != null)
        {
            if (_mainWindow.IsVisible && _mainWindow.WindowState != WindowState.Minimized)
            {
                // Dashboard is visible, hide it
                _mainWindow.Hide();
                UpdateDashboardMenuText(false);
            }
            else
            {
                // Dashboard is hidden or minimized, show it
                _mainWindow.Show();
                _mainWindow.Activate();
                _mainWindow.WindowState = WindowState.Normal;
                UpdateDashboardMenuText(true);
            }
        }
    }

    private void ShowToolbar_Click(object sender, RoutedEventArgs e)
    {
        if (_toolbarWindow == null)
        {
            _toolbarWindow = _host?.Services.GetRequiredService<ToolbarWindow>();
            
            // Set up event handlers for toolbar window state changes
            if (_toolbarWindow != null)
            {
                _toolbarWindow.IsVisibleChanged += ToolbarWindow_IsVisibleChanged;
            }
        }
        
        if (_toolbarWindow != null)
        {
            if (_toolbarWindow.IsVisible)
            {
                // Toolbar is visible, hide it
                _toolbarWindow.Hide();
                UpdateToolbarMenuText(false);
                
                // Update configuration
                var configuration = _host?.Services.GetService<Core.Models.Configuration>();
                if (configuration != null)
                {
                    configuration.ShowToolbar = false;
                    configuration.SaveToFile();
                }
            }
            else
            {
                // Toolbar is hidden, show it
                _toolbarWindow.Show();
                UpdateToolbarMenuText(true);
                
                // Update configuration
                var configuration = _host?.Services.GetService<Core.Models.Configuration>();
                if (configuration != null)
                {
                    configuration.ShowToolbar = true;
                    configuration.SaveToFile();
                }
            }
        }
    }

    private void PauseTracking_Click(object sender, RoutedEventArgs e)
    {
        if (_hookService != null)
        {
            if (_hookService.IsRunning)
            {
                _hookService.StopHooks();
                if (sender is System.Windows.Controls.MenuItem menuItem) menuItem.Header = "Resume Tracking";
            }
            else
            {
                _hookService.StartHooks();
                if (sender is System.Windows.Controls.MenuItem menuItem2) menuItem2.Header = "Pause Tracking";
            }
        }
    }

    private async void ResetStats_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to reset all statistics for today?", 
            "Reset Statistics", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes && _dataLoggerService != null)
        {
            await _dataLoggerService.ResetStatsAsync();
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        LogStartupStep("Application shutdown requested by user");
        Shutdown();
    }
    
    // Event handlers for window state changes
    private void MainWindow_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (_mainWindow != null)
        {
            var isVisible = _mainWindow.IsVisible && _mainWindow.WindowState != WindowState.Minimized;
            UpdateDashboardMenuText(isVisible);
        }
    }
    
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (_mainWindow != null)
        {
            var isVisible = _mainWindow.IsVisible && _mainWindow.WindowState != WindowState.Minimized;
            UpdateDashboardMenuText(isVisible);
        }
    }
    
    private void ToolbarWindow_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (_toolbarWindow != null)
        {
            UpdateToolbarMenuText(_toolbarWindow.IsVisible);
        }
    }
    
    // Helper methods for updating menu text
    private void UpdateDashboardMenuText(bool isVisible)
    {
        if (_openDashboardMenuItem != null)
        {
            _openDashboardMenuItem.Header = isVisible ? "Close Dashboard" : "Open Dashboard";
        }
    }
    
    private void UpdateToolbarMenuText(bool isVisible)
    {
        if (_showToolbarMenuItem != null)
        {
            _showToolbarMenuItem.Header = isVisible ? "Hide Toolbar" : "Show Toolbar";
        }
    }
}