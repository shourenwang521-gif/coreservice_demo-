using CoreService.Models;

namespace CoreService.Scheduling;

/// <summary>
/// 工艺流程调度引擎接口。
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>执行完整工艺流程。</summary>
    Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
        WorkflowDefinition workflow, CancellationToken ct = default);

    /// <summary>正在执行的工艺流程 ID（null 表示空闲）。</summary>
    string? CurrentWorkflowId { get; }

    /// <summary>暂停当前正在执行的工艺流程（在当前步骤完成后暂停）。</summary>
    Task PauseAsync();

    /// <summary>恢复已暂停的工艺流程。</summary>
    Task ResumeAsync();

    /// <summary>取消当前正在执行的工艺流程。</summary>
    Task CancelCurrentWorkflowAsync();

    /// <summary>当前是否处于暂停状态。</summary>
    bool IsPaused { get; }

    /// <summary>步骤开始执行时触发。</summary>
    event EventHandler<StepExecutionResult>? StepStarted;

    /// <summary>步骤执行完成时触发。</summary>
    event EventHandler<StepExecutionResult>? StepCompleted;

    /// <summary>工艺流程执行完成时触发。</summary>
    event EventHandler<WorkflowExecutionResult>? WorkflowCompleted;
}
