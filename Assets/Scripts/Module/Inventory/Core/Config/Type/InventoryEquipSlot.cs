/*
 * ┌──────────────────────────────────┐
 * │  描    述: 装备穿戴部位
 * │  类    名: InventoryEquipSlot.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Inventory.Core.Config.Type
{
    //装备可穿戴到的部位, 装备槽容器按此收物
    public enum InventoryEquipSlot
    {
        //不可穿戴
        None = 0,

        [InspectorName("头部")]
        Helmet = 1,

        [InspectorName("胸挂")]
        ChestRig = 2,

        [InspectorName("背包")]
        Backpack = 3,

        [InspectorName("主武器")]
        PrimaryWeapon = 4,

        [InspectorName("副武器")]
        SecondaryWeapon = 5,

        [InspectorName("近战")]
        Melee = 6,
    }
}