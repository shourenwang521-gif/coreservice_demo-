using System;

namespace CoreService.Models.Biomedical
{
    /// <summary>
    /// 台面容器实时追踪
    /// Benchtop Container Real-time Tracker
    /// 追踪台面上每一个具体的容器实例的位置和状态
    /// </summary>
    public class ContainerTracker
    {
        /// <summary>
        /// 容器实例唯一标识
        /// Container instance unique identifier
        /// </summary>
        public int TrackerId { get; set; }

        /// <summary>
        /// 容器实例ID
        /// Container instance ID (唯一标识一个具体的容器)
        /// </summary>
        public string ContainerInstanceId { get; set; }

        /// <summary>
        /// 容器类型ID（外键）
        /// Container type ID (Foreign Key)
        /// </summary>
        public int ContainerTypeId { get; set; }

        /// <summary>
        /// 容器类型
        /// Container type information
        /// </summary>
        public virtual BiomedicalContainerType ContainerType { get; set; }

        /// <summary>
        /// 当前位置ID（外键）
        /// Current position ID (Foreign Key)
        /// </summary>
        public int CurrentPositionId { get; set; }

        /// <summary>
        /// 当前位置坐标（X,Y,Z）
        /// Current position coordinates (X,Y,Z)
        /// </summary>
        public string CurrentCoordinates { get; set; }

        /// <summary>
        /// 容器状态
        /// Container status ("空闲", "使用中", "等待中", "清洗中", "废弃")
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 当前液体体积（微升）
        /// Current liquid volume in microliters
        /// </summary>
        public double CurrentVolumeMicroliters { get; set; }

        /// <summary>
        /// 液体类型（血清、样本、试剂等）
        /// Liquid type (serum, sample, reagent, etc.)
        /// </summary>
        public string LiquidType { get; set; }

        /// <summary>
        /// 使用次数
        /// Usage count
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 最后使用的工作流ID
        /// Last used workflow ID
        /// </summary>
        public string LastWorkflowId { get; set; }

        /// <summary>
        /// 最后使用时间
        /// Last usage time
        /// </summary>
        public DateTime LastUsedTime { get; set; }

        /// <summary>
        /// 放置时间
        /// Placement time
        /// </summary>
        public DateTime PlacementTime { get; set; }

        /// <summary>
        /// 创建时间
        /// Creation time
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// Last updated time
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
