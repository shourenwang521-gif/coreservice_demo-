using CoreService.Devices;
using CoreService.Models;
using CoreService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreService.Tests;

public class DeviceManagerTests
{
    private DeviceManager CreateManager() =>
        new(NullLogger<DeviceManager>.Instance);

    private RobotDevice CreateRobot(string id = "robot-01") =>
        new(new DeviceInfo { DeviceId = id, Name = "TestRobot", DeviceType = DeviceType.Robot },
            null, NullLogger<RobotDevice>.Instance);

    [Fact]
    public void RegisterDevice_AddsDeviceSuccessfully()
    {
        var manager = CreateManager();
        var robot = CreateRobot();
        manager.RegisterDevice(robot);

        var found = manager.GetDevice("robot-01");
        Assert.NotNull(found);
        Assert.Equal("robot-01", found.DeviceId);
    }

    [Fact]
    public void RegisterDevice_DuplicateId_Throws()
    {
        var manager = CreateManager();
        manager.RegisterDevice(CreateRobot());
        Assert.Throws<InvalidOperationException>(() => manager.RegisterDevice(CreateRobot()));
    }

    [Fact]
    public async Task UnregisterDevice_RemovesDevice()
    {
        var manager = CreateManager();
        manager.RegisterDevice(CreateRobot());
        await manager.UnregisterDeviceAsync("robot-01");
        Assert.Null(manager.GetDevice("robot-01"));
    }

    [Fact]
    public void GetAllDevices_ReturnsAll()
    {
        var manager = CreateManager();
        manager.RegisterDevice(CreateRobot("r1"));
        manager.RegisterDevice(CreateRobot("r2"));
        Assert.Equal(2, manager.GetAllDevices().Count);
    }

    [Fact]
    public void GetDevicesByType_FiltersCorrectly()
    {
        var manager = CreateManager();
        manager.RegisterDevice(CreateRobot());
        manager.RegisterDevice(new ScannerDevice(
            new DeviceInfo { DeviceId = "s1", Name = "Scan", DeviceType = DeviceType.Scanner },
            null, NullLogger<ScannerDevice>.Instance));

        var robots = manager.GetDevicesByType(DeviceType.Robot);
        Assert.Single(robots);
    }

    [Fact]
    public async Task ConnectAll_ConnectsDevices()
    {
        var manager = CreateManager();
        manager.RegisterDevice(CreateRobot());
        var results = await manager.ConnectAllAsync();
        Assert.True(results["robot-01"]);
    }
}
