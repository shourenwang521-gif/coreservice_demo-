using CoreService.Communication;
using CoreService.Models;
using Microsoft.Extensions.Logging;

namespace CoreService.Devices;

/// <summary>
/// 传送带设备驱动：支持 start / stop / set_speed / reverse / get_position。
/// </summary>
public class ConveyorDevice : BaseDevice
{
    private double _speed;
    private string _direction = "forward";
    private double _position;
    private bool _isRunning;

    private static readonly HashSet<string> SupportedActions = new()
    {
        "start", "stop", "set_speed", "reverse", "get_position",
    };

    public ConveyorDevice(DeviceInfo info, ICommunicationChannel? channel, ILogger<ConveyorDevice> logger)
        : base(info, channel, logger) { }

    protected override async Task<CommandResult> OnExecuteCommandAsync(DeviceCommand command, CancellationToken ct)
    {
        if (!SupportedActions.Contains(command.Action))
            return CommandResult.Fail($"Unsupported action: {command.Action}");

        var result = command.Action switch
        {
            "start" => await StartAsync(command.Parameters, ct),
            "stop" => await StopAsync(ct),
            "set_speed" => SetBeltSpeed(command.Parameters),
            "reverse" => Reverse(),
            "get_position" => CommandResult.Ok(new() { ["position"] = _position }),
            _ => CommandResult.Fail("Unknown action"),
        };

        if (!_isRunning && Status == DeviceStatus.Running)
            SetStatus(DeviceStatus.Idle);

        return result;
    }

    private async Task<CommandResult> StartAsync(Dictionary<string, object> p, CancellationToken ct)
    {
        _speed = p.TryGetValue("speed", out var s) ? Convert.ToDouble(s) : 50.0;
        _isRunning = true;
        await Task.Delay(10, ct);
        return CommandResult.Ok(new() { ["running"] = true, ["speed"] = _speed, ["direction"] = _direction });
    }

    private async Task<CommandResult> StopAsync(CancellationToken ct)
    {
        _isRunning = false;
        _speed = 0;
        await Task.Delay(10, ct);
        return CommandResult.Ok(new() { ["running"] = false, ["speed"] = 0.0 });
    }

    private CommandResult SetBeltSpeed(Dictionary<string, object> p)
    {
        _speed = p.TryGetValue("speed", out var s) ? Convert.ToDouble(s) : 50.0;
        return CommandResult.Ok(new() { ["speed"] = _speed });
    }

    private CommandResult Reverse()
    {
        _direction = _direction == "forward" ? "backward" : "forward";
        return CommandResult.Ok(new() { ["direction"] = _direction });
    }

    public override Task<Dictionary<string, object>> GetStatusAsync(CancellationToken ct = default) =>
        Task.FromResult(new Dictionary<string, object>
        {
            ["device_id"] = DeviceId,
            ["status"] = Status.ToString(),
            ["speed"] = _speed,
            ["direction"] = _direction,
            ["position"] = _position,
            ["is_running"] = _isRunning,
        });
}
