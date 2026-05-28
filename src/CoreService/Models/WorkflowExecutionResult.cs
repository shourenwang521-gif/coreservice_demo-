namespace CoreService.Models;

/// <summary>
/// 整个工艺流程的执行结果。
/// </summary>
public class WorkflowExecutionResult
{
    public string WorkflowId { get; set; } = string.Empty;
    public WorkflowExecutionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan TotalElapsed => CompletedAt - StartedAt;
    public List<StepExecutionResult> StepResults { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public bool AllStepsSucceeded => StepResults.TrueForAll(r => r.Success);
}
