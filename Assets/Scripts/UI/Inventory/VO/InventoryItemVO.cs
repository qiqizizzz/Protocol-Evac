/*
 * ┌──────────────────────────────────┐
 * │  描    述:背包物品渲染载体, View层的物品数据切片                      
 * │  类    名: InventoryItemVO.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Inventory.Core.Config.Type;
using UnityEngine;

namespace UI.Inventory
{
    public sealed class InventoryItemVO
    {
        public int InstanceId { get; }
        public int ItemId { get; }
        public string DisplayName { get; }
        public int StackCount { get; }
        public Sprite Icon { get; }
        public InventoryItemRarity Rarity { get; }
        public int UnitValue { get; }
        public int TotalValue => UnitValue * StackCount;
        
        //物品左下角所在格子
        public Vector2Int Origin { get; }
        
        //占格尺寸
        public Vector2Int Size { get; }
        
        public InventoryRotation Rotation { get; }
        public int CellCount => Size.x * Size.y;

        public InventoryItemVO(int instanceId, int itemId, string displayName, int stackCount, Sprite icon,
            InventoryItemRarity rarity, int unitValue,
            Vector2Int origin, Vector2Int size, InventoryRotation rotation)
        {
            InstanceId = instanceId;
            ItemId = itemId;
            DisplayName = displayName;
            StackCount = stackCount;
            Icon = icon;
            Rarity = rarity;
            UnitValue = unitValue;
            Origin = origin;
            Size = size;
            Rotation = rotation;
        }
    }
}