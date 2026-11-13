/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱的物品种类条目, 描述某个种类池在箱子里的抽取权重
* │  类    名: LootTypeEntry.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Module.Inventory.Core.Config.Type;
using UnityEngine;

namespace Module.Inventory.Core.Config.Loot
{
    [Serializable]
    public sealed class LootTypeEntry
    {
        [Tooltip("该物品种类对应的独立掉落池")]
        [SerializeField] private LootItemTypeConfigSO TypeConfigValue;

        [Tooltip("该种类在物资箱内的抽取权重, 填 0 表示本箱不出现该种类")]
        [SerializeField, Min(0)] private int WeightValue = 1;

        public LootItemTypeConfigSO TypeConfig => TypeConfigValue;
        public InventoryItemType ItemType => TypeConfigValue != null ? TypeConfigValue.ItemType : InventoryItemType.None;
        public LootEntry[] Entries => TypeConfigValue != null ? TypeConfigValue.Entries : Array.Empty<LootEntry>();
        public int Weight => WeightValue;

        //箱子只会选择至少有一条可用物品的种类池
        public bool HasUsableEntries()
        {
            return WeightValue > 0 && TypeConfigValue != null && TypeConfigValue.HasUsableEntries();
        }
    }
}
