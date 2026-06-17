using CoreService.Devices;
using CoreService.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreService.Tests;

public class ScannerDeviceTests
{
    private ScannerDevice CreateScanner() =>
        new(new DeviceInfo { DeviceId = "scanner-01", Name = "TestScanner", DeviceType = DeviceType.Scanner },
            null, NullLogger<ScannerDevice>.Instance);

    [Fact]
    public async Task Scan_ReturnsResult()
    {
        var scanner = CreateScanner();
        await scanner.ConnectAsync();
        var result = await scanner.SendCommandAsync("scan");
        Assert.True(result.Success);
        Assert.True(result.Data.ContainsKey("scan_result"));
    }

    [Fact]
    public async Task Configure_UpdatesSettings()
    {
        var scanner = CreateScanner();
        await scanner.ConnectAsync();
        var result = await scanner.SendCommandAsync("configure", new()
        {
            ["mode"] = "qrcode",
            ["resolution"] = 600,
        });
        Assert.True(result.Success);

        var status = await scanner.GetStatusAsync();
        Assert.Equal("qrcode", status["scan_mode"]);
        Assert.Equal(600, status["resolution"]);
    }

    [Fact]
    public async Task ClearBuffer_ClearsScans()
    {
        var scanner = CreateScanner();
        await scanner.ConnectAsync();
        await scanner.SendCommandAsync("scan");
        await scanner.SendCommandAsync("scan");

        var clearResult = await scanner.SendCommandAsync("clear_buffer");
        Assert.True(clearResult.Success);
        Assert.Equal(2, clearResult.Data["cleared_count"]);

        var status = await scanner.GetStatusAsync();
        Assert.Equal(0, status["buffer_size"]);
    }
}
