namespace ClashWidget.Models;

public class AppConfig
{
    public string ApiHost { get; set; } = "0.0.0.0";
    public int ApiPort { get; set; } = 9090;
    public string ApiSecret { get; set; } = "123456";
    public string TestUrl { get; set; } = "https://www.gstatic.com/generate_204";

    public string ApiBaseUrl => $"http://{ApiHost}:{ApiPort}";
}
