using CoreService.Devices;
using CoreService.Models;
using CoreService.Scheduling;
using CoreService.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreService.Tests;

public class WorkflowEngineTests
{
    private (DeviceManager dm, WorkflowEngine engine) Setup()
    {
        var dm = new DeviceManager(NullLogger<DeviceManager>.Instance);

        dm.RegisterDevice(new RobotDevice(
            new DeviceInfo { DeviceId = "robot-01", Name = "Robot", DeviceType = DeviceType.Robot },
            null, NullLogger<RobotDevice>.Instance));
        dm.RegisterDevice(new ScannerDevice(
            new DeviceInfo { DeviceId = "scanner-01", Name = "Scanner", DeviceType = DeviceType.Scanner },
            null, NullLogger<ScannerDevice>.Instance));
        dm.RegisterDevice(new TestDevice(
            new DeviceInfo { DeviceId = "tester-01", Name = "Tester", DeviceType = DeviceType.TestDevice },
            null, NullLogger<TestDevice>.Instance));

        var engine = new WorkflowEngine(dm, NullLogger<WorkflowEngine>.Instance);
        return (dm, engine);
    }

    [Fact]
    public async Task SerialWorkflow_ExecutesInOrder()
    {
        var (dm, engine) = Setup();
        await dm.ConnectAllAsync();

        var workflow = new WorkflowDefinition
        {
            Name = "Serial Test",
            RootGroup = new WorkflowStepGroup
            {
                ExecutionMode = StepExecutionMode.Serial,
                Steps = new List<WorkflowStep>
                {
                    new() { Name = "Move", DeviceId = "robot-01", Action = "move",
                            Parameters = new() { ["x"] = 10.0 }, Order = 1 },
                    new() { Name = "Scan", DeviceId = "scanner-01", Action = "scan", Order = 2 },
                    new() { Name = "Home", DeviceId = "robot-01", Action = "home", Order = 3 },
                },
            },
        };

        var result = await engine.ExecuteWorkflowAsync(workflow);

        Assert.Equal(WorkflowExecutionStatus.Completed, result.Status);
        Assert.Equal(3, result.StepResults.Count);
        Assert.True(result.AllStepsSucceeded);
    }

    [Fact]
    public async Task ParallelWorkflow_ExecutesConcurrently()
    {
        var (dm, engine) = Setup();
        await dm.ConnectAllAsync();

        var workflow = new WorkflowDefinition
        {
            Name = "Parallel Test",
            RootGroup = new WorkflowStepGroup
            {
                ExecutionMode = StepExecutionMode.Parallel,
                Steps = new List<WorkflowStep>
                {
                    new() { Name = "Scan", DeviceId = "scanner-01", Action = "scan", Order = 1 },
                    new() { Name = "Test", DeviceId = "tester-01", Action = "run_test",
                            Parameters = new() { ["test_type"] = "voltage" }, Order = 2 },
                },
            },
        };

        var result = await engine.ExecuteWorkflowAsync(workflow);

        Assert.Equal(WorkflowExecutionStatus.Completed, result.Status);
        Assert.Equal(2, result.StepResults.Count);
        Assert.True(result.AllStepsSucceeded);
    }

    [Fact]
    public async Task MixedWorkflow_SerialWithParallelSubGroup()
    {
        var (dm, engine) = Setup();
        await dm.ConnectAllAsync();

        var workflow = new WorkflowDefinition
        {
            Name = "Mixed Test",
            RootGroup = new WorkflowStepGroup
            {
                ExecutionMode = StepExecutionMode.Serial,
                Steps = new List<WorkflowStep>
                {
                    new() { Name = "Move", DeviceId = "robot-01", Action = "move",
                            Parameters = new() { ["x"] = 100.0 }, Order = 1 },
                },
                SubGroups = new List<WorkflowStepGroup>
                {
                    new()
                    {
                        ExecutionMode = StepExecutionMode.Parallel,
                        Order = 2,
                        Steps = new List<WorkflowStep>
                        {
                            new() { Name = "Scan", DeviceId = "scanner-01", Action = "scan", Order = 1 },
                            new() { Name = "Test", DeviceId = "tester-01", Action = "run_test",
                                    Parameters = new() { ["test_type"] = "voltage" }, Order = 2 },
                        },
                    },
                },
            },
        };

        var result = await engine.ExecuteWorkflowAsync(workflow);

        Assert.Equal(WorkflowExecutionStatus.Completed, result.Status);
        Assert.Equal(3, result.StepResults.Count);
    }

