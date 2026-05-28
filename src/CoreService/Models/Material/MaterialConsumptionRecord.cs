using System;

namespace CoreService.Models.Material
{
    /// <summary>
    /// 物料/容器消耗追踪记录
    /// Material/Container Consumption Tracking Record
    /// 记录孔板、离心管等容器的使用情况
    /// </summary>
    public class MaterialConsumptionRecord
    {
        /// <summary>
        /// 消耗记录唯一标识
        /// Record unique identifier
        /// </summary>
        public int ConsumptionRecordId { get; set; }

        /// <summary>
        /// 物料ID（外键）
        /// Material ID (Foreign Key)
        /// </summary>
        public int MaterialId { get; set; }

        /// <summary>
        /// 物料信息
        /// Material information
        /// </summary>
        public virtual MaterialInfo Material { get; set; }

        /// <summary>
        /// 工作流ID（工艺流程）
        /// Workflow ID (experiment procedure)
        /// </summary>
        public string WorkflowId { get; set; }

        /// <summary>
        /// 步骤ID
        /// Step ID
        /// </summary>
        public string StepId { get; set; }

        /// <summary>
        /// 设备ID（使用该物料的设备）
        /// Device ID (device using this material)
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 容器实例ID（关联到具体的容器)
        /// Container instance ID (linked to specific container)
        /// </summary>
        public string ContainerInstanceId { get; set; }

        /// <summary>
        /// 消耗量
        /// Consumption quantity
        /// </summary>
        public double ConsumptionQuantity { get; set; }

        /// <summary>
        /// 消耗前库存
        /// Stock before consumption
        /// </summary>
        public double StockBefore { get; set; }

        /// <summary>
        /// 消耗后库存
        /// Stock after consumption
        /// </summary>
        public double StockAfter { get; set; }

        /// <summary>
        /// 消耗原因
        /// Reason for consumption (使用、废弃、报废等)
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 操作员/系统标识
        /// Operator/System identifier
        /// </summary>
        public string OperatorId { get; set; }

        /// <summary>
        /// 消耗时间
        /// Consumption time
        /// </summary>
        public DateTime ConsumptionTime { get; set; }

        /// <summary>
        /// 创建时间
        /// Creation time
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 备注
        /// Remarks
        /// </summary>
        public string Remarks { get; set; }
    }
}
