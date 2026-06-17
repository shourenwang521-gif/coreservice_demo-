using CoreService.Models;

namespace CoreService.Devices;

/// <summary>
/// 设备驱动通用接口。
/// </summary>
public interface IDevice : IAsyncDisposable
{
    string DeviceId { get; }
    string Name { get; }
    DeviceType DeviceType { get; }
    DeviceStatus Status { get; }
    DeviceInfo Info { get; }

    Task<bool> ConnectAsync(CancellationToken ct = default);
    Task<bool> DisconnectAsync(CancellationToken ct = default);
    Task<CommandResult> ExecuteCommandAsync(DeviceCommand command, CancellationToken ct = default);
    Task<CommandResult> SendCommandAsync(string action, Dictionary<string, object>? parameters = null, CancellationToken ct = default);
    Task<bool> ResetAsync(CancellationToken ct = default);
    Task<Dictionary<string, object>> GetStatusAsync(CancellationToken ct = default);
}
