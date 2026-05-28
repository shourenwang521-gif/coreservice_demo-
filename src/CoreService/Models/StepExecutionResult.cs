namespace CoreService.Models;

/// <summary>
/// 单个步骤的执行结果。
/// </summary>
public class StepExecutionResult
{
    public string StepId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; } = 1;
    public TimeSpan ElapsedTime { get; set; }
    public CommandResult? CommandResult { get; set; }
}
