using CoreService.Communication;
using CoreService.Models;
using Microsoft.Extensions.Logging;

namespace CoreService.Devices;

/// <summary>
/// 测试仪器设备驱动：支持 run_test / get_results / calibrate / configure_channel / self_test。
/// </summary>
public class TestDevice : BaseDevice
{
    private readonly List<Dictionary<string, object>> _testResults = new();
    private readonly Dictionary<string, Dictionary<string, object>> _channels = new();
    private bool _calibrated;

    private static readonly HashSet<string> SupportedActions = new()
    {
        "run_test", "get_results", "calibrate", "configure_channel", "self_test",
    };

    public TestDevice(DeviceInfo info, ICommunicationChannel? channel, ILogger<TestDevice> logger)
        : base(info, channel, logger) { }

    protected override async Task<CommandResult> OnExecuteCommandAsync(DeviceCommand command, CancellationToken ct)
    {
        if (!SupportedActions.Contains(command.Action))
            return CommandResult.Fail($"Unsupported action: {command.Action}");

        return command.Action switch
        {
            "run_test" => await RunTestAsync(command.Parameters, ct),
            "get_results" => GetResults(),
            "calibrate" => await CalibrateAsync(ct),
            "configure_channel" => ConfigureChannel(command.Parameters),
            "self_test" => await SelfTestAsync(ct),
            _ => CommandResult.Fail("Unknown action"),
        };
    }

    private async Task<CommandResult> RunTestAsync(Dictionary<string, object> p, CancellationToken ct)
    {
        var testType = p.GetValueOrDefault("test_type", "voltage").ToString()!;
        await Task.Delay(20, ct);

        var rng = new Random();
        var measurement = new Dictionary<string, object>
        {
            ["test_id"] = Guid.NewGuid().ToString(),
            ["test_type"] = testType,
            ["value"] = Math.Round(rng.NextDouble() * 100, 4),
            ["unit"] = p.GetValueOrDefault("unit", "V").ToString()!,
            ["passed"] = true,
        };

        if (p.TryGetValue("min_threshold", out var minVal))
        {
            double min = Convert.ToDouble(minVal);
            measurement["passed"] = Convert.ToDouble(measurement["value"]) >= min;
        }
        if (p.TryGetValue("max_threshold", out var maxVal))
        {
            double max = Convert.ToDouble(maxVal);
            measurement["passed"] = (bool)measurement["passed"] && Convert.ToDouble(measurement["value"]) <= max;
        }

        _testResults.Add(measurement);
        return CommandResult.Ok(new() { ["measurement"] = measurement });
    }

    private CommandResult GetResults() =>
        CommandResult.Ok(new() { ["results"] = _testResults.ToList(), ["count"] = _testResults.Count });

    private async Task<CommandResult> CalibrateAsync(CancellationToken ct)
    {
        await Task.Delay(50, ct);
        _calibrated = true;
        return CommandResult.Ok(new() { ["calibrated"] = true });
    }

    private CommandResult ConfigureChannel(Dictionary<string, object> p)
    {
        var channelId = p.GetValueOrDefault("channel_id", "CH1").ToString()!;
        var config = p.Where(kv => kv.Key != "channel_id").ToDictionary(kv => kv.Key, kv => kv.Value);
        _channels[channelId] = config;
        return CommandResult.Ok(new() { ["channel_id"] = channelId, ["config"] = config });
    }

    private async Task<CommandResult> SelfTestAsync(CancellationToken ct)
    {
        await Task.Delay(20, ct);
        return CommandResult.Ok(new() { ["self_test_passed"] = true, ["calibrated"] = _calibrated });
    }

    public override Task<Dictionary<string, object>> GetStatusAsync(CancellationToken ct = default) =>
        Task.FromResult(new Dictionary<string, object>
        {
            ["device_id"] = DeviceId,
            ["status"] = Status.ToString(),
            ["calibrated"] = _calibrated,
            ["channels"] = _channels.ToDictionary(kv => kv.Key, kv => (object)kv.Value),
            ["total_tests_run"] = _testResults.Count,
        });
}
