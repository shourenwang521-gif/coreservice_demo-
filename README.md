# CoreService Demo

自动化产线核心调度服务框架 — 支持多设备管理、串行/并行工艺流程执行、异常处理与重试机制。

An automation production line core scheduling service framework — supporting multi-device management, serial/parallel workflow execution, exception handling, and retry mechanisms.

## Architecture / 架构

```
CoreServiceDemo.sln
├── src/CoreService/                  # 核心类库
│   ├── Models/                       # 数据模型
│   │   ├── DeviceInfo.cs             # 设备信息
│   │   ├── DeviceCommand.cs          # 设备指令
│   │   ├── WorkflowDefinition.cs     # 工艺流程定义
│   │   ├── WorkflowStep.cs           # 工艺步骤
│   │   └── WorkflowStepGroup.cs      # 步骤组（支持嵌套串行/并行）
│   ├── Communication/                # 通讯层（多协议抽象）
│   │   ├── ICommunicationChannel.cs  # 通讯通道接口
│   │   ├── TcpChannel.cs             # TCP Socket 通讯
│   │   ├── SerialPortChannel.cs      # 串口 RS232/RS485 通讯
│   │   ├── ModbusTcpChannel.cs       # Modbus TCP 协议
│   │   └── HttpChannel.cs            # HTTP/REST 通讯
│   ├── Devices/                      # 设备驱动层
│   │   ├── IDevice.cs                # 设备接口
│   │   ├── BaseDevice.cs             # 设备基类（生命周期、锁、超时）
│   │   ├── RobotDevice.cs            # 机器人设备
│   │   ├── ScannerDevice.cs          # 扫码枪设备
│   │   ├── TestDevice.cs             # 测试仪器设备
│   │   ├── ConveyorDevice.cs         # 传送带设备
│   │   ├── PLCDevice.cs              # PLC 设备
│   │   ├── VisionDevice.cs           # 视觉系统设备
│   │   └── GripperDevice.cs          # 夹爪设备
│   ├── Scheduling/                   # 调度引擎
│   │   ├── IWorkflowEngine.cs        # 调度引擎接口
│   │   └── WorkflowEngine.cs         # 核心调度实现
│   ├── Services/                     # 服务层
│   │   ├── DeviceManager.cs          # 设备注册/连接管理
│   │   └── WorkflowService.cs        # 工艺流程管理
│   └── Exceptions/                   # 自定义异常
│       ├── DeviceException.cs        # 设备异常体系
│       └── WorkflowException.cs      # 流程异常体系
├── examples/                         # 示例程序
│   └── Program.cs                    # 完整演示用例
└── tests/CoreService.Tests/          # 单元测试
```

## Key Features / 核心功能

### 1. 工艺流程调度引擎 (Workflow Engine)
- **串行执行** (`StepExecutionMode.Serial`) — 按顺序依次执行步骤
- **并行执行** (`StepExecutionMode.Parallel`) — 同时执行多个步骤
- **嵌套组合** — 串行组内可嵌套并行子组，反之亦然
- **重试机制** — 每个步骤可配置 `RetryCount` 和 `RetryDelay`
- **错误容忍** — `ContinueOnError` 允许某些步骤失败后继续执行
- **流程取消** — 支持通过 `CancellationToken` 随时取消执行

### 2. 多协议通讯层 (Communication Layer)
- **TCP Socket** — 适用于大多数工业设备
- **串口 RS232/RS485** — 传统工业设备通讯
- **Modbus TCP** — PLC 标准协议，支持 FC03/FC06 功能码
- **HTTP/REST** — RESTful API 设备接口
- 统一的 `ICommunicationChannel` 抽象接口

### 3. 设备管理 (Device Management)
- 7 种设备类型：Robot / Scanner / TestDevice / Conveyor / PLC / VisionSystem / Gripper
- 统一的设备生命周期：Offline → Initializing → Idle → Running → Error
- 线程安全的并发设备操作
- 批量连接/断开所有设备

### 4. 异常处理体系
- `DeviceConnectionException` — 设备连接失败
- `DeviceTimeoutException` — 设备指令超时
- `DeviceCommunicationException` — 通讯异常
- `WorkflowException` / `StepExecutionException` — 流程/步骤级别异常

## Quick Start / 快速开始

### Prerequisites
- .NET 8.0 SDK

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run Demo
```bash
dotnet run --project examples/CoreService.Example.csproj
```

## Usage Example / 使用示例

```csharp
// 1. 创建设备管理器
var deviceManager = new DeviceManager(logger);

// 2. 注册设备（可选择通讯方式）
var robot = new RobotDevice(
    new DeviceInfo { DeviceId = "robot-01", Name = "6轴机械臂", DeviceType = DeviceType.Robot },
    channel: new TcpChannel("tcp-robot", new TcpChannelOptions { Host = "192.168.1.10", Port = 5000 }, tcpLogger),
    robotLogger);
deviceManager.RegisterDevice(robot);

// 3. 连接所有设备
await deviceManager.ConnectAllAsync();

// 4. 定义工艺流程
var workflow = new WorkflowDefinition
{
    Name = "自动化测试流程",
    RootGroup = new WorkflowStepGroup
    {
        ExecutionMode = StepExecutionMode.Serial,
        Steps = new List<WorkflowStep>
        {
            new() { Name = "扫码", DeviceId = "scanner-01", Action = "scan", Order = 1 },
            new() { Name = "取料", DeviceId = "robot-01", Action = "pick", Order = 2, RetryCount = 2 },
        },
        SubGroups = new List<WorkflowStepGroup>
        {
            new()
            {
                ExecutionMode = StepExecutionMode.Parallel,
                Order = 3,
                Steps = new List<WorkflowStep>
                {
                    new() { Name = "电压测试", DeviceId = "tester-01", Action = "run_test", Order = 1 },
                    new() { Name = "视觉检测", DeviceId = "vision-01", Action = "inspect", Order = 2 },
                },
            },
        },
    },
};

// 5. 执行工艺流程
var engine = new WorkflowEngine(deviceManager, engineLogger);
var result = await engine.ExecuteWorkflowAsync(workflow);

// 6. 检查结果
Console.WriteLine($"Status: {result.Status}, Steps: {result.StepResults.Count}");
```

## Workflow Definition JSON / 工艺流程 JSON 定义

上位机可以通过 JSON 下发工艺流程：

```csharp
var workflowService = new WorkflowService(engine, logger);
var workflow = workflowService.LoadWorkflowFromJson(jsonString);
var result = await workflowService.ExecuteWorkflowAsync(workflow.WorkflowId);
```

## License

MIT
