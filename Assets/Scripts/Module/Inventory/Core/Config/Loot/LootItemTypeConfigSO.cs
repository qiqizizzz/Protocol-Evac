/*
* ┌──────────────────────────────────┐
* │  描    述: 按物品种类维护的掉落池, 承载该种类下具体物品的概率与数量区间
* │  类    名: LootItemTypeConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Module.Inventory.Core.Config.Type;
using UnityEngine;

namespace Module.Inventory.Core.Config.Loot
{
    [CreateAssetMenu(fileName = "LootItemType", menuName = "配置/背包/物品种类掉落池")]
    public sealed class LootItemTypeConfigSO : ScriptableObject
    {
        [Header("物品种类")]
        [Tooltip("该池只允许配置这一种类的物品")]
        [SerializeField] private InventoryItemType ItemTypeValue = InventoryItemType.None;

        [Header("具体物品")]
        [Tooltip("该种类下可出现的具体物品、条目权重与数量区间")]
        [SerializeField] private LootEntry[] EntriesValue = Array.Empty<LootEntry>();

        public InventoryItemType ItemType => ItemTypeValue;
        public LootEntry[] Entries => EntriesValue;

        /// <summary>
        /// 判断该种类池是否至少存在一条可参与抽取的物品条目
        /// </summary>
        /// <returns>存在物品种类匹配且权重大于零的条目时返回 true</returns>
        public bool HasUsableEntries()
        {
            if (ItemTypeValue == InventoryItemType.None) return false;

            for (int i = 0; i < EntriesValue.Length; i++)
            {
                LootEntry entry = EntriesValue[i];
                if (entry == null || entry.Item == null || entry.Weight <= 0) continue;
                if (entry.ItemType == ItemTypeValue) return true;
            }

            return false;
        }
    }
}
