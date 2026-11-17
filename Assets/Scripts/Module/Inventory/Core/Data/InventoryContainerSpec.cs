/*
 * ┌──────────────────────────────────┐
 * │  描    述: 容器规格定义
 * │  类    名: InventoryContainerSpec.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Inventory.Core.Config.Type;

namespace Module.Inventory.Core.Data
{
    public readonly struct InventoryContainerSpec
    {
        public int Width { get; }
        public int Height { get; }

        //实心掩码, 按行主序排列; 传 null 代表满格矩形
        public IReadOnlyList<bool> Mask { get; }

        //逻辑区域表, 按行主序排列; 传 null 代表所有实心格同属区域 0
        public IReadOnlyList<int> RegionIds { get; }

        public float WeightLimit { get; }

        //容器显示名, 载体表头的容器标题由它供给
        public string DisplayName { get; }

        //穿戴部位, 非装备槽容器为 None
        public InventoryEquipSlot EquipSlot { get; }

        //外接矩形格数, 异形容器的真实容量看掩码
        public int CellCount =>  Width * Height;

        public InventoryContainerSpec(int width, int height, float weightLimit, IReadOnlyList<bool> mask = null,
            string displayName = "", InventoryEquipSlot equipSlot = InventoryEquipSlot.None,
            IReadOnlyList<int> regionIds = null)
        {
            Width = width;
            Height = height;
            Mask = mask;
            RegionIds = regionIds;
            WeightLimit = weightLimit;
            DisplayName = displayName;
            EquipSlot = equipSlot;
        }
    }
}
