namespace CoreService.Models;

/// <summary>
/// 一组工艺步骤，可以串行或并行执行。
/// </summary>
public class WorkflowStepGroup
{
    public string GroupId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;

    /// <summary>本组的执行模式：串行 or 并行。</summary>
    public StepExecutionMode ExecutionMode { get; set; } = StepExecutionMode.Serial;

    /// <summary>本组包含的步骤。</summary>
    public List<WorkflowStep> Steps { get; set; } = new();

    /// <summary>本组包含的子组（支持嵌套）。</summary>
    public List<WorkflowStepGroup> SubGroups { get; set; } = new();

    /// <summary>在父级组中的执行顺序。</summary>
    public int Order { get; set; }
}
