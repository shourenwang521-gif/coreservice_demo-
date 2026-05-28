using CoreService.Communication;
using CoreService.Models;
using Microsoft.Extensions.Logging;

namespace CoreService.Devices;

/// <summary>
/// 视觉系统设备驱动：支持 capture / inspect / get_result / set_recipe / trigger。
/// </summary>
public class VisionDevice : BaseDevice
{
    private string _currentRecipe = "default";
    private Dictionary<string, object>? _lastResult;

    private static readonly HashSet<string> SupportedActions = new()
    {
        "capture", "inspect", "get_result", "set_recipe", "trigger",
    };

    public VisionDevice(DeviceInfo info, ICommunicationChannel? channel, ILogger<VisionDevice> logger)
        : base(info, channel, logger) { }

    protected override async Task<CommandResult> OnExecuteCommandAsync(DeviceCommand command, CancellationToken ct)
    {
        if (!SupportedActions.Contains(command.Action))
            return CommandResult.Fail($"Unsupported action: {command.Action}");

        return command.Action switch
        {
            "capture" => await CaptureAsync(ct),
            "inspect" => await InspectAsync(command.Parameters, ct),
            "get_result" => GetLastResult(),
            "set_recipe" => SetRecipe(command.Parameters),
            "trigger" => await TriggerAsync(ct),
            _ => CommandResult.Fail("Unknown action"),
        };
    }

    private async Task<CommandResult> CaptureAsync(CancellationToken ct)
    {
        await Task.Delay(30, ct);
        return CommandResult.Ok(new() { ["image_id"] = Guid.NewGuid().ToString(), ["captured"] = true });
    }

    private async Task<CommandResult> InspectAsync(Dictionary<string, object> p, CancellationToken ct)
    {
        await Task.Delay(50, ct);
        var rng = new Random();
        _lastResult = new Dictionary<string, object>
        {
            ["inspection_id"] = Guid.NewGuid().ToString(),
            ["recipe"] = _currentRecipe,
            ["passed"] = rng.NextDouble() > 0.1,
            ["defects_found"] = rng.Next(0, 3),
            ["confidence"] = Math.Round(rng.NextDouble() * 0.3 + 0.7, 3),
        };
        return CommandResult.Ok(new() { ["result"] = _lastResult });
    }

    private CommandResult GetLastResult() =>
        CommandResult.Ok(new() { ["result"] = (object?)_lastResult ?? "no_result" });

    private CommandResult SetRecipe(Dictionary<string, object> p)
    {
        _currentRecipe = p.GetValueOrDefault("recipe", "default").ToString()!;
        return CommandResult.Ok(new() { ["recipe"] = _currentRecipe });
    }

    private async Task<CommandResult> TriggerAsync(CancellationToken ct)
    {
        await CaptureAsync(ct);
        return await InspectAsync(new(), ct);
    }

    public override Task<Dictionary<string, object>> GetStatusAsync(CancellationToken ct = default) =>
        Task.FromResult(new Dictionary<string, object>
        {
            ["device_id"] = DeviceId,
            ["status"] = Status.ToString(),
            ["current_recipe"] = _currentRecipe,
            ["has_last_result"] = _lastResult != null,
        });
}
