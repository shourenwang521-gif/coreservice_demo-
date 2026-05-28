using CoreService.Communication;
using CoreService.Models;
using Microsoft.Extensions.Logging;

namespace CoreService.Devices;

/// <summary>
/// 机器人设备驱动：支持 move / pick / place / home / set_speed / get_position / emergency_stop。
/// </summary>
public class RobotDevice : BaseDevice
{
    private readonly Dictionary<string, double> _position = new() { ["x"] = 0, ["y"] = 0, ["z"] = 0 };
    private double _speed = 100.0;
    private bool _holdingItem;

    private static readonly HashSet<string> SupportedActions = new()
    {
        "move", "pick", "place", "home", "set_speed", "get_position", "emergency_stop",
    };

    public RobotDevice(DeviceInfo info, ICommunicationChannel? channel, ILogger<RobotDevice> logger)
        : base(info, channel, logger) { }

    protected override async Task<CommandResult> OnExecuteCommandAsync(DeviceCommand command, CancellationToken ct)
    {
        if (!SupportedActions.Contains(command.Action))
            return CommandResult.Fail($"Unsupported action: {command.Action}");

        return command.Action switch
        {
            "move" => await MoveAsync(command.Parameters, ct),
            "pick" => await PickAsync(ct),
            "place" => await PlaceAsync(ct),
            "home" => await HomeAsync(ct),
            "set_speed" => SetSpeed(command.Parameters),
            "get_position" => GetPosition(),
            "emergency_stop" => EmergencyStop(),
            _ => CommandResult.Fail("Unknown action"),
        };
    }

    private async Task<CommandResult> MoveAsync(Dictionary<string, object> p, CancellationToken ct)
    {
        if (p.TryGetValue("x", out var x)) _position["x"] = Convert.ToDouble(x);
        if (p.TryGetValue("y", out var y)) _position["y"] = Convert.ToDouble(y);
        if (p.TryGetValue("z", out var z)) _position["z"] = Convert.ToDouble(z);

        if (Channel != null)
        {
            var data = System.Text.Encoding.UTF8.GetBytes($"MOVE:{_position["x"]},{_position["y"]},{_position["z"]}");
            await Channel.SendAsync(data, ct);
        }
        await Task.Delay(10, ct);
        return CommandResult.Ok(new Dictionary<string, object> { ["position"] = new Dictionary<string, double>(_position) });
    }

    private async Task<CommandResult> PickAsync(CancellationToken ct)
    {
        await Task.Delay(10, ct);
        _holdingItem = true;
        return CommandResult.Ok(new() { ["holding_item"] = true });
    }

    private async Task<CommandResult> PlaceAsync(CancellationToken ct)
    {
        await Task.Delay(10, ct);
        _holdingItem = false;
        return CommandResult.Ok(new() { ["holding_item"] = false });
    }

    private async Task<CommandResult> HomeAsync(CancellationToken ct)
    {
        _position["x"] = 0; _position["y"] = 0; _position["z"] = 0;
        await Task.Delay(10, ct);
        return CommandResult.Ok(new() { ["position"] = new Dictionary<string, double>(_position) });
    }

    private CommandResult SetSpeed(Dictionary<string, object> p)
    {
        if (p.TryGetValue("speed", out var s)) _speed = Convert.ToDouble(s);
        return CommandResult.Ok(new() { ["speed"] = _speed });
    }

    private CommandResult GetPosition() =>
        CommandResult.Ok(new Dictionary<string, object> { ["position"] = new Dictionary<string, double>(_position) });

    private CommandResult EmergencyStop()
    {
        SetStatus(DeviceStatus.Paused);
        return CommandResult.Ok(new() { ["stopped"] = true, ["position"] = new Dictionary<string, double>(_position) });
    }

    public override Task<Dictionary<string, object>> GetStatusAsync(CancellationToken ct = default) =>
        Task.FromResult(new Dictionary<string, object>
        {
            ["device_id"] = DeviceId,
            ["status"] = Status.ToString(),
            ["position"] = new Dictionary<string, double>(_position),
            ["speed"] = _speed,
            ["holding_item"] = _holdingItem,
        });
}
