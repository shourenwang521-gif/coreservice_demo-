using System.Diagnostics;
using CoreService.Devices;
using CoreService.Exceptions;
using CoreService.Models;
using CoreService.Services;
using Microsoft.Extensions.Logging;

namespace CoreService.Scheduling;

/// <summary>
/// 核心调度引擎：接收上位机下发的工艺流程，支持串行/并行步骤执行，
/// 带有重试、超时、异常恢复等机制。
/// </summary>
public class WorkflowEngine : IWorkflowEngine
{
    private readonly DeviceManager _deviceManager;
    private readonly ILogger<WorkflowEngine> _logger;
    private CancellationTokenSource? _workflowCts;

    public string? CurrentWorkflowId { get; private set; }

    public event EventHandler<StepExecutionResult>? StepStarted;
    public event EventHandler<StepExecutionResult>? StepCompleted;
    public event EventHandler<WorkflowExecutionResult>? WorkflowCompleted;

    public WorkflowEngine(DeviceManager deviceManager, ILogger<WorkflowEngine> logger)
    {
        _deviceManager = deviceManager;
        _logger = logger;
    }

    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
        WorkflowDefinition workflow, CancellationToken ct = default)
    {
        if (CurrentWorkflowId != null)
            throw new WorkflowException(workflow.WorkflowId, "Another workflow is already running.");

        _workflowCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        CurrentWorkflowId = workflow.WorkflowId;

        var result = new WorkflowExecutionResult
        {
            WorkflowId = workflow.WorkflowId,
            Status = WorkflowExecutionStatus.Running,
            StartedAt = DateTime.UtcNow,
        };

        _logger.LogInformation("========== Workflow [{Id}] {Name} STARTED ==========",
            workflow.WorkflowId, workflow.Name);

        try
        {
            var stepResults = await ExecuteGroupAsync(workflow.RootGroup, _workflowCts.Token);
            result.StepResults.AddRange(stepResults);
            result.Status = result.AllStepsSucceeded
                ? WorkflowExecutionStatus.Completed
                : WorkflowExecutionStatus.Failed;

            if (!result.AllStepsSucceeded)
            {
                var failedStep = result.StepResults.FirstOrDefault(r => !r.Success);
                result.ErrorMessage = failedStep?.ErrorMessage;
            }
        }
        catch (OperationCanceledException)
        {
            result.Status = WorkflowExecutionStatus.Cancelled;
            result.ErrorMessage = "Workflow was cancelled.";
            _logger.LogWarning("Workflow [{Id}] CANCELLED", workflow.WorkflowId);
        }
        catch (Exception ex)
        {
            result.Status = WorkflowExecutionStatus.Failed;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Workflow [{Id}] FAILED", workflow.WorkflowId);
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
            CurrentWorkflowId = null;
            _workflowCts.Dispose();
            _workflowCts = null;
        }

        _logger.LogInformation("========== Workflow [{Id}] {Status} (elapsed: {Elapsed}) ==========",
            workflow.WorkflowId, result.Status, result.TotalElapsed);

        WorkflowCompleted?.Invoke(this, result);
        return result;
    }

    public Task CancelCurrentWorkflowAsync()
    {
        if (_workflowCts != null && !_workflowCts.IsCancellationRequested)
        {
            _logger.LogWarning("Cancelling workflow [{Id}]", CurrentWorkflowId);
            _workflowCts.Cancel();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 递归执行步骤组（支持嵌套的串行/并行组合）。
    /// </summary>
    private async Task<List<StepExecutionResult>> ExecuteGroupAsync(
        WorkflowStepGroup group, CancellationToken ct)
    {
        _logger.LogInformation("Executing group [{GroupId}] {Name} (mode: {Mode})",
            group.GroupId, group.Name, group.ExecutionMode);

        // Merge steps and sub-groups into a single ordered list of executable items
        var items = new List<(int Order, WorkflowStep? Step, WorkflowStepGroup? SubGroup)>();
        foreach (var step in group.Steps)
            items.Add((step.Order, step, null));
        foreach (var sub in group.SubGroups)
            items.Add((sub.Order, null, sub));
        items.Sort((a, b) => a.Order.CompareTo(b.Order));

        if (group.ExecutionMode == StepExecutionMode.Parallel)
        {
            return await ExecuteParallelAsync(items, ct);
        }
        else
        {
            return await ExecuteSerialAsync(items, ct);
        }
    }

    /// <summary>串行执行：逐一执行每个步骤/子组。</summary>
    private async Task<List<StepExecutionResult>> ExecuteSerialAsync(
        List<(int Order, WorkflowStep? Step, WorkflowStepGroup? SubGroup)> items,
        CancellationToken ct)
    {
        var results = new List<StepExecutionResult>();

        foreach (var (_, step, subGroup) in items)
        {
            ct.ThrowIfCancellationRequested();

            if (subGroup != null)
            {
                var subResults = await ExecuteGroupAsync(subGroup, ct);
                results.AddRange(subResults);

                if (subResults.Any(r => !r.Success))
                {
                    _logger.LogWarning("Sub-group [{GroupId}] had failures", subGroup.GroupId);
                    // For serial mode, stop if a non-ContinueOnError step fails
                    break;
                }
            }
            else if (step != null)
            {
                var result = await ExecuteStepWithRetryAsync(step, ct);
                results.Add(result);

                if (!result.Success && !step.ContinueOnError)
                {
                    _logger.LogError("Step [{StepId}] {Name} failed, stopping serial execution",
                        step.StepId, step.Name);
                    break;
                }
            }
        }

        return results;
    }

    /// <summary>并行执行：同时执行所有步骤/子组。</summary>
    private async Task<List<StepExecutionResult>> ExecuteParallelAsync(
        List<(int Order, WorkflowStep? Step, WorkflowStepGroup? SubGroup)> items,
        CancellationToken ct)
    {
        var tasks = new List<Task<List<StepExecutionResult>>>();

        foreach (var (_, step, subGroup) in items)
        {
            if (subGroup != null)
            {
                tasks.Add(ExecuteGroupAsync(subGroup, ct));
            }
            else if (step != null)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var r = await ExecuteStepWithRetryAsync(step, ct);
                    return new List<StepExecutionResult> { r };
                }, ct));
            }
        }

        var allResults = await Task.WhenAll(tasks);
        return allResults.SelectMany(r => r).ToList();
    }

    /// <summary>
    /// 带重试的步骤执行：失败后按配置重试。
    /// </summary>
    private async Task<StepExecutionResult> ExecuteStepWithRetryAsync(
        WorkflowStep step, CancellationToken ct)
    {
        int maxAttempts = step.RetryCount + 1;
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var stepResult = new StepExecutionResult
            {
                StepId = step.StepId,
                StepName = step.Name,
                AttemptCount = attempt,
            };

            StepStarted?.Invoke(this, stepResult);
            _logger.LogInformation("Step [{StepId}] {Name} attempt {Attempt}/{Max}",
                step.StepId, step.Name, attempt, maxAttempts);

            var sw = Stopwatch.StartNew();

            try
            {
                var device = _deviceManager.GetDevice(step.DeviceId);
                if (device == null)
                {
                    throw new DeviceException(step.DeviceId,
                        $"Device '{step.DeviceId}' not found in device manager.");
                }

                var cmdResult = await device.SendCommandAsync(step.Action, step.Parameters, ct);
                sw.Stop();

                stepResult.Success = cmdResult.Success;
                stepResult.CommandResult = cmdResult;
                stepResult.ElapsedTime = sw.Elapsed;

                if (!cmdResult.Success)
                {
                    stepResult.ErrorMessage = cmdResult.ErrorMessage;
                    lastException = new StepExecutionException(
                        "", step.StepId, cmdResult.ErrorMessage ?? "Command failed", attempt);
                    _logger.LogWarning("Step [{StepId}] attempt {Attempt} failed: {Error}",
                        step.StepId, attempt, cmdResult.ErrorMessage);
                }
                else
                {
                    _logger.LogInformation("Step [{StepId}] {Name} succeeded in {Elapsed}",
                        step.StepId, step.Name, sw.Elapsed);
                    StepCompleted?.Invoke(this, stepResult);
                    return stepResult;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // propagate workflow-level cancellation
            }
            catch (DeviceTimeoutException ex)
            {
                sw.Stop();
                lastException = ex;
                stepResult.Success = false;
                stepResult.ErrorMessage = ex.Message;
                stepResult.ElapsedTime = sw.Elapsed;
                _logger.LogWarning("Step [{StepId}] attempt {Attempt} timed out", step.StepId, attempt);
            }
            catch (DeviceException ex)
            {
                sw.Stop();
                lastException = ex;
                stepResult.Success = false;
                stepResult.ErrorMessage = ex.Message;
                stepResult.ElapsedTime = sw.Elapsed;
                _logger.LogWarning(ex, "Step [{StepId}] attempt {Attempt} device error", step.StepId, attempt);
            }

            StepCompleted?.Invoke(this, stepResult);

            // Wait before retry (unless it's the last attempt)
            if (attempt < maxAttempts)
            {
                _logger.LogInformation("Retrying step [{StepId}] in {Delay}", step.StepId, step.RetryDelay);
                await Task.Delay(step.RetryDelay, ct);
            }
        }

        // All attempts exhausted
        return new StepExecutionResult
        {
            StepId = step.StepId,
            StepName = step.Name,
            Success = false,
            ErrorMessage = lastException?.Message ?? "All retry attempts exhausted",
            AttemptCount = maxAttempts,
        };
    }
}
