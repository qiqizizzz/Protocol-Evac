/*
* ┌──────────────────────────────────┐
* │  描    述: 背包容器身份定义
* │  类    名: InventoryContainerIds.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

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

        //地上战利品箱
        public const int LOOT = 10;

        //角色身上的容器槽位, 装配处按此顺序注册
        public static readonly int[] ROLE_SLOT_IDS =
        {
            BACKPACK, RIG, POCKETS, SAFEBOX
        };
    }
}
