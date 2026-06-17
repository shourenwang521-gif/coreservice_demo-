# CoreService Demo

自动化产线核心调度服务框架 — 支持多设备管理、串行/并行工艺流程执行、异常处理与重试机制、物料库存管理和台面位置追踪。

An automation production line core scheduling service framework — supporting multi-device management, serial/parallel workflow execution, exception handling, retry mechanisms, material inventory management, and benchtop position tracking.

## Architecture / 架构

```
CoreServiceDemo.sln
├── src/CoreService/                  # 核心类库
│   ├── Models/                       # 数据模型
│   │   ├── DeviceInfo.cs             # 设备信息
│   │   ├── DeviceCommand.cs          # 设备指令
│   │   ├── WorkflowDefinition.cs     # 工艺流程定义
│   │   ├── WorkflowStep.cs           # 工艺步骤
│   │   ├── WorkflowStepGroup.cs      # 步骤组（支持嵌套串行/并行）
│   │   ├── Material/                 # 物料管理模块
│   │   │   ├── MaterialInfo.cs       # 物料/耗材信息（孔板、离心管、EP管等）
│   │   │   └── MaterialConsumptionRecord.cs  # 物料消耗记录追踪
│   │   ├── Biomedical/               # 生物医药容器管理
│   │   │   ├── BiomedicalContainerType.cs    # 生物医药容器类型定义
│   │   │   ├── ContainerTracker.cs   # 台面容器实时追踪
│   │   │   └── ContainerLocationMap.cs       # 容器位置映射
│   │   └── Position/                 # 台面位置管理
│   │       ├── BenchtopPosition.cs   # 台面位置坐标模型
│   │       └── PositionOperationLog.cs       # 位置操作日志
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
│   │   ├── WorkflowService.cs        # 工艺流程管理
│   │   ├── MaterialManager.cs        # 物料库存管理
│   │   └── PositionManager.cs        # 台面位置管理
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

### 4. 物料库存管理 (Material Inventory Management) ✨ NEW
- **物料信息管理** — 支持孔板、离心管、EP管、移液枪头等生物医药容器
- **库存追踪** — 实时库存计数、最小值警告、批号管理
- **消耗记录** — 详细记录每个工作流中的物料消耗情况
- **有效期管理** — 生产日期和过期日期追踪
- **供应商管理** — 链接物料到供应商信息

### 5. 生物医药容器管理 (Biomedical Container Management) ✨ NEW
- **容器类型定义** — 96孔板、384孔板、1.5mL离心管、15mL离心管等标准规格
- **容器规格参数** — 容积、尺寸、孔位数、温度范围等
- **实时追踪** — 追踪每个具体容器实例在台面上的位置和状态
- **液体管理** — 记录容器内液体类型、体积、使用次数

### 6. 台面位置管理与追踪 (Benchtop Position & WMS) ✨ NEW
- **位置坐标系统** — 精确定义台面上所有位置（X,Y,Z,旋转角度）
- **位置类型** — 物料放置区、工作区、废液区、临时缓冲、设备位置等
- **位置操作日志** — 记录所有物料移动、放置、取出操作
- **操作追踪** — 记录操作前后坐标、设备ID、执行状态、耗时等
- **容器位置映射** — 维护容器与台面位置的对应关系

### 7. 异常处理体系
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

### 基础工作流执行

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

### 物料库存管理

```csharp
// 1. 创建物料信息
var plate96 = new MaterialInfo
{
    MaterialCode = "PLATE-96-001",
    MaterialName = "96孔微孔板",
    Category = "孔板",
    Specification = "96孔",
    Unit = "盒",
    CurrentStock = 50,
    MinimumStock = 10,
    MaximumStock = 100,
    SupplierCode = "SUPPLIER-001",
    BatchNumber = "BATCH-2026-05-001",
    ExpirationDate = DateTime.Now.AddMonths(12),
    DefaultLocationCoordinate = "A1"
};

// 2. 记录物料消耗
var consumptionRecord = new MaterialConsumptionRecord
{
    MaterialId = plate96.MaterialId,
    WorkflowId = "wf-001",
    DeviceId = "robot-01",
    ConsumptionQuantity = 5,
    StockBefore = 50,
    StockAfter = 45,
    Reason = "使用",
    OperatorId = "operator-001",
    ConsumptionTime = DateTime.Now
};
```

### 台面位置管理

```csharp
// 1. 定义台面位置
var position = new BenchtopPosition
{
    PositionCode = "A1",
    PositionName = "主甲板左上",
    PositionType = "物料放置",
    CoordinateX = 100.0,
    CoordinateY = 200.0,
    CoordinateZ = 50.0,
    RotationAngle = 0.0,
    Status = "空闲"
};

// 2. 记录位置操作
var operationLog = new PositionOperationLog
{
    PositionId = position.PositionId,
    OperationType = "放置",
    DeviceId = "gripper-01",
    WorkflowId = "wf-001",
    ContainerInstanceId = "container-001",
    CoordinatesBefore = "0,0,0,0",
    CoordinatesAfter = "100,200,50,0",
    Status = "成功",
    OperationTime = DateTime.Now,
    DurationMs = 1500
};

// 3. 追踪容器位置
var tracker = new ContainerTracker
{
    ContainerInstanceId = "container-001",
    ContainerTypeId = containerType.ContainerTypeId,
    CurrentPositionId = position.PositionId,
    Status = "使用中",
    CurrentVolumeMicroliters = 100.0,
    LiquidType = "血清样本"
};
```

## Workflow Definition JSON / 工艺流程 JSON 定义

上位机可以通过 JSON 下发工艺流程：

```csharp
var workflowService = new WorkflowService(engine, logger);
var workflow = workflowService.LoadWorkflowFromJson(jsonString);
var result = await workflowService.ExecuteWorkflowAsync(workflow.WorkflowId);
```

## 数据库模型关系

```
MaterialInfo (物料信息)
    ├── ConsumptionRecords (消耗记录) 1:N

BiomedicalContainerType (容器类型)
    ├── ContainerTracker (容器追踪) 1:N

BenchtopPosition (台面位置)
    ├── OperationLogs (位置操作日志) 1:N

ContainerTracker (容器追踪)
    ├── BiomedicalContainerType (容器类型) N:1
    └── BenchtopPosition (位置) N:1
```

## License

MIT
