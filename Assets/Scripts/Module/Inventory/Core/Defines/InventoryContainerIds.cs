/*
* ┌──────────────────────────────────┐
* │  描    述: 背包容器身份定义
* │  类    名: InventoryContainerIds.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Inventory.Core.Config.Type;

namespace Module.Inventory.Core.Defines
{
    public static class InventoryContainerIds
    {
        //角色主背包
        public const int BACKPACK = 1;

        //胸挂
        public const int RIG = 2;

        //口袋
        public const int POCKETS = 3;

        //安全箱
        public const int SAFEBOX = 4;

        //头盔装备槽
        public const int HELMET_SLOT = 5;

        //胸挂装备槽
        public const int CHESTRIG_SLOT = 6;

        //背包装备槽
        public const int BACKPACK_SLOT = 7;

        //主武器装备槽
        public const int PRIMARY_WEAPON_SLOT = 8;

        //副武器装备槽
        public const int SECONDARY_WEAPON_SLOT = 9;

        //近战装备槽
        public const int MELEE_SLOT = 10;

        //手枪装备槽
        public const int PISTOL_SLOT = 11;

        //护甲装备槽
        public const int ARMOR_SLOT = 12;

        //安全箱装备槽
        public const int SAFEBOX_SLOT = 13;

        //地上战利品箱起始号, 运行时往后递增分配
        public const int LOOT = 20;

        //角色身上的四个容量容器, 读档与测试按此遍历
        public static readonly int[] ROLE_SLOT_IDS =
        {
            BACKPACK, RIG, POCKETS, SAFEBOX
        };

        //角色全部装备槽, 其中胸挂与背包槽用于支撑容量载体, 不直接显示在左列
        public static readonly int[] EQUIP_SLOT_IDS =
        {
            HELMET_SLOT, ARMOR_SLOT, CHESTRIG_SLOT, BACKPACK_SLOT, PISTOL_SLOT, MELEE_SLOT, PRIMARY_WEAPON_SLOT, SECONDARY_WEAPON_SLOT, SAFEBOX_SLOT
        };

        //战备面板载体的装配顺序, 必须与预制体 Containers[] 逐项对应
        public static readonly int[] EQUIPMENT_CONTAINER_IDS =
        {
            HELMET_SLOT, ARMOR_SLOT, PISTOL_SLOT, PRIMARY_WEAPON_SLOT, SECONDARY_WEAPON_SLOT, MELEE_SLOT,
            RIG, POCKETS, BACKPACK, SAFEBOX, CHESTRIG_SLOT, BACKPACK_SLOT, SAFEBOX_SLOT
        };

        /// <summary>
        /// 容器号反查装备槽部位, 非装备槽返回 false
        /// </summary>
        /// <param name="containerId">容器号</param>
        /// <param name="slot">命中的穿戴部位</param>
        public static bool TryGetEquipSlotRole(int containerId, out InventoryEquipSlot slot)
        {
            slot = containerId switch
            {
                HELMET_SLOT => InventoryEquipSlot.Helmet,
                CHESTRIG_SLOT => InventoryEquipSlot.ChestRig,
                BACKPACK_SLOT => InventoryEquipSlot.Backpack,
                PRIMARY_WEAPON_SLOT => InventoryEquipSlot.PrimaryWeapon,
                SECONDARY_WEAPON_SLOT => InventoryEquipSlot.SecondaryWeapon,
                MELEE_SLOT => InventoryEquipSlot.Melee,
                PISTOL_SLOT => InventoryEquipSlot.Pistol,
                ARMOR_SLOT => InventoryEquipSlot.Armor,
                SAFEBOX_SLOT => InventoryEquipSlot.SafeBox,
                _ => InventoryEquipSlot.None
            };

            return slot != InventoryEquipSlot.None;
        }

        /// <summary>
        /// 穿戴部位查它撑起的容量容器号, 头盔与武器等不带容量的部位返回 false
        /// </summary>
        /// <param name="slot">穿戴部位</param>
        /// <param name="containerId">对应的容量容器号</param>
        public static bool TryGetCapacityContainerId(InventoryEquipSlot slot, out int containerId)
        {
            switch (slot)
            {
                case InventoryEquipSlot.Backpack:
                    containerId = BACKPACK;
                    return true;
                case InventoryEquipSlot.ChestRig:
                    containerId = RIG;
                    return true;
                default:
                    containerId = 0;
                    return false;
            }
        }
    }
}
