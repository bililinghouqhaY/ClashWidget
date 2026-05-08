using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClashWidget.Models;
using ClashWidget.Services;

namespace ClashWidget.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly AppConfig _config;

    public SettingsViewModel()
    {
        _config = ConfigService.Load();
        _apiHost = _config.ApiHost;
        _apiPort = _config.ApiPort.ToString();
        _apiSecret = _config.ApiSecret;
        _testUrl = _config.TestUrl;
    }

    private string _apiHost;
    public string ApiHost
    {
        get => _apiHost;
        set => SetProperty(ref _apiHost, value);
    }

    private string _apiPort;
    public string ApiPort
    {
        get => _apiPort;
        set => SetProperty(ref _apiPort, value);
    }

    private string _apiSecret;
    public string ApiSecret
    {
        get => _apiSecret;
        set => SetProperty(ref _apiSecret, value);
    }

    private string _testUrl;
    public string TestUrl
    {
        get => _testUrl;
        set => SetProperty(ref _testUrl, value);
    }

    public AppConfig GetConfig()
    {
        int.TryParse(ApiPort, out int port);
        if (port <= 0 || port > 65535) port = 9090;

        return new AppConfig
        {
            ApiHost = ApiHost.Trim(),
            ApiPort = port,
            ApiSecret = ApiSecret.Trim(),
            TestUrl = TestUrl.Trim()
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
