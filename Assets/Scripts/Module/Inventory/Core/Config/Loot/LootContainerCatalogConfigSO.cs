/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱预制体目录, 为动态生成提供外观、可开启状态与掉落配置映射
* │  类    名: LootContainerCatalogConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.log;

namespace Module.Inventory.Core.Config.Loot
{
    [CreateAssetMenu(fileName = "LootContainerCatalog", menuName = "配置/背包/物资箱预制体目录")]
    public sealed class LootContainerCatalogConfigSO : ScriptableObject
    {
        [Tooltip("可参与物资箱生成与绑定的预制体列表")]
        [SerializeField] private LootContainerPrefabEntry[] EntriesValue = Array.Empty<LootContainerPrefabEntry>();

        public IReadOnlyList<LootContainerPrefabEntry> Entries => EntriesValue;

        /// <summary>
        /// 按预制体查找目录条目
        /// </summary>
        /// <param name="prefab">待查找的物资箱预制体</param>
        /// <param name="entry">命中的目录条目</param>
        /// <returns>是否找到匹配条目</returns>
        public bool TryResolve(GameObject prefab, out LootContainerPrefabEntry entry)
        {
            for (int i = 0; i < EntriesValue.Length; i++)
            {
                LootContainerPrefabEntry candidate = EntriesValue[i];
                if (candidate == null || !ReferenceEquals(candidate.Prefab, prefab)) continue;

                entry = candidate;
                return true;
            }

            entry = null;
            return false;
        }

        /// <summary>
        /// 收集所有具备开启条件的预制体条目
        /// </summary>
        /// <param name="results">复用的结果列表, 调用时先清空</param>
        /// <returns>收集到的条目数量</returns>
        public int CollectOpenable(List<LootContainerPrefabEntry> results)
        {
            if (results == null)
            {
                QLog.Error("收集可开启物资箱失败：结果列表为空");
                return 0;
            }

            results.Clear();
            for (int i = 0; i < EntriesValue.Length; i++)
            {
                LootContainerPrefabEntry entry = EntriesValue[i];
                if (entry == null)
                {
                    QLog.Error($"物资箱目录第 {i} 项为空");
                    continue;
                }

                if (!entry.CanOpen) continue;

                if (entry.Prefab == null)
                {
                    QLog.Error($"物资箱目录第 {i} 项未配置预制体");
                    continue;
                }

                if (entry.LootConfig == null)
                {
                    QLog.Error($"物资箱目录第 {i} 项未配置掉落");
                    continue;
                }

                if (entry.SpawnWeight < 0)
                {
                    QLog.Error($"物资箱目录第 {i} 项生成权重不能为负数：{entry.SpawnWeight}");
                    continue;
                }

                results.Add(entry);
            }

            return results.Count;
        }
    }
}
