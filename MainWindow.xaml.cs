using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ClashWidget.Helpers;
using ClashWidget.ViewModels;

namespace ClashWidget;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private Polyline? _downloadLine;
    private Polyline? _uploadLine;
    private bool _isMaximized;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        Opacity = 0;
        Loaded += OnLoaded;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.SpeedHistory.CollectionChanged += OnSpeedHistoryChanged;
        _viewModel.DropdownToggled += OnDropdownToggled;

        CreateSparklines();
        UpdateStatusDot();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                var source = HwndSource.FromHwnd(hwnd);
                if (source?.CompositionTarget != null)
                    source.CompositionTarget.BackgroundColor = Colors.Transparent;
            }
            NativeMethods.ApplyAcrylic(this);
            NativeMethods.SetTopmost(this);
            NativeMethods.HideFromTaskbar(this);
        }
        catch { }

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Top + 20;

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, fadeIn);

        _viewModel.Start();
    }

    private void CreateSparklines()
    {
        _downloadLine = new Polyline
        {
            Stroke = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
            StrokeThickness = 1.5, StrokeLineJoin = PenLineJoin.Round
        };
        _uploadLine = new Polyline
        {
            Stroke = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
            StrokeThickness = 1, StrokeLineJoin = PenLineJoin.Round
        };
        SparklineCanvas.Children.Add(_downloadLine);
        SparklineCanvas.Children.Add(_uploadLine);
    }

    private void OnSpeedHistoryChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(DrawSparklines));
    }

    private void DrawSparklines()
    {
        if (_downloadLine == null || _uploadLine == null) return;
        var history = _viewModel.SpeedHistory.ToList();
        if (history.Count < 2) return;
        double cw = SparklineCanvas.ActualWidth, ch = SparklineCanvas.ActualHeight;
        if (cw < 10 || ch < 10) return;
        double maxVal = Math.Max(history.Max(p => p.DownloadBps), history.Max(p => p.UploadBps));
        if (maxVal < 1) maxVal = 1;
        double xStep = cw / (history.Count - 1);
        var dl = new PointCollection();
        var ul = new PointCollection();
        for (int i = 0; i < history.Count; i++)
        {
            double x = i * xStep;
            dl.Add(new Point(x, ch - (history[i].DownloadBps / maxVal * ch)));
            ul.Add(new Point(x, ch - (history[i].UploadBps / maxVal * ch)));
        }
        _downloadLine.Points = dl;
        _uploadLine.Points = ul;
    }

    private void OnWindowPreviewClick(object sender, MouseButtonEventArgs e)
    {
        if (NodePopup.IsOpen)
        {
            // Close popup if click is outside the popup
            var pos = e.GetPosition(PopupBorder);
            if (pos.X < 0 || pos.Y < 0 || pos.X > PopupBorder.ActualWidth || pos.Y > PopupBorder.ActualHeight)
            {
                NodePopup.IsOpen = false;
            }
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try { DragMove(); } catch { }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => FadeOutAndClose();
    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
    {
        _isMaximized = !_isMaximized;
        WindowState = _isMaximized ? WindowState.Maximized : WindowState.Normal;
    }

    private void OnNodeAreaClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        NodePopup.IsOpen = true;
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        _viewModel.IsDropdownOpen = true;
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _viewModel.IsDropdownOpen = false;
    }

    private void OnDropdownToggled(bool open)
    {
        Dispatcher.BeginInvoke(new Action(() => NodePopup.IsOpen = open));
    }

    private void OnPopupBorderClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true; // prevent bubbling to window drag
    }

    private async void OnNodeItemClick(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (NodeListBox.SelectedItem is string nodeName && !string.IsNullOrEmpty(nodeName))
        {
            await _viewModel.SwitchNodeAsync(nodeName);
            NodePopup.IsOpen = false;
        }
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow(this);
        settings.Owner = this;
        settings.ShowDialog();
        if (settings.Saved) _viewModel.Reconnect();
    }

    private async void OnRefreshLatencyClick(object sender, RoutedEventArgs e)
    {
        BtnRefreshLatency.IsEnabled = false;
        try { await _viewModel.RefreshLatencyAsync(); } finally { BtnRefreshLatency.IsEnabled = true; }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsConnected)) UpdateStatusDot();
    }

    private void UpdateStatusDot()
    {
        StatusDot.Fill = new SolidColorBrush(_viewModel.IsConnected ? Colors.LimeGreen : Colors.Tomato);
    }

    private void FadeOutAndClose()
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fadeOut);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
