using System.IO.Ports;
using Microsoft.Extensions.Logging;

namespace CoreService.Communication;

/// <summary>
/// 基于串口（RS232/RS485）的通讯通道。
/// </summary>
public class SerialPortChannel : ICommunicationChannel
{
    private readonly SerialChannelOptions _options;
    private readonly ILogger<SerialPortChannel> _logger;
    private SerialPort? _port;

    public string ChannelId { get; }
    public bool IsConnected => _port?.IsOpen ?? false;

    public SerialPortChannel(string channelId, SerialChannelOptions options, ILogger<SerialPortChannel> logger)
    {
        ChannelId = channelId;
        _options = options;
        _logger = logger;
    }

    public Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        try
        {
            _port = new SerialPort
            {
                PortName = _options.PortName,
                BaudRate = _options.BaudRate,
                DataBits = _options.DataBits,
                Parity = Enum.Parse<Parity>(_options.Parity),
                StopBits = Enum.Parse<StopBits>(_options.StopBits),
                ReadTimeout = (int)_options.ReadTimeout.TotalMilliseconds,
            };
            _port.Open();
            _logger.LogInformation("Serial channel {ChannelId} opened on {Port}",
                ChannelId, _options.PortName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Serial channel {ChannelId} failed to open", ChannelId);
            return Task.FromResult(false);
        }
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _port?.Close();
        _port?.Dispose();
        _port = null;
        _logger.LogInformation("Serial channel {ChannelId} closed", ChannelId);
        return Task.CompletedTask;
    }

    public Task<int> SendAsync(byte[] data, CancellationToken ct = default)
    {
        if (_port == null || !_port.IsOpen)
            throw new InvalidOperationException("Serial port not open.");
        _port.Write(data, 0, data.Length);
        return Task.FromResult(data.Length);
    }

    public Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken ct = default)
    {
        if (_port == null || !_port.IsOpen)
            throw new InvalidOperationException("Serial port not open.");
        var buffer = new byte[bufferSize];
        int bytesRead = _port.Read(buffer, 0, bufferSize);
        return Task.FromResult(buffer[..bytesRead]);
    }

    public async Task<byte[]> SendAndReceiveAsync(byte[] data, TimeSpan timeout, CancellationToken ct = default)
    {
        await SendAsync(data, ct);
        await Task.Delay(50, ct); // brief settling delay
        return await ReceiveAsync(4096, ct);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        GC.SuppressFinalize(this);
    }
}
