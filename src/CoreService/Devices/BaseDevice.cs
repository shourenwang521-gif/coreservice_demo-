using System.Diagnostics;
using CoreService.Communication;
using CoreService.Exceptions;
using CoreService.Models;
using Microsoft.Extensions.Logging;

namespace CoreService.Devices;

/// <summary>
/// 设备驱动基类，提供通用生命周期管理、通讯和异常处理。
/// </summary>
public abstract class BaseDevice : IDevice
{
    protected readonly ICommunicationChannel? Channel;
    protected readonly ILogger Logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly List<DeviceCommand> _commandHistory = new();

    public string DeviceId => Info.DeviceId;
    public string Name => Info.Name;
    public DeviceType DeviceType => Info.DeviceType;
    public DeviceStatus Status => Info.Status;
    public DeviceInfo Info { get; }

    protected BaseDevice(DeviceInfo info, ICommunicationChannel? channel, ILogger logger)
    {
        Info = info;
        Channel = channel;
        Logger = logger;
    }

    protected void SetStatus(DeviceStatus status, string errorMessage = "")
    {
        Info.Status = status;
        Info.ErrorMessage = errorMessage;
        Info.LastHeartbeat = DateTime.UtcNow;
        Logger.LogInformation("[{DeviceId}] {Name} -> {Status}", DeviceId, Name, status);
    }

    public virtual async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        SetStatus(DeviceStatus.Initializing);
        try
        {
            if (Channel != null)
            {
                bool connected = await Channel.ConnectAsync(ct);
                if (!connected)
                {
                    SetStatus(DeviceStatus.Error, "Channel connection failed");
                    return false;
                }
            }
            await OnConnectedAsync(ct);
            SetStatus(DeviceStatus.Idle);
            return true;
        }
        catch (Exception ex)
        {
            SetStatus(DeviceStatus.Error, ex.Message);
            throw new DeviceConnectionException(DeviceId, ex.Message, ex);
        }
    }

    public virtual async Task<bool> DisconnectAsync(CancellationToken ct = default)
    {
        try
        {
            await OnDisconnectingAsync(ct);
            if (Channel != null)
                await Channel.DisconnectAsync(ct);
            SetStatus(DeviceStatus.Offline);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{DeviceId}] Disconnect error", DeviceId);
            SetStatus(DeviceStatus.Offline);
            return false;
        }
    }

    public async Task<CommandResult> ExecuteCommandAsync(DeviceCommand command, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _commandHistory.Add(command);
            SetStatus(DeviceStatus.Running);

            var sw = Stopwatch.StartNew();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(command.Timeout);

            try
            {
                var result = await OnExecuteCommandAsync(command, cts.Token);
                sw.Stop();
                result.ElapsedTime = sw.Elapsed;
                SetStatus(DeviceStatus.Idle);
                return result;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                sw.Stop();
                SetStatus(DeviceStatus.Idle);
                throw; // propagate external cancellation
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                SetStatus(DeviceStatus.Error, "Command timed out");
                throw new DeviceTimeoutException(DeviceId, command.Timeout);
            }
            catch (Exception ex) when (ex is not DeviceException)
            {
                sw.Stop();
                SetStatus(DeviceStatus.Error, ex.Message);
                return CommandResult.Fail(ex.Message);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<CommandResult> SendCommandAsync(
        string action, Dictionary<string, object>? parameters = null, CancellationToken ct = default)
    {
        var command = new DeviceCommand
        {
            DeviceId = DeviceId,
            Action = action,
            Parameters = parameters ?? new(),
        };
        return await ExecuteCommandAsync(command, ct);
    }

    public virtual async Task<bool> ResetAsync(CancellationToken ct = default)
    {
        Logger.LogWarning("[{DeviceId}] Resetting device", DeviceId);
        SetStatus(DeviceStatus.Initializing);
        try
        {
            await DisconnectAsync(ct);
            return await ConnectAsync(ct);
        }
        catch (Exception ex)
        {
            SetStatus(DeviceStatus.Error, ex.Message);
            return false;
        }
    }

    public abstract Task<Dictionary<string, object>> GetStatusAsync(CancellationToken ct = default);

    protected virtual Task OnConnectedAsync(CancellationToken ct) => Task.CompletedTask;
    protected virtual Task OnDisconnectingAsync(CancellationToken ct) => Task.CompletedTask;
    protected abstract Task<CommandResult> OnExecuteCommandAsync(DeviceCommand command, CancellationToken ct);

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
