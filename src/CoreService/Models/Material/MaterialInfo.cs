using System;
using System.Collections.Generic;

namespace CoreService.Models.Material
{
    /// <summary>
    /// 物料/耗材信息模型
    /// Material/Consumables Information Model
    /// 用于生物医药液体工作站：孔板、离心管、EP管等容器
    /// </summary>
    public class MaterialInfo
    {
        /// <summary>
        /// 物料唯一标识
        /// Material unique identifier
        /// </summary>
        public int MaterialId { get; set; }

        /// <summary>
        /// 物料编码
        /// Material code
        /// </summary>
        public string MaterialCode { get; set; }

        /// <summary>
        /// 物料名称
        /// Material name
        /// </summary>
        public string MaterialName { get; set; }

        /// <summary>
        /// 物料分类
        /// Material category (孔板、离心管、EP管、移液枪头、试管等)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 容器规格
        /// Container specification (96孔、384孔、1.5mL、15mL等)
        /// </summary>
        public string Specification { get; set; }

        /// <summary>
        /// 单位
        /// Unit (e.g., "盒", "支", "包", "个")
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// 当前库存
        /// Current stock (台面上的数量)
        /// </summary>
        public double CurrentStock { get; set; }

        /// <summary>
        /// 最小库存警告值
        /// Minimum stock warning level
        /// </summary>
        public double MinimumStock { get; set; }

        /// <summary>
        /// 最大库存
        /// Maximum stock capacity
        /// </summary>
        public double MaximumStock { get; set; }

        /// <summary>
        /// 供应商编码
        /// Supplier code
        /// </summary>
        public string SupplierCode { get; set; }

        /// <summary>
        /// 批号/批次
        /// Batch number
        /// </summary>
        public string BatchNumber { get; set; }

        /// <summary>
        /// 生产日期
        /// Production date
        /// </summary>
        public DateTime? ProductionDate { get; set; }

        /// <summary>
        /// 过期日期
        /// Expiration date
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// 存储位置（台面坐标）
        /// Storage location (benchtop coordinate)
        /// </summary>
        public string DefaultLocationCoordinate { get; set; }

        /// <summary>
        /// 物料描述
        /// Material description
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
        /// 关联的耗材消耗记录
        /// Associated consumption records
        /// </summary>
        public virtual ICollection<MaterialConsumptionRecord> ConsumptionRecords { get; set; } = new List<MaterialConsumptionRecord>();
    }
}
