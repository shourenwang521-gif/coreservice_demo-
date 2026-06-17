using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace CoreService.Communication;

/// <summary>
/// 基于 TCP Socket 的通讯通道。
/// </summary>
public class TcpChannel : ICommunicationChannel
{
    private readonly TcpChannelOptions _options;
    private readonly ILogger<TcpChannel> _logger;
    private TcpClient? _client;
    private NetworkStream? _stream;

    public string ChannelId { get; }
    public bool IsConnected => _client?.Connected ?? false;

    public TcpChannel(string channelId, TcpChannelOptions options, ILogger<TcpChannel> logger)
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
            cts.CancelAfter(_options.ConnectTimeout);
            await _client.ConnectAsync(_options.Host, _options.Port, cts.Token);
            _stream = _client.GetStream();
            _logger.LogInformation("TCP channel {ChannelId} connected to {Host}:{Port}",
                ChannelId, _options.Host, _options.Port);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP channel {ChannelId} failed to connect", ChannelId);
            return false;
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_stream != null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }
        _client?.Dispose();
        _client = null;
        _logger.LogInformation("TCP channel {ChannelId} disconnected", ChannelId);
    }

    public async Task<int> SendAsync(byte[] data, CancellationToken ct = default)
    {
        if (_stream == null) throw new InvalidOperationException("Channel not connected.");
        await _stream.WriteAsync(data, ct);
        await _stream.FlushAsync(ct);
        return data.Length;
    }

    public async Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken ct = default)
    {
        if (_stream == null) throw new InvalidOperationException("Channel not connected.");
        var buffer = new byte[bufferSize];
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_options.ReadTimeout);
        int bytesRead = await _stream.ReadAsync(buffer.AsMemory(0, bufferSize), cts.Token);
        return buffer[..bytesRead];
    }

    public async Task<byte[]> SendAndReceiveAsync(byte[] data, TimeSpan timeout, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        await SendAsync(data, cts.Token);
        return await ReceiveAsync(_options.ReceiveBufferSize, cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        GC.SuppressFinalize(this);
    }
}
