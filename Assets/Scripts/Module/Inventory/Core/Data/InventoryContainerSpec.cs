/*
 * ┌──────────────────────────────────┐
 * │  描    述: 容器规格定义
 * │  类    名: InventoryContainerSpec.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;

namespace Module.Inventory.Core.Data
{
    public readonly struct InventoryContainerSpec
    {
        public int Width { get; }
        public int Height { get; }

        //实心掩码, 按行主序排列; 传 null 代表满格矩形
        public IReadOnlyList<bool> Mask { get; }

        public float WeightLimit { get; }

        //容器显示名, 载体表头的容器标题由它供给
        public string DisplayName { get; }

        //外接矩形格数, 异形容器的真实容量看掩码
        public int CellCount =>  Width * Height;

        public InventoryContainerSpec(int width, int height, float weightLimit, IReadOnlyList<bool> mask = null,
            string displayName = "")
        {
            Width = width;
            Height = height;
            Mask = mask;
            WeightLimit = weightLimit;
            DisplayName = displayName;
        }
    }
}