/*
* ┌──────────────────────────────────┐
* │  描    述: 野外物资箱配置, 一份网格加一张带权重的掉落表
* │  类    名: LootContainerConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Module.Inventory.Core.Config.Type;
using UnityEngine;
using Utils.log;

namespace Module.Inventory.Core.Config.Loot
{
    [CreateAssetMenu(fileName = "LootConfig", menuName = "配置/背包/野外物资箱")]
    public sealed class LootContainerConfigSO : ScriptableObject
    {
        [Header("标识")]
        [Tooltip("物资箱显示名, 留空则沿用网格定义的容器名")]
        [SerializeField] private string DisplayNameValue = string.Empty;

        [Header("网格")]
        [Tooltip("物资箱自身的网格定义, 决定容量与异形形状")]
        [SerializeField] private InventoryContainerConfigSO ContainerValue;

        [Header("抽取次数")]
        [Tooltip("每次开箱按权重抽取的最少次数")]
        [SerializeField, Min(0)] private int RollCountMinValue = 1;

        [Tooltip("每次开箱按权重抽取的最多次数")]
        [SerializeField, Min(0)] private int RollCountMaxValue = 1;

        [Header("掉落表")]
        [Tooltip("可掉落的物品与各自权重")]
        [SerializeField] private LootEntry[] EntriesValue = Array.Empty<LootEntry>();

        public string DisplayName => DisplayNameValue;
        public InventoryContainerConfigSO Container => ContainerValue;
        public int RollCountMin => RollCountMinValue;
        public int RollCountMax => RollCountMaxValue;
        public LootEntry[] Entries => EntriesValue;

        /// <summary>
        /// 判断掉落表是否包含指定物品种类
        /// </summary>
        /// <param name="itemType">待查询的物品种类</param>
        /// <returns>至少有一条有效掉落条目命中时返回 true</returns>
        public bool ContainsItemType(InventoryItemType itemType)
        {
            if (itemType == InventoryItemType.None) return false;

            for (int i = 0; i < EntriesValue.Length; i++)
            {
                LootEntry entry = EntriesValue[i];
                if (entry != null && entry.ItemType == itemType) return true;
            }

            return false;
        }

        /// <summary>
        /// 收集掉落表中实际出现的物品种类
        /// </summary>
        /// <param name="results">复用的结果列表, 调用时先清空</param>
        /// <returns>收集到的物品种类数量</returns>
        public int CollectItemTypes(List<InventoryItemType> results)
        {
            if (results == null)
            {
                QLog.Error("收集物资箱物品种类失败：结果列表为空");
                return 0;
            }

            results.Clear();
            HashSet<InventoryItemType> collectedTypes = new HashSet<InventoryItemType>();

            for (int i = 0; i < EntriesValue.Length; i++)
            {
                LootEntry entry = EntriesValue[i];
                if (entry == null || entry.ItemType == InventoryItemType.None) continue;
                if (!collectedTypes.Add(entry.ItemType)) continue;

                results.Add(entry.ItemType);
            }

            return results.Count;
        }
    }
}
