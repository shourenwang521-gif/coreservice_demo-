namespace CoreService.Models;

/// <summary>
/// 发送给设备的指令。
/// </summary>
public class DeviceCommand
{
    public string CommandId { get; set; } = Guid.NewGuid().ToString();
    public string DeviceId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int Priority { get; set; }
}
