using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using ClashWidget.Models;
using ClashWidget.Services;

namespace ClashWidget.ViewModels;

public struct SpeedPoint
{
    public double DownloadBps { get; set; }
    public double UploadBps { get; set; }
}

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private ClashApiService _apiService;
    private readonly DispatcherTimer _speedTimer;
    private TrafficInfo? _lastTraffic;
    private DateTime _lastPollTime;
    private DateTime _lastNodeFetchTime = DateTime.MinValue;
    private bool _isPolling;
    private string? _groupName;
    private const int MaxHistoryPoints = 200;
    private static readonly TimeSpan NodeFetchInterval = TimeSpan.FromSeconds(3);

    public MainViewModel()
    {
        var config = ConfigService.Load();
        _apiService = new ClashApiService(config);
        _speedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _speedTimer.Tick += OnSpeedTimerTick;
    }

    public ObservableCollection<SpeedPoint> SpeedHistory { get; } = new();
    public ObservableCollection<string> AvailableNodes { get; } = new();
    private readonly Dictionary<string, string> _nodeRawNames = new();

    private double _downloadSpeed;
    public double DownloadSpeed
    {
        get => _downloadSpeed;
        set => SetProperty(ref _downloadSpeed, value);
    }

    private double _uploadSpeed;
    public double UploadSpeed
    {
        get => _uploadSpeed;
        set => SetProperty(ref _uploadSpeed, value);
    }

    private string _currentNode = "Connecting...";
    public string CurrentNode
    {
        get => _currentNode;
        set => SetProperty(ref _currentNode, value);
    }

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    private string _latencyText = "";
    public string LatencyText
    {
        get => _latencyText;
        set => SetProperty(ref _latencyText, value);
    }

    private bool _isDropdownOpen;
    public bool IsDropdownOpen
    {
        get => _isDropdownOpen;
        set
        {
            if (SetProperty(ref _isDropdownOpen, value))
                DropdownToggled?.Invoke(value);
        }
    }

    public event Action<bool>? DropdownToggled;

    public async void Start()
    {
        _speedTimer.Start();
        LatencyText = "...";
        var ms = await _apiService.MeasureLatencyAsync();
        LatencyText = ms > 0 ? $"{ms:F0}ms" : "— ms";
    }

    public void Reconnect()
    {
        _speedTimer.Stop();
        var config = ConfigService.Load();
        _apiService = new ClashApiService(config);
        _lastTraffic = null;
        _lastNodeFetchTime = DateTime.MinValue;
        SpeedHistory.Clear();
        DownloadSpeed = 0;
        UploadSpeed = 0;
        CurrentNode = "Connecting...";
        IsConnected = false;
        LatencyText = "...";
        _speedTimer.Start();
    }

    public async Task RefreshLatencyAsync()
    {
        var ms = await _apiService.MeasureLatencyAsync();
        LatencyText = ms > 0 ? $"{ms:F0}ms" : "— ms";
    }

    public async Task SwitchNodeAsync(string nodeName)
    {
        if (_groupName == null) return;
        var rawName = _nodeRawNames.GetValueOrDefault(nodeName, nodeName);
        await _apiService.SwitchProxyAsync(_groupName, rawName);
        IsDropdownOpen = false;
    }

    private async void OnSpeedTimerTick(object? sender, EventArgs e)
    {
        if (_isPolling) return;
        _isPolling = true;

        try
        {
            var traffic = await _apiService.GetTrafficAsync();

            if (traffic == null)
            {
                IsConnected = false;
                DownloadSpeed = 0;
                UploadSpeed = 0;
                CurrentNode = "Disconnected";
                return;
            }

            IsConnected = true;
            var now = DateTime.UtcNow;

            if (_lastTraffic != null)
            {
                var elapsed = (now - _lastPollTime).TotalSeconds;
                if (elapsed > 0)
                {
                    DownloadSpeed = Math.Max(0, (traffic.DownTotal - _lastTraffic.DownTotal) / elapsed);
                    UploadSpeed = Math.Max(0, (traffic.UpTotal - _lastTraffic.UpTotal) / elapsed);

                    SpeedHistory.Add(new SpeedPoint { DownloadBps = DownloadSpeed, UploadBps = UploadSpeed });
                    while (SpeedHistory.Count > MaxHistoryPoints)
                        SpeedHistory.RemoveAt(0);
                }
            }

            _lastTraffic = traffic;
            _lastPollTime = now;

            if (now - _lastNodeFetchTime >= NodeFetchInterval)
            {
                _lastNodeFetchTime = now;
                var groups = await _apiService.GetProxyGroupsAsync();
                var proxies = await _apiService.GetProxiesAsync();
                var (nodeName, groupName, available) = _apiService.ExtractCurrentNode(groups, proxies);

                if (nodeName != null) CurrentNode = nodeName;
                _groupName = groupName;

                if (available != null)
                {
                    AvailableNodes.Clear();
                    _nodeRawNames.Clear();
                    foreach (var raw in available)
                    {
                        var display = ClashApiService.FixEmojiFlags(raw);
                        AvailableNodes.Add(display);
                        _nodeRawNames[display] = raw;
                    }
                }
            }
        }
        finally
        {
            _isPolling = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public void Dispose() => _speedTimer.Stop();
}
