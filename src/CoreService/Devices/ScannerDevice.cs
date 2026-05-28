using CoreService.Communication;
using CoreService.Models;
using Microsoft.Extensions.Logging;

namespace CoreService.Devices;

/// <summary>
/// 扫码枪/条码读取器设备驱动：支持 scan / configure / get_last_scan / clear_buffer。
/// </summary>
public class ScannerDevice : BaseDevice
{
    private readonly List<Dictionary<string, object>> _scanBuffer = new();
    private string _scanMode = "barcode";
    private int _resolution = 300;

    private static readonly HashSet<string> SupportedActions = new()
    {
        "scan", "configure", "get_last_scan", "clear_buffer",
    };

    public ScannerDevice(DeviceInfo info, ICommunicationChannel? channel, ILogger<ScannerDevice> logger)
        : base(info, channel, logger) { }

    protected override async Task<CommandResult> OnExecuteCommandAsync(DeviceCommand command, CancellationToken ct)
    {
        if (!SupportedActions.Contains(command.Action))
            return CommandResult.Fail($"Unsupported action: {command.Action}");

        return command.Action switch
        {
            "scan" => await ScanAsync(command.Parameters, ct),
            "configure" => Configure(command.Parameters),
            "get_last_scan" => GetLastScan(),
            "clear_buffer" => ClearBuffer(),
            _ => CommandResult.Fail("Unknown action"),
        };
    }

    private async Task<CommandResult> ScanAsync(Dictionary<string, object> p, CancellationToken ct)
    {
        if (Channel != null)
        {
            var triggerData = System.Text.Encoding.UTF8.GetBytes("TRIGGER");
            var response = await Channel.SendAndReceiveAsync(triggerData, TimeSpan.FromSeconds(5), ct);
            var scannedData = System.Text.Encoding.UTF8.GetString(response);
            var result = new Dictionary<string, object>
            {
                ["scan_id"] = Guid.NewGuid().ToString(),
                ["data"] = scannedData,
                ["scan_type"] = _scanMode,
            };
            _scanBuffer.Add(result);
            return CommandResult.Ok(new() { ["scan_result"] = result });
        }

        await Task.Delay(20, ct);
        var simResult = new Dictionary<string, object>
        {
            ["scan_id"] = Guid.NewGuid().ToString(),
            ["data"] = p.GetValueOrDefault("expected_data", $"ITEM-{Guid.NewGuid().ToString()[..8].ToUpper()}"),
            ["scan_type"] = _scanMode,
            ["quality"] = 0.98,
        };
        _scanBuffer.Add(simResult);
        return CommandResult.Ok(new() { ["scan_result"] = simResult });
    }

    private CommandResult Configure(Dictionary<string, object> p)
    {
        if (p.TryGetValue("mode", out var m)) _scanMode = m.ToString()!;
        if (p.TryGetValue("resolution", out var r)) _resolution = Convert.ToInt32(r);
        return CommandResult.Ok(new() { ["mode"] = _scanMode, ["resolution"] = _resolution });
    }

    private CommandResult GetLastScan()
    {
        var last = _scanBuffer.Count > 0 ? _scanBuffer[^1] : null;
        return CommandResult.Ok(new() { ["scan_result"] = (object?)last ?? "none" });
    }

    private CommandResult ClearBuffer()
    {
        int count = _scanBuffer.Count;
        _scanBuffer.Clear();
        return CommandResult.Ok(new() { ["cleared_count"] = count });
    }

    public override Task<Dictionary<string, object>> GetStatusAsync(CancellationToken ct = default) =>
        Task.FromResult(new Dictionary<string, object>
        {
            ["device_id"] = DeviceId,
            ["status"] = Status.ToString(),
            ["scan_mode"] = _scanMode,
            ["resolution"] = _resolution,
            ["buffer_size"] = _scanBuffer.Count,
        });
}
