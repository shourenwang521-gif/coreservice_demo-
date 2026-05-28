namespace CoreService.Models;

/// <summary>
/// 实验/工艺流程运行状态。
/// </summary>
public enum ExperimentStatus
{
    Idle,
    Running,
    Paused,
    Stopped,
    Completed,
    Faulted,
}
