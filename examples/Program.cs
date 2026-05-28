using CoreService.Devices;
using CoreService.Models;
using CoreService.Scheduling;
using CoreService.Services;
using Microsoft.Extensions.Logging;

// ============================================================
// CoreService Demo — 自动化产线工艺流程调度示例
// ============================================================

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);
    builder.AddConsole();
});

var dmLogger = loggerFactory.CreateLogger<DeviceManager>();
var weLogger = loggerFactory.CreateLogger<WorkflowEngine>();
var wsLogger = loggerFactory.CreateLogger<WorkflowService>();

// --- 1. 初始化设备管理器并注册设备 ---
await using var deviceManager = new DeviceManager(dmLogger);

var robot = new RobotDevice(
    new DeviceInfo { DeviceId = "robot-01", Name = "6轴机械臂", DeviceType = DeviceType.Robot },
    channel: null,
    loggerFactory.CreateLogger<RobotDevice>());

var scanner = new ScannerDevice(
    new DeviceInfo { DeviceId = "scanner-01", Name = "条码扫描枪", DeviceType = DeviceType.Scanner },
    channel: null,
    loggerFactory.CreateLogger<ScannerDevice>());

var tester = new TestDevice(
    new DeviceInfo { DeviceId = "tester-01", Name = "万用表", DeviceType = DeviceType.TestDevice },
    channel: null,
    loggerFactory.CreateLogger<TestDevice>());

var conveyor = new ConveyorDevice(
    new DeviceInfo { DeviceId = "conv-01", Name = "主传送带", DeviceType = DeviceType.Conveyor },
    channel: null,
    loggerFactory.CreateLogger<ConveyorDevice>());

var gripper = new GripperDevice(
    new DeviceInfo { DeviceId = "gripper-01", Name = "气动夹爪", DeviceType = DeviceType.Gripper },
    channel: null,
    loggerFactory.CreateLogger<GripperDevice>());

deviceManager.RegisterDevice(robot);
deviceManager.RegisterDevice(scanner);
deviceManager.RegisterDevice(tester);
deviceManager.RegisterDevice(conveyor);
deviceManager.RegisterDevice(gripper);

// --- 2. 连接所有设备 ---
Console.WriteLine("\n===== 连接所有设备 =====");
var connectResults = await deviceManager.ConnectAllAsync();
foreach (var kv in connectResults)
    Console.WriteLine($"  {kv.Key}: {(kv.Value ? "OK" : "FAILED")}");

// --- 3. 构建工艺流程（串行 + 并行混合） ---
var workflow = new WorkflowDefinition
{
    Name = "自动化测试产线流程",
    Description = "传送带送料 -> 扫码 -> 机械臂取料 -> 并行(电压测试+电阻测试) -> 放料",
    RootGroup = new WorkflowStepGroup
    {
        Name = "主流程",
        ExecutionMode = StepExecutionMode.Serial,
        Steps = new List<WorkflowStep>
        {
            new()
            {
                Name = "启动传送带",
                DeviceId = "conv-01",
                Action = "start",
                Parameters = new() { ["speed"] = 60.0 },
                Order = 1,
            },
            new()
            {
                Name = "扫码识别工件",
                DeviceId = "scanner-01",
                Action = "scan",
                Parameters = new() { ["expected_data"] = "PART-A001" },
                Order = 2,
                RetryCount = 2,
                RetryDelay = TimeSpan.FromMilliseconds(500),
            },
            new()
            {
                Name = "停止传送带",
                DeviceId = "conv-01",
                Action = "stop",
                Order = 3,
            },
            new()
            {
                Name = "夹爪关闭",
                DeviceId = "gripper-01",
                Action = "close",
                Parameters = new() { ["force"] = 80.0 },
                Order = 4,
            },
            new()
            {
                Name = "机械臂取料",
                DeviceId = "robot-01",
                Action = "pick",
                Order = 5,
            },
            new()
            {
                Name = "移动到测试工位",
                DeviceId = "robot-01",
                Action = "move",
                Parameters = new() { ["x"] = 500.0, ["y"] = 200.0, ["z"] = 100.0 },
                Order = 6,
            },
        },
        SubGroups = new List<WorkflowStepGroup>
        {
            new()
            {
                Name = "并行测试组",
                ExecutionMode = StepExecutionMode.Parallel,
                Order = 7,
                Steps = new List<WorkflowStep>
                {
                    new()
                    {
                        Name = "电压测试",
                        DeviceId = "tester-01",
                        Action = "run_test",
                        Parameters = new()
                        {
                            ["test_type"] = "voltage",
                            ["unit"] = "V",
                            ["min_threshold"] = 3.0,
                            ["max_threshold"] = 5.5,
                        },
                        Order = 1,
                        RetryCount = 1,
                    },
                    new()
                    {
                        Name = "电阻测试",
                        DeviceId = "tester-01",
                        Action = "run_test",
                        Parameters = new()
                        {
                            ["test_type"] = "resistance",
                            ["unit"] = "kOhm",
                        },
                        Order = 2,
                    },
                },
            },
        },
    },
};

// 并行组之后的收尾步骤
workflow.RootGroup.Steps.AddRange(new[]
{
    new WorkflowStep
    {
        Name = "机械臂放料",
        DeviceId = "robot-01",
        Action = "place",
        Order = 8,
    },
    new WorkflowStep
    {
        Name = "夹爪打开",
        DeviceId = "gripper-01",
        Action = "open",
        Order = 9,
    },
    new WorkflowStep
    {
        Name = "机械臂回原点",
        DeviceId = "robot-01",
        Action = "home",
        Order = 10,
    },
});

// --- 4. 执行工艺流程 ---
Console.WriteLine("\n===== 开始执行工艺流程 =====");
var engine = new WorkflowEngine(deviceManager, weLogger);
var workflowService = new WorkflowService(engine, wsLogger);

var result = await workflowService.ExecuteWorkflowAsync(workflow);

// --- 5. 输出结果 ---
Console.WriteLine($"\n===== 执行结果 =====");
Console.WriteLine($"工艺流程: {workflow.Name}");
Console.WriteLine($"状态:     {result.Status}");
Console.WriteLine($"总耗时:   {result.TotalElapsed.TotalMilliseconds:F0} ms");
Console.WriteLine($"步骤数:   {result.StepResults.Count}");
Console.WriteLine();

foreach (var sr in result.StepResults)
{
    string icon = sr.Success ? "+" : "x";
    Console.WriteLine($"  [{icon}] {sr.StepName,-20} | {sr.ElapsedTime.TotalMilliseconds,6:F0} ms | retry: {sr.AttemptCount} | {(sr.Success ? "OK" : sr.ErrorMessage)}");
}

if (!string.IsNullOrEmpty(result.ErrorMessage))
    Console.WriteLine($"\nError: {result.ErrorMessage}");

// --- 6. 查询设备最终状态 ---
Console.WriteLine("\n===== 设备状态 =====");
var allStatus = await deviceManager.GetAllStatusAsync();
foreach (var (deviceId, status) in allStatus)
{
    Console.WriteLine($"  {deviceId}: {string.Join(", ", status.Select(kv => $"{kv.Key}={kv.Value}"))}");
}

Console.WriteLine("\n===== Demo 完成 =====");
