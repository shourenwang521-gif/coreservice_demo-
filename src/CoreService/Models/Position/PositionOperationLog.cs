using System;

namespace CoreService.Models.Position
{
    /// <summary>
    /// 位置操作日志
    /// Position Operation Log
    /// 记录台面上所有物料/容器的移动、放置、取出操作
    /// </summary>
    public class PositionOperationLog
    {
        /// <summary>
        /// 日志唯一标识
        /// Log unique identifier
        /// </summary>
        public int LogId { get; set; }

        /// <summary>
        /// 位置ID（外键）
        /// Position ID (Foreign Key)
        /// </summary>
        public int PositionId { get; set; }

        /// <summary>
        /// 位置信息
        /// Position information
        /// </summary>
        public virtual BenchtopPosition Position { get; set; }

        /// <summary>
        /// 操作类型
        /// Operation type ("移动", "放置", "取出", "校准", "检验")
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// 设备ID（执行操作的设备）
        /// Device ID (device performing the operation)
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 工作流ID
        /// Workflow ID
        /// </summary>
        public string WorkflowId { get; set; }

        /// <summary>
        /// 容器实例ID
        /// Container instance ID
        /// </summary>
        public string ContainerInstanceId { get; set; }

        /// <summary>
        /// 操作前坐标（X,Y,Z,旋转角度）
        /// Coordinates before operation (X,Y,Z,Rotation)
        /// </summary>
        public string CoordinatesBefore { get; set; }

        /// <summary>
        /// 操作后坐标（X,Y,Z,旋转角度）
        /// Coordinates after operation (X,Y,Z,Rotation)
        /// </summary>
        public string CoordinatesAfter { get; set; }

        /// <summary>
        /// 操作状态
        /// Operation status ("成功", "失败", "异常", "警告")
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 错误信息
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 操作耗时（毫秒）
        /// Operation duration in milliseconds
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// 操作员ID
        /// Operator ID
        /// </summary>
        public string OperatorId { get; set; }

        /// <summary>
        /// 操作时间
        /// Operation time
        /// </summary>
        public DateTime OperationTime { get; set; }

        /// <summary>
        /// 备注
        /// Remarks
        /// </summary>
        public string Remarks { get; set; }
    }
}
