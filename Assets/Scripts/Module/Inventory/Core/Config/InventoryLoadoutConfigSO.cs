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

        [Header("安全箱基础容量")]
        [Tooltip("安全箱是常驻网格, 卸下型号后回落到这份基础容器定义; 留空表示安全箱不常驻")]
        [SerializeField] private InventoryContainerConfigSO SafeBoxBaseValue;

        //穿戴进槽的入口只取背包与胸挂两件容器装备
        public InventoryItemConfigSO Backpack => BackpackValue;
        public InventoryItemConfigSO Rig => RigValue;
        public InventoryItemConfigSO SafeBox => SafeBoxValue;

        //安全箱卸下型号后的常驻底格
        public InventoryContainerConfigSO SafeBoxBase => SafeBoxBaseValue;

        public bool TryGetContainer(int slotId, out InventoryContainerConfigSO container)
        {
            container = slotId switch
            {
                InventoryContainerIds.BACKPACK => GetWornContainer(BackpackValue),
                InventoryContainerIds.RIG => GetWornContainer(RigValue),
                InventoryContainerIds.POCKETS => PocketsValue,
                InventoryContainerIds.SAFEBOX => GetWornContainer(SafeBoxValue) ?? SafeBoxBaseValue,
                _ => null
            };

            return container != null;
        }

        /// <summary>
        /// 取常驻容量容器卸下型号后的基础网格, 非常驻槽位或没配基础网格时返回 false
        /// </summary>
        /// <param name="slotId">角色容器槽位号</param>
        /// <param name="container">回落用的容器定义</param>
        public bool TryGetPersistentContainer(int slotId, out InventoryContainerConfigSO container)
        {
            container = slotId switch
            {
                InventoryContainerIds.SAFEBOX => SafeBoxBaseValue,
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
