using System.Net;
using System.Net.Sockets;
using System.Text;
using CoreService.Models;
using Microsoft.Extensions.Logging;

namespace CoreService.Services;

/// <summary>
/// TCP 服务端：监听上位机指令，支持通过 TCP 协议远程控制实验启动/停止/暂停/恢复。
/// 协议格式：纯文本行命令（UTF-8，\n 结尾）。
/// 支持命令：START [workflowId]、STOP、PAUSE、RESUME、STATUS、QUIT
/// </summary>
public class ExperimentTcpServer : IAsyncDisposable
{
    private readonly IExperimentController _controller;
    private readonly ILogger<ExperimentTcpServer> _logger;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public int Port { get; }

    public ExperimentTcpServer(
        IExperimentController controller,
        int port,
        ILogger<ExperimentTcpServer> logger)
    {
        _controller = controller;
        Port = port;
        _logger = logger;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start();
        _logger.LogInformation("ExperimentTcpServer listening on port {Port}", Port);
        _listenTask = AcceptClientsAsync(_cts.Token);
    }

    private async Task AcceptClientsAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var client = await _listener!.AcceptTcpClientAsync(ct);
                _ = HandleClientAsync(client, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP server error");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        _logger.LogInformation("Client connected: {Endpoint}", endpoint);

        try
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            await using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            await writer.WriteLineAsync("CORESERVICE READY");

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;

                var response = await ProcessCommandAsync(line.Trim());
                await writer.WriteLineAsync(response);

                if (line.Trim().Equals("QUIT", StringComparison.OrdinalIgnoreCase))
                    break;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Client {Endpoint} error", endpoint);
        }
        finally
        {
            client.Dispose();
            _logger.LogInformation("Client disconnected: {Endpoint}", endpoint);
        }
    }

    private async Task<string> ProcessCommandAsync(string command)
    {
        _logger.LogInformation("Received command: {Command}", command);

        var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "ERR EMPTY_COMMAND";

        var cmd = parts[0].ToUpperInvariant();
        var arg = parts.Length > 1 ? parts[1] : null;

        switch (cmd)
        {
            case "START":
                if (string.IsNullOrEmpty(arg))
                    return "ERR START requires workflow ID";
                var started = await _controller.StartAsync(arg);
                return started ? "OK STARTED" : $"ERR Cannot start (current status: {_controller.Status})";

            case "STOP":
                var stopped = await _controller.StopAsync();
                return stopped ? "OK STOPPED" : $"ERR Cannot stop (current status: {_controller.Status})";

            case "PAUSE":
                var paused = await _controller.PauseAsync();
                return paused ? "OK PAUSED" : $"ERR Cannot pause (current status: {_controller.Status})";

            case "RESUME":
                var resumed = await _controller.ResumeAsync();
                return resumed ? "OK RESUMED" : $"ERR Cannot resume (current status: {_controller.Status})";

            case "STATUS":
                var status = _controller.Status;
                var wfId = _controller.CurrentWorkflowId ?? "none";
                return $"OK STATUS={status} WORKFLOW={wfId}";

            case "QUIT":
                return "OK BYE";

            default:
                return $"ERR UNKNOWN_COMMAND: {cmd}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();
        if (_listenTask != null)
        {
            try { await _listenTask; }
            catch { /* expected */ }
        }
        _cts?.Dispose();
    }
}
