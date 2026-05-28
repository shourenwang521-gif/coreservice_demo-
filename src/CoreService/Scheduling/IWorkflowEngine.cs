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

    /// <summary>取消当前正在执行的工艺流程。</summary>
    Task CancelCurrentWorkflowAsync();

    /// <summary>步骤开始执行时触发。</summary>
    event EventHandler<StepExecutionResult>? StepStarted;

    /// <summary>步骤执行完成时触发。</summary>
    event EventHandler<StepExecutionResult>? StepCompleted;

    /// <summary>工艺流程执行完成时触发。</summary>
    event EventHandler<WorkflowExecutionResult>? WorkflowCompleted;
}