    [Fact]
    public async Task StepWithRetry_RetriesOnFailure()
    {
        var (dm, engine) = Setup();
        await dm.ConnectAllAsync();

        var workflow = new WorkflowDefinition
        {
            Name = "Retry Test",
            RootGroup = new WorkflowStepGroup
            {
                ExecutionMode = StepExecutionMode.Serial,
                Steps = new List<WorkflowStep>
                {
                    new()
                    {
                        Name = "Bad Action",
                        DeviceId = "robot-01",
                        Action = "nonexistent_action",
                        RetryCount = 2,
                        RetryDelay = TimeSpan.FromMilliseconds(10),
                        Order = 1,
                    },
                },
            },
        };

        var result = await engine.ExecuteWorkflowAsync(workflow);

        Assert.Equal(WorkflowExecutionStatus.Failed, result.Status);
        var stepResult = result.StepResults[0];
        Assert.False(stepResult.Success);
        Assert.Equal(3, stepResult.AttemptCount); // 1 initial + 2 retries
    }

    [Fact]
    public async Task ContinueOnError_ContinuesAfterFailure()
    {
        var (dm, engine) = Setup();
        await dm.ConnectAllAsync();

        var workflow = new WorkflowDefinition
        {
            Name = "ContinueOnError Test",
            RootGroup = new WorkflowStepGroup
            {
                ExecutionMode = StepExecutionMode.Serial,
                Steps = new List<WorkflowStep>
                {
                    new()
                    {
                        Name = "Failing Step",
                        DeviceId = "robot-01",
                        Action = "nonexistent",
                        ContinueOnError = true,
                        Order = 1,
                    },
                    new()
                    {
                        Name = "Next Step",
                        DeviceId = "robot-01",
                        Action = "home",
                        Order = 2,
                    },
                },
            },
        };

        var result = await engine.ExecuteWorkflowAsync(workflow);

        Assert.Equal(2, result.StepResults.Count);
        Assert.False(result.StepResults[0].Success);
        Assert.True(result.StepResults[1].Success);
    }

    [Fact]
    public async Task DeviceNotFound_StepFails()
    {
        var (dm, engine) = Setup();
        await dm.ConnectAllAsync();

        var workflow = new WorkflowDefinition
        {
            Name = "Missing Device Test",
            RootGroup = new WorkflowStepGroup
            {
                ExecutionMode = StepExecutionMode.Serial,
                Steps = new List<WorkflowStep>
                {
                    new() { Name = "Missing", DeviceId = "does-not-exist", Action = "move", Order = 1 },
                },
            },
        };

        var result = await engine.ExecuteWorkflowAsync(workflow);
        Assert.Equal(WorkflowExecutionStatus.Failed, result.Status);
    }

    [Fact]
    public async Task CancelWorkflow_StopsExecution()
    {
        var (dm, engine) = Setup();
        await dm.ConnectAllAsync();

        var workflow = new WorkflowDefinition
        {
            Name = "Cancel Test",
            RootGroup = new WorkflowStepGroup
            {
                ExecutionMode = StepExecutionMode.Serial,
                Steps = Enumerable.Range(1, 100).Select(i => new WorkflowStep
                {
                    Name = $"Step {i}",
                    DeviceId = "robot-01",
                    Action = "move",
                    Parameters = new() { ["x"] = (double)i },
                    Order = i,
                }).ToList(),
            },
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var result = await engine.ExecuteWorkflowAsync(workflow, cts.Token);

        Assert.Equal(WorkflowExecutionStatus.Cancelled, result.Status);
        Assert.True(result.StepResults.Count < 100);
    }
}
