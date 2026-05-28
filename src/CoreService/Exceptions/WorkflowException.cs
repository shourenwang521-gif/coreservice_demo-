namespace CoreService.Exceptions;

/// <summary>
/// 工艺流程执行异常。
/// </summary>
public class WorkflowException : Exception
{
    public string WorkflowId { get; }
    public string? StepId { get; }

    public WorkflowException(string workflowId, string message, string? stepId = null)
        : base(message)
    {
        WorkflowId = workflowId;
        StepId = stepId;
    }

    public WorkflowException(string workflowId, string message, Exception inner, string? stepId = null)
        : base(message, inner)
    {
        WorkflowId = workflowId;
        StepId = stepId;
    }
}

/// <summary>
/// 工艺流程步骤执行失败。
/// </summary>
public class StepExecutionException : WorkflowException
{
    public int AttemptCount { get; }

    public StepExecutionException(string workflowId, string stepId, string message, int attemptCount)
        : base(workflowId, message, stepId)
    {
        AttemptCount = attemptCount;
    }

    public StepExecutionException(string workflowId, string stepId, string message, Exception inner, int attemptCount)
        : base(workflowId, message, inner, stepId)
    {
        AttemptCount = attemptCount;
    }
}
