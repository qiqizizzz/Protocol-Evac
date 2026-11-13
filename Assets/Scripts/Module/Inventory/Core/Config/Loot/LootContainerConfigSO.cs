/*
* ┌──────────────────────────────────┐
* │  描    述: 野外物资箱配置, 一份网格加一张按物品种类加权的掉落池表
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

        [Header("物品种类掉落表")]
        [Tooltip("本箱允许出现的物品种类及各类型权重, 具体物品在对应种类池中配置")]
        [SerializeField] private LootTypeEntry[] TypeEntriesValue = Array.Empty<LootTypeEntry>();

        public string DisplayName => DisplayNameValue;
        public InventoryContainerConfigSO Container => ContainerValue;
        public int RollCountMin => RollCountMinValue;
        public int RollCountMax => RollCountMaxValue;
        public LootTypeEntry[] TypeEntries => TypeEntriesValue;

        /// <summary>
        /// 判断箱子是否配置了指定物品种类的可用掉落池
        /// </summary>
        /// <param name="itemType">待查询的物品种类</param>
        /// <returns>种类权重有效且种类池内至少有一条可用物品时返回 true</returns>
        public bool ContainsItemType(InventoryItemType itemType)
        {
            if (itemType == InventoryItemType.None) return false;

            for (int i = 0; i < TypeEntriesValue.Length; i++)
            {
                LootTypeEntry entry = TypeEntriesValue[i];
                if (entry == null || entry.ItemType != itemType || !entry.HasUsableEntries()) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 收集箱子实际参与抽取的物品种类
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

            for (int i = 0; i < TypeEntriesValue.Length; i++)
            {
                LootTypeEntry entry = TypeEntriesValue[i];
                if (entry == null || entry.ItemType == InventoryItemType.None || !entry.HasUsableEntries()) continue;
                if (!collectedTypes.Add(entry.ItemType)) continue;

                results.Add(entry.ItemType);
            }

            return results.Count;
        }
    }
}
