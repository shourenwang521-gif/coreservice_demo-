namespace CoreService.Models;

/// <summary>
/// 工艺流程中的单个步骤定义。
/// </summary>
public class WorkflowStep
{
    public string StepId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>目标设备 ID。</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>要执行的动作。</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>动作参数。</summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>超时时间。</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>失败后重试次数。</summary>
    public int RetryCount { get; set; }

    /// <summary>重试间隔。</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>步骤失败是否允许继续执行后续步骤。</summary>
    public bool ContinueOnError { get; set; }

    /// <summary>在当前步骤组内的执行方式（仅当父级为 Group 时生效）。</summary>
    public int Order { get; set; }
}
