using CoreService.Communication;
using CoreService.Models;
using Microsoft.Extensions.Logging;

namespace CoreService.Devices;

/// <summary>
/// 夹爪设备驱动：支持 open / close / set_force / get_state。
/// </summary>
public class GripperDevice : BaseDevice
{
    private bool _isClosed;
    private double _force = 50.0;
    private double _position = 100.0; // 0=fully closed, 100=fully open

    private static readonly HashSet<string> SupportedActions = new()
    {
        "open", "close", "set_force", "get_state",
    };

    public GripperDevice(DeviceInfo info, ICommunicationChannel? channel, ILogger<GripperDevice> logger)
        : base(info, channel, logger) { }

    protected override async Task<CommandResult> OnExecuteCommandAsync(DeviceCommand command, CancellationToken ct)
    {
        if (!SupportedActions.Contains(command.Action))
            return CommandResult.Fail($"Unsupported action: {command.Action}");

        return command.Action switch
        {
            "open" => await OpenAsync(command.Parameters, ct),
            "close" => await CloseAsync(command.Parameters, ct),
            "set_force" => SetForce(command.Parameters),
            "get_state" => GetState(),
            _ => CommandResult.Fail("Unknown action"),
        };
    }

    private async Task<CommandResult> OpenAsync(Dictionary<string, object> p, CancellationToken ct)
    {
        _position = p.TryGetValue("position", out var pos) ? Convert.ToDouble(pos) : 100.0;
        _isClosed = false;
        await Task.Delay(10, ct);
        return CommandResult.Ok(new() { ["is_closed"] = false, ["position"] = _position });
    }

    private async Task<CommandResult> CloseAsync(Dictionary<string, object> p, CancellationToken ct)
    {
        _position = p.TryGetValue("position", out var pos) ? Convert.ToDouble(pos) : 0.0;
        _isClosed = true;
        await Task.Delay(10, ct);
        return CommandResult.Ok(new() { ["is_closed"] = true, ["position"] = _position });
    }

    private CommandResult SetForce(Dictionary<string, object> p)
    {
        _force = p.TryGetValue("force", out var f) ? Convert.ToDouble(f) : 50.0;
        return CommandResult.Ok(new() { ["force"] = _force });
    }

    private CommandResult GetState() =>
        CommandResult.Ok(new()
        {
            ["is_closed"] = _isClosed,
            ["position"] = _position,
            ["force"] = _force,
        });

    public override Task<Dictionary<string, object>> GetStatusAsync(CancellationToken ct = default) =>
        Task.FromResult(new Dictionary<string, object>
        {
            ["device_id"] = DeviceId,
            ["status"] = Status.ToString(),
            ["is_closed"] = _isClosed,
            ["position"] = _position,
            ["force"] = _force,
        });
}
