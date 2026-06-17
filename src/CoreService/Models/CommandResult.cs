namespace CoreService.Models;

/// <summary>
/// 设备指令执行结果。
/// </summary>
public class CommandResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public TimeSpan ElapsedTime { get; set; }

    public static CommandResult Ok(Dictionary<string, object>? data = null) =>
        new() { Success = true, Data = data ?? new() };

    public static CommandResult Fail(string error) =>
        new() { Success = false, ErrorMessage = error };
}
