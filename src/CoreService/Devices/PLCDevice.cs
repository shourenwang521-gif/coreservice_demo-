using CoreService.Communication;
using CoreService.Models;
using Microsoft.Extensions.Logging;

namespace CoreService.Devices;

/// <summary>
/// PLC 设备驱动：支持 read_register / write_register / read_coil / write_coil / read_input。
/// </summary>
public class PLCDevice : BaseDevice
{
    private readonly Dictionary<int, int> _registers = new();
    private readonly Dictionary<int, bool> _coils = new();

    private static readonly HashSet<string> SupportedActions = new()
    {
        "read_register", "write_register", "read_coil", "write_coil", "read_input",
    };

    public PLCDevice(DeviceInfo info, ICommunicationChannel? channel, ILogger<PLCDevice> logger)
        : base(info, channel, logger) { }

    protected override async Task<CommandResult> OnExecuteCommandAsync(DeviceCommand command, CancellationToken ct)
    {
        if (!SupportedActions.Contains(command.Action))
            return CommandResult.Fail($"Unsupported action: {command.Action}");

        return command.Action switch
        {
            "read_register" => await ReadRegisterAsync(command.Parameters, ct),
            "write_register" => await WriteRegisterAsync(command.Parameters, ct),
            "read_coil" => ReadCoil(command.Parameters),
            "write_coil" => WriteCoil(command.Parameters),
            "read_input" => ReadInput(command.Parameters),
            _ => CommandResult.Fail("Unknown action"),
        };
    }

    private async Task<CommandResult> ReadRegisterAsync(Dictionary<string, object> p, CancellationToken ct)
    {
        int address = Convert.ToInt32(p["address"]);
        int count = p.TryGetValue("count", out var c) ? Convert.ToInt32(c) : 1;

        if (Channel is ModbusTcpChannel modbus)
        {
            var values = await modbus.ReadHoldingRegistersAsync((ushort)address, (ushort)count, ct);
            return CommandResult.Ok(new() { ["values"] = values.Select(v => (object)(int)v).ToList() });
        }

        var simValues = Enumerable.Range(address, count)
            .Select(a => (object)_registers.GetValueOrDefault(a, 0))
            .ToList();
        return CommandResult.Ok(new() { ["values"] = simValues });
    }

    private async Task<CommandResult> WriteRegisterAsync(Dictionary<string, object> p, CancellationToken ct)
    {
        int address = Convert.ToInt32(p["address"]);
        int value = Convert.ToInt32(p["value"]);

        if (Channel is ModbusTcpChannel modbus)
        {
            await modbus.WriteSingleRegisterAsync((ushort)address, (ushort)value, ct);
        }

        _registers[address] = value;
        return CommandResult.Ok(new() { ["address"] = address, ["value"] = value });
    }

    private CommandResult ReadCoil(Dictionary<string, object> p)
    {
        int address = Convert.ToInt32(p["address"]);
        bool val = _coils.GetValueOrDefault(address, false);
        return CommandResult.Ok(new() { ["address"] = address, ["value"] = val });
    }

    private CommandResult WriteCoil(Dictionary<string, object> p)
    {
        int address = Convert.ToInt32(p["address"]);
        bool val = Convert.ToBoolean(p["value"]);
        _coils[address] = val;
        return CommandResult.Ok(new() { ["address"] = address, ["value"] = val });
    }

    private CommandResult ReadInput(Dictionary<string, object> p)
    {
        int address = Convert.ToInt32(p["address"]);
        return CommandResult.Ok(new() { ["address"] = address, ["value"] = _registers.GetValueOrDefault(address, 0) });
    }

    public override Task<Dictionary<string, object>> GetStatusAsync(CancellationToken ct = default) =>
        Task.FromResult(new Dictionary<string, object>
        {
            ["device_id"] = DeviceId,
            ["status"] = Status.ToString(),
            ["register_count"] = _registers.Count,
            ["coil_count"] = _coils.Count,
        });
}
