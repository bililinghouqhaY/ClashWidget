using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace ClashWidget;

public partial class App : Application
{
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Kill any existing ClashWidget instances
        var current = Process.GetCurrentProcess();
        foreach (var p in Process.GetProcessesByName("ClashWidget"))
        {
            if (p.Id != current.Id)
            {
                try { p.Kill(); p.WaitForExit(2000); } catch { }
            }
        }

        // Single-instance mutex
        _mutex = new Mutex(true, "ClashWidget_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("ClashWidget is already running.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Error:\n{args.Exception.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            var window = new MainWindow();
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup failed:\n{ex.Message}",
                "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
