using System.Collections.Concurrent;
using CoreService.Devices;
using CoreService.Exceptions;
using CoreService.Models;
using Microsoft.Extensions.Logging;

namespace CoreService.Services;

/// <summary>
/// 设备管理器：注册、查询、连接/断开所有设备。
/// </summary>
public class DeviceManager : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, IDevice> _devices = new();
    private readonly ILogger<DeviceManager> _logger;

    public DeviceManager(ILogger<DeviceManager> logger)
    {
        _logger = logger;
    }

    /// <summary>注册设备。</summary>
    public void RegisterDevice(IDevice device)
    {
        if (!_devices.TryAdd(device.DeviceId, device))
            throw new InvalidOperationException($"Device '{device.DeviceId}' is already registered.");
        _logger.LogInformation("Device registered: {DeviceId} ({Name}, {Type})",
            device.DeviceId, device.Name, device.DeviceType);
    }

    /// <summary>注销设备。</summary>
    public async Task UnregisterDeviceAsync(string deviceId)
    {
        if (_devices.TryRemove(deviceId, out var device))
        {
            await device.DisconnectAsync();
            _logger.LogInformation("Device unregistered: {DeviceId}", deviceId);
        }
    }

    /// <summary>获取设备实例，不存在返回 null。</summary>
    public IDevice? GetDevice(string deviceId)
    {
        _devices.TryGetValue(deviceId, out var device);
        return device;
    }

    /// <summary>获取设备实例，不存在则抛异常。</summary>
    public IDevice GetDeviceOrThrow(string deviceId)
    {
        return GetDevice(deviceId)
            ?? throw new DeviceException(deviceId, $"Device '{deviceId}' not found.");
    }

    /// <summary>获取所有已注册设备。</summary>
    public IReadOnlyCollection<IDevice> GetAllDevices() => _devices.Values.ToList().AsReadOnly();

    /// <summary>按类型获取设备。</summary>
    public IReadOnlyCollection<IDevice> GetDevicesByType(DeviceType type) =>
        _devices.Values.Where(d => d.DeviceType == type).ToList().AsReadOnly();

    /// <summary>连接所有设备。</summary>
    public async Task<Dictionary<string, bool>> ConnectAllAsync(CancellationToken ct = default)
    {
        var results = new Dictionary<string, bool>();
        var tasks = _devices.Values.Select(async d =>
        {
            try
            {
                bool ok = await d.ConnectAsync(ct);
                return (d.DeviceId, ok);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect device {DeviceId}", d.DeviceId);
                return (d.DeviceId, false);
            }
        });

        foreach (var (deviceId, ok) in await Task.WhenAll(tasks))
            results[deviceId] = ok;

        int success = results.Count(kv => kv.Value);
        _logger.LogInformation("Connected {Success}/{Total} devices", success, results.Count);
        return results;
    }

    /// <summary>断开所有设备。</summary>
    public async Task DisconnectAllAsync(CancellationToken ct = default)
    {
        var tasks = _devices.Values.Select(async d =>
        {
            try { await d.DisconnectAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Error disconnecting {DeviceId}", d.DeviceId); }
        });
        await Task.WhenAll(tasks);
        _logger.LogInformation("All devices disconnected");
    }

    /// <summary>获取所有设备的状态摘要。</summary>
    public async Task<Dictionary<string, Dictionary<string, object>>> GetAllStatusAsync(CancellationToken ct = default)
    {
        var statusMap = new Dictionary<string, Dictionary<string, object>>();
        foreach (var device in _devices.Values)
        {
            statusMap[device.DeviceId] = await device.GetStatusAsync(ct);
        }
        return statusMap;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAllAsync();
        foreach (var device in _devices.Values)
            await device.DisposeAsync();
        _devices.Clear();
        GC.SuppressFinalize(this);
    }
}
