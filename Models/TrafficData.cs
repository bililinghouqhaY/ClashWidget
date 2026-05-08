using System.Text.Json.Serialization;

namespace ClashWidget.Models;

public class TrafficInfo
{
    [JsonPropertyName("upTotal")]
    public long UpTotal { get; set; }

    [JsonPropertyName("downTotal")]
    public long DownTotal { get; set; }
}

public class ProxyEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("now")]
    public string? Now { get; set; }

    [JsonPropertyName("all")]
    public List<string>? All { get; set; }
}

public class ProxyGroupResponse
{
    [JsonPropertyName("proxies")]
    public List<ProxyEntry> Proxies { get; set; } = new();
}
