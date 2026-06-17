using CoreService.Models;
using CoreService.Scheduling;
using Microsoft.Extensions.Logging;

namespace CoreService.Services;

/// <summary>
/// 实验控制器：对外提供统一的启动/停止/暂停/恢复接口，
/// 适用于上位机 TCP 指令、HTTP API、PLC 触发等多种外部控制方式。
/// </summary>
public class ExperimentController : IExperimentController
{
    private readonly WorkflowService _workflowService;
    private readonly IWorkflowEngine _engine;
    private readonly ILogger<ExperimentController> _logger;
    private readonly object _lock = new();

    private Task? _runningTask;
    private CancellationTokenSource? _cts;

    public ExperimentStatus Status { get; private set; } = ExperimentStatus.Idle;
    public string? CurrentWorkflowId => _engine.CurrentWorkflowId;
    public WorkflowExecutionResult? LastResult { get; private set; }

    public event EventHandler<ExperimentStatusChangedEventArgs>? StatusChanged;

    public ExperimentController(
        WorkflowService workflowService,
        IWorkflowEngine engine,
        ILogger<ExperimentController> logger)
    {
        _workflowService = workflowService;
        _engine = engine;
        _logger = logger;

        if (_engine is WorkflowEngine we)
        {
            we.Paused += (_, _) => SetStatus(ExperimentStatus.Paused, "Paused by external command");
            we.Resumed += (_, _) => SetStatus(ExperimentStatus.Running, "Resumed by external command");
        }
    }

    public async Task<bool> StartAsync(WorkflowDefinition workflow)
    {
        _workflowService.RegisterWorkflow(workflow);
        return await StartAsync(workflow.WorkflowId);
    }

    public Task<bool> StartAsync(string workflowId)
    {
        lock (_lock)
        {
            if (Status == ExperimentStatus.Running || Status == ExperimentStatus.Paused)
            {
                _logger.LogWarning("Cannot start: experiment is already {Status}", Status);
                return Task.FromResult(false);
            }

            _cts = new CancellationTokenSource();
            SetStatus(ExperimentStatus.Running, $"Starting workflow {workflowId}");

            _runningTask = Task.Run(async () =>
            {
                try
                {
                    var result = await _workflowService.ExecuteWorkflowAsync(workflowId, _cts.Token);
                    LastResult = result;

                    var finalStatus = result.Status switch
                    {
                        WorkflowExecutionStatus.Completed => ExperimentStatus.Completed,
                        WorkflowExecutionStatus.Cancelled => ExperimentStatus.Stopped,
                        _ => ExperimentStatus.Faulted,
                    };
                    SetStatus(finalStatus, $"Workflow finished: {result.Status}");
                }
                catch (OperationCanceledException)
                {
                    SetStatus(ExperimentStatus.Stopped, "Experiment stopped by external command");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Experiment faulted");
                    SetStatus(ExperimentStatus.Faulted, ex.Message);
                }
            });
        }

        return Task.FromResult(true);
    }

    public async Task<bool> StopAsync()
    {
        if (Status != ExperimentStatus.Running && Status != ExperimentStatus.Paused)
        {
            _logger.LogWarning("Cannot stop: experiment is {Status}", Status);
            return false;
        }

        _logger.LogWarning("Stopping experiment...");

        // If paused, resume first so the engine can process the cancellation
        if (_engine.IsPaused)
            await _engine.ResumeAsync();

        _cts?.Cancel();
        await _engine.CancelCurrentWorkflowAsync();

        if (_runningTask != null)
        {
            try { await _runningTask; }
            catch { /* already handled inside the task */ }
        }

        return true;
    }

    public async Task<bool> PauseAsync()
    {
        if (Status != ExperimentStatus.Running)
        {
            _logger.LogWarning("Cannot pause: experiment is {Status}", Status);
            return false;
        }

        await _engine.PauseAsync();
        return true;
    }

    public async Task<bool> ResumeAsync()
    {
        if (Status != ExperimentStatus.Paused)
        {
            _logger.LogWarning("Cannot resume: experiment is {Status}", Status);
            return false;
        }

        await _engine.ResumeAsync();
        return true;
    }

    private void SetStatus(ExperimentStatus newStatus, string? message = null)
    {
        var old = Status;
        if (old == newStatus) return;
        Status = newStatus;
        _logger.LogInformation("Experiment status: {Old} -> {New} ({Message})", old, newStatus, message);
        StatusChanged?.Invoke(this, new ExperimentStatusChangedEventArgs
        {
            OldStatus = old,
            NewStatus = newStatus,
            WorkflowId = CurrentWorkflowId,
            Message = message,
        });
    }
}
