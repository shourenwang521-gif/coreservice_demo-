using System;
using System.Collections.Generic;

namespace CoreService.Models.Biomedical
{
    /// <summary>
    /// 容器位置映射
    /// Container Location Map
    /// 维护容器与台面位置之间的映射关系
    /// </summary>
    public class ContainerLocationMap
    {
        /// <summary>
        /// 映射唯一标识
        /// Map unique identifier
        /// </summary>
        public int MapId { get; set; }

        /// <summary>
        /// 容器实例ID
        /// Container instance ID
        /// </summary>
        public string ContainerInstanceId { get; set; }

        /// <summary>
        /// 位置编码
        /// Position code (e.g., "A1", "B2")
        /// </summary>
        public string PositionCode { get; set; }

        /// <summary>
        /// 位置坐标（X,Y,Z）
        /// Position coordinates (X,Y,Z)
        /// </summary>
        public string Coordinates { get; set; }

        /// <summary>
        /// 映射状态
        /// Map status ("活跃", "历史", "无效")
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 映射开始时间
        /// Map start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 映射结束时间
        /// Map end time
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 关联的工作流ID
        /// Associated workflow ID
        /// </summary>
        public string WorkflowId { get; set; }

        /// <summary>
        /// 备注
        /// Remarks
        /// </summary>
        public string Remarks { get; set; }

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
