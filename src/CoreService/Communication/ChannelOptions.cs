namespace CoreService.Communication;

/// <summary>
/// TCP 通讯通道配置。
/// </summary>
public class TcpChannelOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5000;
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public int ReceiveBufferSize { get; set; } = 8192;
}

/// <summary>
/// 串口通讯通道配置。
/// </summary>
public class SerialChannelOptions
{
    public string PortName { get; set; } = "COM1";
    public int BaudRate { get; set; } = 9600;
    public int DataBits { get; set; } = 8;
    public string Parity { get; set; } = "None";
    public string StopBits { get; set; } = "One";
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Modbus TCP 通讯通道配置。
/// </summary>
public class ModbusChannelOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 502;
    public byte SlaveId { get; set; } = 1;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// HTTP/REST 通讯通道配置。
/// </summary>
public class HttpChannelOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8080";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
}
