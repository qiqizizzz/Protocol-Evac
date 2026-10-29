/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品种类                     
 * │  类    名: InventoryItemType.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Inventory.Core.Config.Type
{
    public enum InventoryItemType
    {
        None = 0,
        
        [InspectorName("武器")]
        Weapon=10,
        
        [InspectorName("弹药")]
        Ammo = 20,
        
        [InspectorName("护甲")]
        Armor = 30,
        
        [InspectorName("医疗物品")]
        Medical = 40,
        
        [InspectorName("贵重物品")]
        Valuable = 50,
    }
}