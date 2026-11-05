/*
* ┌──────────────────────────────────┐
* │  描    述: 穿戴配置, 声明角色每个容器槽位当前穿着哪件容器装备
* │  类    名: InventoryLoadoutConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Inventory.Core.Defines;
using UnityEngine;

namespace Module.Inventory.Core.Config
{
    [CreateAssetMenu(fileName = "LoadoutConfig", menuName = "配置/背包/穿戴配置")]
    public sealed class InventoryLoadoutConfigSO : ScriptableObject
    {
        [Header("背包槽")]
        [Tooltip("该槽位穿着的容器装备, 留空表示没穿")]
        [SerializeField] private InventoryItemConfigSO BackpackValue;

        [Header("胸挂槽")]
        [Tooltip("该槽位穿着的容器装备, 留空表示没穿")]
        [SerializeField] private InventoryItemConfigSO RigValue;

        [Header("口袋")]
        [Tooltip("口袋是角色固有容量, 不随装备变化, 因此直接引用容器定义")]
        [SerializeField] private InventoryContainerConfigSO PocketsValue;

        [Header("安全箱槽")]
        [Tooltip("该槽位穿着的容器装备, 留空表示没穿")]
        [SerializeField] private InventoryItemConfigSO SafeBoxValue;

        //穿戴进槽的入口只取背包与胸挂两件容器装备
        public InventoryItemConfigSO Backpack => BackpackValue;
        public InventoryItemConfigSO Rig => RigValue;
        public InventoryItemConfigSO SafeBox => SafeBoxValue;

        public bool TryGetContainer(int slotId, out InventoryContainerConfigSO container)
        {
            container = slotId switch
            {
                InventoryContainerIds.BACKPACK => GetWornContainer(BackpackValue),
                InventoryContainerIds.RIG => GetWornContainer(RigValue),
                InventoryContainerIds.POCKETS => PocketsValue,
                InventoryContainerIds.SAFEBOX => GetWornContainer(SafeBoxValue),
                _ => null
            };

            return container != null;
        }

        //从穿戴的物品上取容器定义
        private static InventoryContainerConfigSO GetWornContainer(InventoryItemConfigSO item)
        {
            return item != null ? item.Container : null;
        }
    }
}
