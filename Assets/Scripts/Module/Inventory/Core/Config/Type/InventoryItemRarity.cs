/*
 * ┌──────────────────────────────────┐
 * │  描    述: 物品稀有度                      
 * │  类    名: InventoryItemRarity.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Inventory.Core.Config.Type
{
    public enum InventoryItemRarity
    {
        None = 0,         // 未定义（白色)
        Common = 1,       // 普通（灰色）
        Uncommon = 2,     // 优秀（绿）
        Rare = 3,         // 稀有（蓝）
        Epic = 4,         // 史诗（紫）
        Legendary = 5     // 传说（金/橙）
    }
}