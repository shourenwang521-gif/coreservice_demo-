namespace CoreService.Models;

/// <summary>
/// 设备生命周期状态。
/// </summary>
public enum DeviceStatus
{
    Offline,
    Initializing,
    Idle,
    Running,
    Paused,
    Error,
    Maintenance,
}
