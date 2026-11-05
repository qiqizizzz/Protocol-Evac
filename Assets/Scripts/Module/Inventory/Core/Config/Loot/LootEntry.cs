/*
* ┌──────────────────────────────────┐
* │  描    述: 战利品掉落条目, 描述一件物品在物资箱里的抽取权重与数量区间
* │  类    名: LootEntry.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using UnityEngine;

namespace Module.Inventory.Core.Config.Loot
{
    [Serializable]
    public sealed class LootEntry
    {
        [Tooltip("掉落的物品定义")]
        [SerializeField] private InventoryItemConfigSO ItemValue;

        [Tooltip("抽取权重, 与本箱全部条目权重之和相比决定命中率, 填 0 表示不参与抽取")]
        [SerializeField, Min(0)] private int WeightValue = 1;

        [Tooltip("单次命中的最少件数")]
        [SerializeField, Min(1)] private int CountMinValue = 1;

        [Tooltip("单次命中的最多件数, 可堆叠物品超出堆叠上限时自动拆成多堆")]
        [SerializeField, Min(1)] private int CountMaxValue = 1;

        public InventoryItemConfigSO Item => ItemValue;
        public int Weight => WeightValue;
        public int CountMin => CountMinValue;
        public int CountMax => CountMaxValue;
    }
}
