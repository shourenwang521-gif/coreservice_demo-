using System.Text;
using Microsoft.Extensions.Logging;

namespace CoreService.Communication;

/// <summary>
/// 基于 HTTP/REST 的通讯通道（适用于 RESTful 设备接口）。
/// </summary>
public class HttpChannel : ICommunicationChannel
{
    private readonly HttpChannelOptions _options;
    private readonly ILogger<HttpChannel> _logger;
    private HttpClient? _httpClient;

    public string ChannelId { get; }
    public bool IsConnected => _httpClient != null;

    public HttpChannel(string channelId, HttpChannelOptions options, ILogger<HttpChannel> logger)
    {
        ChannelId = channelId;
        _options = options;
        _logger = logger;
    }

    public Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_options.BaseUrl),
            Timeout = _options.Timeout,
        };
        foreach (var header in _options.DefaultHeaders)
        {
            _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
        _logger.LogInformation("HTTP channel {ChannelId} initialized for {BaseUrl}",
            ChannelId, _options.BaseUrl);
        return Task.FromResult(true);
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _httpClient?.Dispose();
        _httpClient = null;
        return Task.CompletedTask;
    }

    public async Task<int> SendAsync(byte[] data, CancellationToken ct = default)
    {
        if (_httpClient == null) throw new InvalidOperationException("Not connected.");
        var content = new ByteArrayContent(data);
        var response = await _httpClient.PostAsync("/api/command", content, ct);
        response.EnsureSuccessStatusCode();
        return data.Length;
    }

    public async Task<byte[]> ReceiveAsync(int bufferSize = 4096, CancellationToken ct = default)
    {
        if (_httpClient == null) throw new InvalidOperationException("Not connected.");
        var response = await _httpClient.GetAsync("/api/status", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<byte[]> SendAndReceiveAsync(byte[] data, TimeSpan timeout, CancellationToken ct = default)
    {
        if (_httpClient == null) throw new InvalidOperationException("Not connected.");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        var content = new ByteArrayContent(data);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        var response = await _httpClient.PostAsync("/api/command", content, cts.Token);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cts.Token);
    }

    /// <summary>发送 JSON 请求并获取 JSON 响应。</summary>
    public async Task<string> SendJsonAsync(string endpoint, string json, CancellationToken ct = default)
    {
        if (_httpClient == null) throw new InvalidOperationException("Not connected.");
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        GC.SuppressFinalize(this);
    }
}
