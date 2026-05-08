using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClashWidget.Helpers;
using ClashWidget.Services;
using ClashWidget.ViewModels;

namespace ClashWidget;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly Window _owner;
    public bool Saved { get; private set; }

    public SettingsWindow(Window owner)
    {
        InitializeComponent();

        _owner = owner;
        _viewModel = new SettingsViewModel();
        DataContext = _viewModel;

        Opacity = 0;
        Loaded += OnLoaded;
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

        PositionWindow();

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, fadeIn);
    }

    private void PositionWindow()
    {
        double left = _owner.Left - Width - 12;
        if (left < 0) left = _owner.Left + _owner.ActualWidth + 12;

        var workArea = SystemParameters.WorkArea;
        double top = _owner.Top;
        if (top < workArea.Top) top = workArea.Top + 10;
        if (top + Height > workArea.Bottom) top = workArea.Bottom - Height - 10;

        Left = left;
        Top = top;
    }

    private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try { DragMove(); } catch { }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => FadeOutAndClose();

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        ConfigService.Save(_viewModel.GetConfig());
        Saved = true;
        FadeOutAndClose();
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
}
