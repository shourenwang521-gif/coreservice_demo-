using CoreService.Models;

namespace CoreService.Services;

/// <summary>
/// 外部接口控制器：支持上位机或外部系统通过统一接口控制实验/工艺流程的启动、停止、暂停、恢复。
/// </summary>
public interface IExperimentController
{
    /// <summary>启动实验（加载工艺流程并开始执行）。</summary>
    Task<bool> StartAsync(WorkflowDefinition workflow);

    /// <summary>按已注册的工艺流程 ID 启动实验。</summary>
    Task<bool> StartAsync(string workflowId);

    /// <summary>停止当前正在执行的实验。</summary>
    Task<bool> StopAsync();

    /// <summary>暂停当前正在执行的实验（在当前步骤完成后暂停）。</summary>
    Task<bool> PauseAsync();

    /// <summary>恢复已暂停的实验。</summary>
    Task<bool> ResumeAsync();

    /// <summary>当前实验状态。</summary>
    ExperimentStatus Status { get; }

    /// <summary>当前正在执行的工艺流程 ID。</summary>
    string? CurrentWorkflowId { get; }

    /// <summary>最近一次执行结果。</summary>
    WorkflowExecutionResult? LastResult { get; }

    /// <summary>实验状态变更事件。</summary>
    event EventHandler<ExperimentStatusChangedEventArgs>? StatusChanged;
}

/// <summary>
/// 实验状态变更事件参数。
/// </summary>
public class ExperimentStatusChangedEventArgs : EventArgs
{
    public ExperimentStatus OldStatus { get; init; }
    public ExperimentStatus NewStatus { get; init; }
    public string? WorkflowId { get; init; }
    public string? Message { get; init; }
}
