namespace CoreService.Common.Models;

/// <summary>
/// Generic result model for API responses
/// </summary>
/// <typeparam name="T">The type of data returned</typeparam>
public class Result<T>
{
    /// <summary>
    /// Indicates success or failure
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Result data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error code
    /// </summary>
    public string? ErrorCode { get; set; }

    public static Result<T> SuccessResult(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static Result<T> FailureResult(string errorCode, string message)
        => new() { Success = false, ErrorCode = errorCode, Message = message };
}

/// <summary>
/// Result model without data
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates success or failure
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error code
    /// </summary>
    public string? ErrorCode { get; set; }

    public static Result SuccessResult(string? message = null)
        => new() { Success = true, Message = message };

    public static Result FailureResult(string errorCode, string message)
        => new() { Success = false, ErrorCode = errorCode, Message = message };
}
