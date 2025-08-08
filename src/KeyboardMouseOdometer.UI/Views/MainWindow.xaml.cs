using KeyboardMouseOdometer.UI.ViewModels;
using KeyboardMouseOdometer.Core.Models;
using System.Windows;

namespace KeyboardMouseOdometer.UI.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Configuration _configuration;
    
    public MainWindow(MainWindowViewModel viewModel, Configuration configuration)
    {
        InitializeComponent();
        DataContext = viewModel;
        _configuration = configuration;
        
        // Restore window size
        Width = _configuration.MainWindowWidth;
        Height = _configuration.MainWindowHeight;
        
        // Subscribe to size changed event
        SizeChanged += MainWindow_SizeChanged;
    }
    
    private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // Save window size
        _configuration.MainWindowWidth = Width;
        _configuration.MainWindowHeight = Height;
        _configuration.SaveToFile();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }

        base.OnStateChanged(e);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Don't actually close, just hide to tray
        e.Cancel = true;
        Hide();
    }
}