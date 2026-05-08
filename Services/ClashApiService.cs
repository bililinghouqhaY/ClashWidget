using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClashWidget.Models;

namespace ClashWidget.Services;

public class ClashApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _testUrl;

    private readonly string _tokenQuery;

    public ClashApiService(AppConfig config)
    {
        _testUrl = config.TestUrl;
        _tokenQuery = $"?token={Uri.EscapeDataString(config.ApiSecret)}";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(config.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(5)
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", config.ApiSecret);
    }

    private string WithToken(string path) => $"{path}{_tokenQuery}";

    public async Task<TrafficInfo?> GetTrafficAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/traffic");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var response = await _httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            if (!response.IsSuccessStatusCode) return null;

            using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var reader = new StreamReader(stream);
            var line = await reader.ReadLineAsync(cts.Token);

            if (line != null)
                return JsonSerializer.Deserialize<TrafficInfo>(line);
        }
        catch { }
        return null;
    }

    public async Task<ProxyGroupResponse?> GetProxyGroupsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProxyGroupResponse>("/group");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ProxyEntry>?> GetProxiesAsync()
    {
        try
        {
            // /proxies returns {"proxies": {"GLOBAL": {...}, "🔰 选择节点": {...}}}
            using var doc = await _httpClient.GetFromJsonAsync<JsonDocument>("/proxies");
            if (doc == null) return null;

            var root = doc.RootElement;
            if (!root.TryGetProperty("proxies", out var proxiesEl) || proxiesEl.ValueKind != JsonValueKind.Object)
                return null;

            var list = new List<ProxyEntry>();
            foreach (var kv in proxiesEl.EnumerateObject())
            {
                var entry = JsonSerializer.Deserialize<ProxyEntry>(kv.Value.GetRawText());
                if (entry != null)
                {
                    entry.Name = kv.Name;
                    list.Add(entry);
                }
            }
            return list;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> SwitchProxyAsync(string groupName, string proxyName)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));

            // First: ensure parent groups route through this proxy group
            var proxies = await GetProxiesAsync();
            if (proxies != null)
            {
                var skip = new HashSet<string> { "DIRECT", "REJECT", "REJECT-DROP" };
                foreach (var g in proxies)
                {
                    if (g.Type == "Selector" && g.All != null
                        && g.All.Contains(groupName)
                        && g.Now != null && skip.Contains(g.Now))
                    {
                        var parentPayload = JsonSerializer.Serialize(new { name = groupName });
                        using var parentContent = new StringContent(parentPayload,
                            System.Text.Encoding.UTF8, "application/json");
                        using var parentReq = new HttpRequestMessage(HttpMethod.Put,
                            WithToken($"/proxies/{Uri.EscapeDataString(g.Name)}"))
                        { Content = parentContent };
                        await _httpClient.SendAsync(parentReq, cts.Token);
                    }
                }
            }

            // Then: switch the actual proxy within the group
            var payload = JsonSerializer.Serialize(new { name = proxyName });
            using var content = new StringContent(payload,
                System.Text.Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put,
                WithToken($"/proxies/{Uri.EscapeDataString(groupName)}"))
            { Content = content };
            using var response = await _httpClient.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                // Force close all connections to reconnect through the new proxy
                try
                {
                    using var delReq = new HttpRequestMessage(HttpMethod.Delete,
                        WithToken("/connections"));
                    await _httpClient.SendAsync(delReq, cts.Token);
                }
                catch { }
            }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public (string? nodeName, string? groupName, List<string>? availableNodes) ExtractCurrentNode(
        ProxyGroupResponse? groupResponse, List<ProxyEntry>? proxiesList)
    {
        var skipValues = new HashSet<string> { "DIRECT", "REJECT", "REJECT-DROP" };
        var priorityNames = new[] { "🔰 选择节点", "Proxy", "🚀 自动选择", "♻️ 自动故障转移" };

        List<ProxyEntry>? entries = proxiesList ?? groupResponse?.Proxies;
        if (entries == null) return (null, null, null);

        foreach (var name in priorityNames)
        {
            var match = entries.FirstOrDefault(p =>
                p.Name == name && p.Now != null && !skipValues.Contains(p.Now));
            if (match?.Now != null)
                return (FixEmojiFlags(match.Now), match.Name, match.All);
        }

        var selector = entries.FirstOrDefault(p =>
            p.Type == "Selector" && p.Now != null && !skipValues.Contains(p.Now));
        if (selector?.Now != null)
            return (FixEmojiFlags(selector.Now), selector.Name, selector.All);

        return (null, null, null);
    }

    public async Task<double> MeasureLatencyAsync()
    {
        try
        {
            using var testClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var sw = Stopwatch.StartNew();
            var response = await testClient.GetAsync(_testUrl);
            sw.Stop();

            return response.IsSuccessStatusCode ? sw.Elapsed.TotalMilliseconds : -1;
        }
        catch
        {
            return -1;
        }
    }

    public static string FixEmojiFlags(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var result = new System.Text.StringBuilder();
        int i = 0;
        int pairStart = -1;

        while (i < text.Length)
        {
            char c = text[i];

            if (char.IsHighSurrogate(c) && i + 1 < text.Length)
            {
                int codePoint = char.ConvertToUtf32(c, text[i + 1]);

                if (codePoint >= 0x1F1E6 && codePoint <= 0x1F1FF)
                {
                    if (pairStart < 0)
                    {
                        pairStart = result.Length;
                        result.Append((char)('A' + (codePoint - 0x1F1E6)));
                    }
                    else
                    {
                        result.Append((char)('A' + (codePoint - 0x1F1E6)));
                        var countryCode = result.ToString(pairStart, 2);
                        result.Length = pairStart;
                        result.Append('[');
                        result.Append(countryCode);
                        result.Append(']');
                        pairStart = -1;
                    }
                    i += 2;
                    continue;
                }
                else
                {
                    if (pairStart >= 0) pairStart = -1;
                    result.Append(c);
                    result.Append(text[i + 1]);
                    i += 2;
                    continue;
                }
            }

            if (pairStart >= 0) pairStart = -1;
            result.Append(c);
            i++;
        }

        return result.ToString();
    }
}
