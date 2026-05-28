namespace CoreService.Models;

public enum WorkflowExecutionStatus
{
    Pending,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled,
}
