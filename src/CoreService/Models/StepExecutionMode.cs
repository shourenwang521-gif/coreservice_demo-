namespace CoreService.Models;

/// <summary>
/// 工艺步骤执行模式。
/// </summary>
public enum StepExecutionMode
{
    /// <summary>串行执行</summary>
    Serial,

    /// <summary>并行执行</summary>
    Parallel,
}
