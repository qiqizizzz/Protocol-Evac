/*
* ┌──────────────────────────────────┐
* │  描    述: 单次开箱得到的一条掉落结果
* │  类    名: LootDrop.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Inventory.Core.Config;

namespace Module.Inventory.Core.Loot
{
    public readonly struct LootDrop
    {
        public InventoryItemConfigSO Item { get; }

        //本条掉落的件数, 可堆叠物品即堆叠数
        public int Count { get; }

        public LootDrop(InventoryItemConfigSO item, int count)
        {
            Item = item;
            Count = count < 1 ? 1 : count;
        }

        public override string ToString()
        {
            return $"{Item.DisplayName} x{Count}";
        }
    }
}
