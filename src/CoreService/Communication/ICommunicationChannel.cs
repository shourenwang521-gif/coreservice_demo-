namespace CoreService.Communication;

/// <summary>
/// 通用通讯通道抽象接口，支持多种底层协议。
/// </summary>
public interface ICommunicationChannel : IAsyncDisposable
{
    /// <summary>通道唯一标识。</summary>
    string ChannelId { get; }

    /// <summary>通道是否已连接。</summary>
    bool IsConnected { get; }

    /// <summary>建立连接。</summary>
    Task<bool> ConnectAsync(CancellationToken ct = default);

    /// <summary>断开连接。</summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>发送原始字节数据。</summary>
    Task<int> SendAsync(byte[] data, CancellationToken ct = default);

    /// <summary>接收原始字节数据。</summary>
    Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken ct = default);

    /// <summary>发送并等待回复（请求-响应模式）。</summary>
    Task<byte[]> SendAndReceiveAsync(byte[] data, TimeSpan timeout, CancellationToken ct = default);
}
