namespace CoreService.Models;

/// <summary>
/// 完整的工艺流程定义，由上位机下发。
/// </summary>
public class WorkflowDefinition
{
    public string WorkflowId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>顶层步骤组（根节点）。</summary>
    public WorkflowStepGroup RootGroup { get; set; } = new();

    /// <summary>全局参数。</summary>
    public Dictionary<string, object> GlobalParameters { get; set; } = new();
}
