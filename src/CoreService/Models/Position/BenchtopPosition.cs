using System;
using System.Collections.Generic;

namespace CoreService.Models.Position
{
    /// <summary>
    /// 台面位置坐标模型
    /// Benchtop Position Coordinate Model
    /// 定义液体工作站台面上的所有位置点
    /// </summary>
    public class BenchtopPosition
    {
        /// <summary>
        /// 位置唯一标识
        /// Position unique identifier
        /// </summary>
        public int PositionId { get; set; }

        /// <summary>
        /// 位置编码
        /// Position code (e.g., "A1", "B2", "Deck1", "Gripper1")
        /// </summary>
        public string PositionCode { get; set; }

        /// <summary>
        /// 位置名称
        /// Position name (e.g., "主甲板左上", "夹爪位置1", "废液桶")
        /// </summary>
        public string PositionName { get; set; }

        /// <summary>
        /// 位置类型
        /// Position type ("物料放置", "工作区", "废液区", "临时缓冲", "设备位置" 等)
        /// </summary>
        public string PositionType { get; set; }

        /// <summary>
        /// X坐标（毫米）
        /// X coordinate in millimeters
        /// </summary>
        public double CoordinateX { get; set; }

        /// <summary>
        /// Y坐标（毫米）
        /// Y coordinate in millimeters
        /// </summary>
        public double CoordinateY { get; set; }

        /// <summary>
        /// Z坐标 - 高度（毫米）
        /// Z coordinate - height in millimeters
        /// </summary>
        public double CoordinateZ { get; set; }

        /// <summary>
        /// 旋转角度（度数 0-360）
        /// Rotation angle in degrees (0-360)
        /// </summary>
        public double RotationAngle { get; set; }

        /// <summary>
        /// 关联的物料ID（该位置默认放置的物料类型）
        /// Associated Material ID (default material type at this position)
        /// </summary>
        public int? DefaultMaterialId { get; set; }

        /// <summary>
        /// 位置状态
        /// Position status ("空闲", "使用中", "故障", "维护中")
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 当前容纳的设备ID
        /// Device ID currently at this position
        /// </summary>
        public string CurrentDeviceId { get; set; }

        /// <summary>
        /// 描述/备注
        /// Description/Remarks
        /// </summary>
        public string Description { get; set; }

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

        /// <summary>
        /// 是否启用
        /// Is enabled
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 关联的位置操作记录
        /// Associated position operation records
        /// </summary>
        public virtual ICollection<PositionOperationLog> OperationLogs { get; set; } = new List<PositionOperationLog>();
    }
}
