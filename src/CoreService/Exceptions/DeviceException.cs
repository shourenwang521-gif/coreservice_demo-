namespace CoreService.Exceptions;

/// <summary>
/// 设备操作异常基类。
/// </summary>
public class DeviceException : Exception
{
    public string DeviceId { get; }

    public DeviceException(string deviceId, string message)
        : base(message)
    {
        DeviceId = deviceId;
    }

    public DeviceException(string deviceId, string message, Exception innerException)
        : base(message, innerException)
    {
        DeviceId = deviceId;
    }
}

/// <summary>
/// 设备连接失败异常。
/// </summary>
public class DeviceConnectionException : DeviceException
{
    public DeviceConnectionException(string deviceId, string message)
        : base(deviceId, $"Connection failed for device {deviceId}: {message}") { }

    public DeviceConnectionException(string deviceId, string message, Exception inner)
        : base(deviceId, $"Connection failed for device {deviceId}: {message}", inner) { }
}

/// <summary>
/// 设备命令超时异常。
/// </summary>
public class DeviceTimeoutException : DeviceException
{
    public TimeSpan Timeout { get; }

    public DeviceTimeoutException(string deviceId, TimeSpan timeout)
        : base(deviceId, $"Device {deviceId} command timed out after {timeout.TotalSeconds}s")
    {
        Timeout = timeout;
    }
}

/// <summary>
/// 设备通讯异常。
/// </summary>
public class DeviceCommunicationException : DeviceException
{
    public DeviceCommunicationException(string deviceId, string message)
        : base(deviceId, $"Communication error with device {deviceId}: {message}") { }

    public DeviceCommunicationException(string deviceId, string message, Exception inner)
        : base(deviceId, $"Communication error with device {deviceId}: {message}", inner) { }
}
