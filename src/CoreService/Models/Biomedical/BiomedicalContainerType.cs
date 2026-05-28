using System;
using System.Collections.Generic;

namespace CoreService.Models.Biomedical
{
    /// <summary>
    /// 生物医药容器类型定义
    /// Biomedical Container Type Definition
    /// 定义孔板、离心管、EP管等生物医药容器的标准规格
    /// </summary>
    public class BiomedicalContainerType
    {
        /// <summary>
        /// 容器类型唯一标识
        /// Container type unique identifier
        /// </summary>
        public int ContainerTypeId { get; set; }

        /// <summary>
        /// 容器类型编码
        /// Container type code (e.g., "PLATE96", "TUBE1.5ML", "EPPC")
        /// </summary>
        public string TypeCode { get; set; }

        /// <summary>
        /// 容器类型名称
        /// Container type name (e.g., "96孔板", "1.5mL离心管", "EP管")
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// 容器分类
        /// Container category ("孔板", "离心管", "EP管", "移液枪头", "试管" 等)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 容器规格
        /// Container specification (e.g., "96孔", "384孔", "1.5mL", "15mL")
        /// </summary>
        public string Specification { get; set; }

        /// <summary>
        /// 容器容积（微升）
        /// Container capacity in microliters
        /// </summary>
        public double CapacityMicroliters { get; set; }

        /// <summary>
        /// 容器高度（毫米）
        /// Container height in millimeters
        /// </summary>
        public double HeightMm { get; set; }

        /// <summary>
        /// 容器宽度（毫米）
        /// Container width in millimeters
        /// </summary>
        public double WidthMm { get; set; }

        /// <summary>
        /// 容器深度（毫米）
        /// Container depth in millimeters
        /// </summary>
        public double DepthMm { get; set; }

        /// <summary>
        /// 孔位数量（对于孔板）
        /// Number of wells (for plates)
        /// </summary>
        public int? WellCount { get; set; }

        /// <summary>
        /// 是否可重复使用
        /// Is reusable
        /// </summary>
        public bool IsReusable { get; set; }

        /// <summary>
        /// 温度范围（例如：-20~+80°C）
        /// Temperature range (e.g., "-20~+80°C")
        /// </summary>
        public string TemperatureRange { get; set; }

        /// <summary>
        /// 容器描述
        /// Container description
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
    }
}
