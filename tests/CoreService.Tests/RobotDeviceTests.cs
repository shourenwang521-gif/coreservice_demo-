using CoreService.Devices;
using CoreService.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreService.Tests;

public class RobotDeviceTests
{
    private RobotDevice CreateRobot() =>
        new(new DeviceInfo { DeviceId = "robot-01", Name = "TestRobot", DeviceType = DeviceType.Robot },
            null, NullLogger<RobotDevice>.Instance);

    [Fact]
    public async Task Connect_SetsStatusToIdle()
    {
        var robot = CreateRobot();
        await robot.ConnectAsync();
        Assert.Equal(DeviceStatus.Idle, robot.Status);
    }

    [Fact]
    public async Task Move_UpdatesPosition()
    {
        var robot = CreateRobot();
        await robot.ConnectAsync();

        var result = await robot.SendCommandAsync("move", new()
        {
            ["x"] = 100.0, ["y"] = 200.0, ["z"] = 50.0,
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task PickAndPlace_TogglesHoldingItem()
    {
        var robot = CreateRobot();
        await robot.ConnectAsync();

        var pickResult = await robot.SendCommandAsync("pick");
        Assert.True(pickResult.Success);

        var status = await robot.GetStatusAsync();
        Assert.True((bool)status["holding_item"]);

        var placeResult = await robot.SendCommandAsync("place");
        Assert.True(placeResult.Success);

        status = await robot.GetStatusAsync();
        Assert.False((bool)status["holding_item"]);
    }

    [Fact]
    public async Task Home_ResetsPosition()
    {
        var robot = CreateRobot();
        await robot.ConnectAsync();

        await robot.SendCommandAsync("move", new() { ["x"] = 100.0 });
        await robot.SendCommandAsync("home");

        var status = await robot.GetStatusAsync();
        var pos = (Dictionary<string, double>)status["position"];
        Assert.Equal(0.0, pos["x"]);
        Assert.Equal(0.0, pos["y"]);
        Assert.Equal(0.0, pos["z"]);
    }

    [Fact]
    public async Task UnsupportedAction_ReturnsFail()
    {
        var robot = CreateRobot();
        await robot.ConnectAsync();
        var result = await robot.SendCommandAsync("fly");
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Disconnect_SetsOffline()
    {
        var robot = CreateRobot();
        await robot.ConnectAsync();
        await robot.DisconnectAsync();
        Assert.Equal(DeviceStatus.Offline, robot.Status);
    }
}
