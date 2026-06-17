using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace CoreService.Communication;

/// <summary>
/// 基于 Modbus TCP 协议的通讯通道。
/// 实现最常用的功能码：03(读保持寄存器) / 06(写单个寄存器) / 16(写多个寄存器)。
/// </summary>
public class ModbusTcpChannel : ICommunicationChannel
{
    private readonly ModbusChannelOptions _options;
    private readonly ILogger<ModbusTcpChannel> _logger;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private ushort _transactionId;

    public string ChannelId { get; }
    public bool IsConnected => _client?.Connected ?? false;

    public ModbusTcpChannel(string channelId, ModbusChannelOptions options, ILogger<ModbusTcpChannel> logger)
    {
        ChannelId = channelId;
        _options = options;
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        try
        {
            _client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_options.Timeout);
            await _client.ConnectAsync(_options.Host, _options.Port, cts.Token);
            _stream = _client.GetStream();
            _logger.LogInformation("Modbus channel {ChannelId} connected to {Host}:{Port}",
                ChannelId, _options.Host, _options.Port);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Modbus channel {ChannelId} connection failed", ChannelId);
            return false;
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_stream != null) await _stream.DisposeAsync();
        _client?.Dispose();
        _stream = null;
        _client = null;
    }

    public async Task<int> SendAsync(byte[] data, CancellationToken ct = default)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected.");
        await _stream.WriteAsync(data, ct);
        return data.Length;
    }

    public async Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken ct = default)
    {
        if (_stream == null) throw new InvalidOperationException("Not connected.");
        var buffer = new byte[bufferSize];
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_options.Timeout);
        int read = await _stream.ReadAsync(buffer.AsMemory(0, bufferSize), cts.Token);
        return buffer[..read];
    }

    public async Task<byte[]> SendAndReceiveAsync(byte[] data, TimeSpan timeout, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        await SendAsync(data, cts.Token);
        return await ReceiveAsync(256, cts.Token);
    }

    /// <summary>读保持寄存器 (FC 03)。</summary>
    public async Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count, CancellationToken ct = default)
    {
        var request = BuildModbusRequest(0x03, startAddress, count);
        var response = await SendAndReceiveAsync(request, _options.Timeout, ct);
        return ParseRegisterResponse(response);
    }

    /// <summary>写单个寄存器 (FC 06)。</summary>
    public async Task WriteSingleRegisterAsync(ushort address, ushort value, CancellationToken ct = default)
    {
        var request = BuildModbusRequest(0x06, address, value);
        await SendAndReceiveAsync(request, _options.Timeout, ct);
    }

    private byte[] BuildModbusRequest(byte functionCode, ushort addr, ushort value)
    {
        _transactionId++;
        var frame = new byte[12];
        // MBAP Header
        frame[0] = (byte)(_transactionId >> 8);
        frame[1] = (byte)(_transactionId & 0xFF);
        // Protocol ID = 0
        frame[4] = 0; frame[5] = 6; // Length
        frame[6] = _options.SlaveId;
        frame[7] = functionCode;
        frame[8] = (byte)(addr >> 8);
        frame[9] = (byte)(addr & 0xFF);
        frame[10] = (byte)(value >> 8);
        frame[11] = (byte)(value & 0xFF);
        return frame;
    }

    private static ushort[] ParseRegisterResponse(byte[] response)
    {
        if (response.Length < 9) return Array.Empty<ushort>();
        int byteCount = response[8];
        var registers = new ushort[byteCount / 2];
        for (int i = 0; i < registers.Length; i++)
        {
            registers[i] = (ushort)((response[9 + i * 2] << 8) | response[10 + i * 2]);
        }
        return registers;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        GC.SuppressFinalize(this);
    }
}
