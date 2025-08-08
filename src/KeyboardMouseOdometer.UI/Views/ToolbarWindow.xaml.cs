using KeyboardMouseOdometer.UI.ViewModels;
using KeyboardMouseOdometer.Core.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using System.Windows.Forms;

namespace KeyboardMouseOdometer.UI.Views;

/// <summary>
/// Simple transparent floating toolbar window
/// </summary>
public partial class ToolbarWindow : Window
{
    private readonly ToolbarViewModel _viewModel;
    private readonly Configuration _configuration;
    private bool _forceClose = false;

    public ToolbarWindow(ToolbarViewModel viewModel, Configuration configuration)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _configuration = configuration;
        DataContext = viewModel;
        
        // Set minimum size constraints
        MinWidth = 300;
        MinHeight = 35;
        
        // Load size and position from configuration
        Width = _configuration.ToolbarWidth;
        Height = _configuration.ToolbarHeight;
        
        Loaded += ToolbarWindow_Loaded;
        SizeChanged += ToolbarWindow_SizeChanged;
        LocationChanged += ToolbarWindow_LocationChanged;
    }

    private void ToolbarWindow_Loaded(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Toolbar window loaded. Initial size: {Width}x{Height}");
        // Position the toolbar at the bottom center of the screen by default
        PositionWindow();
    }

    private void PositionWindow()
    {
        // Try to find the saved monitor
        Screen? targetScreen = null;
        if (!string.IsNullOrEmpty(_configuration.ToolbarMonitorDeviceName))
        {
            targetScreen = Screen.AllScreens.FirstOrDefault(s => s.DeviceName == _configuration.ToolbarMonitorDeviceName);
        }
        
        // Fall back to primary screen if saved monitor not found
        if (targetScreen == null)
        {
            targetScreen = Screen.PrimaryScreen ?? Screen.AllScreens.FirstOrDefault();
        }
        
        if (targetScreen == null)
        {
            // If still no screen found, skip positioning
            return;
        }

        var screenBounds = targetScreen.Bounds;
        var workAreaBounds = targetScreen.WorkingArea;
        
        // Use saved position if available and valid for the target screen
        if (_configuration.ToolbarLeft >= 0 && _configuration.ToolbarTop >= 0)
        {
            // Check if saved position is within the target screen bounds
            var savedLeft = _configuration.ToolbarLeft;
            var savedTop = _configuration.ToolbarTop;
            
            // Ensure window is within screen bounds
            Left = Math.Max(screenBounds.Left, Math.Min(savedLeft, screenBounds.Right - Width));
            Top = Math.Max(screenBounds.Top, Math.Min(savedTop, workAreaBounds.Bottom - Height));
            System.Diagnostics.Debug.WriteLine($"Restored toolbar position: {Left},{Top} on monitor {targetScreen.DeviceName}");
        }
        else
        {
            // Center horizontally on target screen, position near bottom but above taskbar
            Left = screenBounds.Left + (screenBounds.Width - Width) / 2;
            Top = workAreaBounds.Bottom - Height - 10; // 10px margin from taskbar
            System.Diagnostics.Debug.WriteLine($"Default toolbar position: {Left},{Top} on monitor {targetScreen.DeviceName}");
        }
    }

    /// <summary>
    /// Save toolbar size when it changes
    /// </summary>
    private void ToolbarWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Toolbar size changed: {Width}x{Height}");
        _configuration.ToolbarWidth = Width;
        _configuration.ToolbarHeight = Height;
        SaveConfiguration();
    }
    
    /// <summary>
    /// Save toolbar position when it moves
    /// </summary>
    private void ToolbarWindow_LocationChanged(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Toolbar position changed: {Left},{Top}");
        _configuration.ToolbarLeft = Left;
        _configuration.ToolbarTop = Top;
        
        // Save the monitor the toolbar is currently on
        var currentScreen = Screen.FromPoint(new System.Drawing.Point((int)(Left + Width/2), (int)(Top + Height/2)));
        if (currentScreen != null)
        {
            _configuration.ToolbarMonitorDeviceName = currentScreen.DeviceName;
        }
        
        SaveConfiguration();
    }
    
    /// <summary>
    /// Save configuration to file
    /// </summary>
    private void SaveConfiguration()
    {
        try
        {
            _configuration.SaveToFile();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save toolbar configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Make the window draggable by clicking and dragging the main area
    /// </summary>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        
        if (e.ClickCount == 1)
        {
            try
            {
                // With WindowChrome, we need to check if we're in the client area
                // and not on a resize border
                var position = e.GetPosition(this);
                var chrome = WindowChrome.GetWindowChrome(this);
                
                if (chrome != null)
                {
                    var borderThickness = chrome.ResizeBorderThickness;
                    
                    // Check if click is in the draggable area (not on resize borders)
                    if (position.X > borderThickness.Left && 
                        position.X < ActualWidth - borderThickness.Right &&
                        position.Y > borderThickness.Top && 
                        position.Y < ActualHeight - borderThickness.Bottom)
                    {
                        DragMove();
                    }
                }
                else
                {
                    DragMove();
                }
            }
            catch (InvalidOperationException)
            {
                // DragMove can only be called when left mouse button is down
                // Ignore this exception as it's expected in some cases
            }
        }
    }

    /// <summary>
    /// Override closing behavior to hide instead of close
    /// </summary>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_forceClose)
        {
            // Don't actually close, just hide
            e.Cancel = true;
            Hide();
        }
        // If _forceClose is true, allow normal closing behavior
    }

    /// <summary>
    /// Method to actually close the window when application is shutting down
    /// </summary>
    public void ForceClose()
    {
        // Set a flag to bypass the closing override
        _forceClose = true;
        Close();
    }
}