using CoreService.Devices;
using CoreService.Models;
using CoreService.Scheduling;
using CoreService.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreService.Tests;

public class ExperimentControllerTests
{
    private static (ExperimentController Controller, WorkflowService Service, DeviceManager DevMgr) CreateController()
    {
        var devMgr = new DeviceManager(NullLogger<DeviceManager>.Instance);
        var robot = new RobotDevice(
            new DeviceInfo { DeviceId = "robot-01", Name = "Robot", DeviceType = DeviceType.Robot },
            null, NullLogger<RobotDevice>.Instance);
        devMgr.RegisterDevice(robot);

        var engine = new WorkflowEngine(devMgr, NullLogger<WorkflowEngine>.Instance);
        var svc = new WorkflowService(engine, NullLogger<WorkflowService>.Instance);
        var ctrl = new ExperimentController(svc, engine, NullLogger<ExperimentController>.Instance);
        return (ctrl, svc, devMgr);
    }

    private static WorkflowDefinition CreateSimpleWorkflow(int stepCount = 3) => new()
    {
        Name = "TestWorkflow",
        RootGroup = new WorkflowStepGroup
        {
            ExecutionMode = StepExecutionMode.Serial,
            Steps = Enumerable.Range(1, stepCount).Select(i => new WorkflowStep
            {
                Name = $"Step{i}",
                DeviceId = "robot-01",
                Action = "home",
                Order = i,
            }).ToList(),
        },
    };

    [Fact]
    public async Task Start_RunsWorkflow_CompletesSuccessfully()
    {
        var (ctrl, _, devMgr) = CreateController();
        await devMgr.ConnectAllAsync();

        var workflow = CreateSimpleWorkflow();
        var started = await ctrl.StartAsync(workflow);
        Assert.True(started);

        // Wait for completion
        for (int i = 0; i < 50 && ctrl.Status == ExperimentStatus.Running; i++)
            await Task.Delay(50);

        Assert.Equal(ExperimentStatus.Completed, ctrl.Status);
        Assert.NotNull(ctrl.LastResult);
        Assert.Equal(WorkflowExecutionStatus.Completed, ctrl.LastResult!.Status);
    }

    [Fact]
    public async Task Start_WhenAlreadyRunning_ReturnsFalse()
    {
        var (ctrl, _, devMgr) = CreateController();
        await devMgr.ConnectAllAsync();

        var workflow = CreateSimpleWorkflow(10);
        await ctrl.StartAsync(workflow);

        var secondStart = await ctrl.StartAsync("some-other-id");
        Assert.False(secondStart);
    }

    [Fact]
    public async Task Stop_CancelsRunningWorkflow()
    {
        var (ctrl, _, devMgr) = CreateController();
        await devMgr.ConnectAllAsync();

        var workflow = CreateSimpleWorkflow(20);
        await ctrl.StartAsync(workflow);
        await Task.Delay(50);

        var stopped = await ctrl.StopAsync();
        Assert.True(stopped);

        for (int i = 0; i < 50 && ctrl.Status == ExperimentStatus.Running; i++)
            await Task.Delay(50);

        Assert.Equal(ExperimentStatus.Stopped, ctrl.Status);
    }

    [Fact]
    public async Task Stop_WhenIdle_ReturnsFalse()
    {
        var (ctrl, _, _) = CreateController();
        var stopped = await ctrl.StopAsync();
        Assert.False(stopped);
    }

    [Fact]
    public async Task Pause_PausesRunningWorkflow()
    {
        var (ctrl, _, devMgr) = CreateController();
        await devMgr.ConnectAllAsync();

        var workflow = CreateSimpleWorkflow(20);
        await ctrl.StartAsync(workflow);
        await Task.Delay(30);

        var paused = await ctrl.PauseAsync();
        Assert.True(paused);

        // Wait for pause to take effect
        for (int i = 0; i < 50 && ctrl.Status != ExperimentStatus.Paused; i++)
            await Task.Delay(50);

        Assert.Equal(ExperimentStatus.Paused, ctrl.Status);
    }

    [Fact]
    public async Task Pause_WhenNotRunning_ReturnsFalse()
    {
        var (ctrl, _, _) = CreateController();
        var paused = await ctrl.PauseAsync();
        Assert.False(paused);
    }

    [Fact]
    public async Task Resume_ResumesAfterPause()
    {
        var (ctrl, _, devMgr) = CreateController();
        await devMgr.ConnectAllAsync();

        var workflow = CreateSimpleWorkflow(20);
        await ctrl.StartAsync(workflow);
        await Task.Delay(30);

        await ctrl.PauseAsync();
        for (int i = 0; i < 50 && ctrl.Status != ExperimentStatus.Paused; i++)
            await Task.Delay(50);
        Assert.Equal(ExperimentStatus.Paused, ctrl.Status);

        var resumed = await ctrl.ResumeAsync();
        Assert.True(resumed);

        for (int i = 0; i < 100 && ctrl.Status == ExperimentStatus.Running; i++)
            await Task.Delay(50);

        Assert.Equal(ExperimentStatus.Completed, ctrl.Status);
    }

    [Fact]
    public async Task Resume_WhenNotPaused_ReturnsFalse()
    {
        var (ctrl, _, _) = CreateController();
        var resumed = await ctrl.ResumeAsync();
        Assert.False(resumed);
    }

    [Fact]
    public async Task StatusChanged_FiresOnTransitions()
    {
        var (ctrl, _, devMgr) = CreateController();
        await devMgr.ConnectAllAsync();

        var transitions = new List<(ExperimentStatus Old, ExperimentStatus New)>();
        ctrl.StatusChanged += (_, e) => transitions.Add((e.OldStatus, e.NewStatus));

        var workflow = CreateSimpleWorkflow();
        await ctrl.StartAsync(workflow);

        for (int i = 0; i < 50 && ctrl.Status == ExperimentStatus.Running; i++)
            await Task.Delay(50);

        Assert.Contains(transitions, t => t.Old == ExperimentStatus.Idle && t.New == ExperimentStatus.Running);
        Assert.Contains(transitions, t => t.New == ExperimentStatus.Completed);
    }

    [Fact]
    public async Task StopWhilePaused_StopsCorrectly()
    {
        var (ctrl, _, devMgr) = CreateController();
        await devMgr.ConnectAllAsync();

        var workflow = CreateSimpleWorkflow(20);
        await ctrl.StartAsync(workflow);
        await Task.Delay(30);

        await ctrl.PauseAsync();
        for (int i = 0; i < 50 && ctrl.Status != ExperimentStatus.Paused; i++)
            await Task.Delay(50);

        var stopped = await ctrl.StopAsync();
        Assert.True(stopped);

        for (int i = 0; i < 50 && ctrl.Status == ExperimentStatus.Paused; i++)
            await Task.Delay(50);

        Assert.Equal(ExperimentStatus.Stopped, ctrl.Status);
    }
}
