namespace CoreService.Models;

/// <summary>
/// 设备静态/动态信息描述。
/// </summary>
public class DeviceInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Offline;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public string ComPort { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime? LastHeartbeat { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public bool IsAvailable => Status is DeviceStatus.Idle or DeviceStatus.Running;
}
