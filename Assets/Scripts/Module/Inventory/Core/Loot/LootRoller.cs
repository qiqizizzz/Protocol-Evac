/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱掉落抽取器, 先按种类权重选池再按池内权重摇出物品
* │  类    名: LootRoller.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Loot;
using Module.Inventory.Core.Config.Type;
using Utils.log;

namespace Module.Inventory.Core.Loot
{
    public static class LootRoller
    {
        private static readonly LootDrop[] EMPTY_DROPS = Array.Empty<LootDrop>();

        /// <summary>
        /// 按种子抽取一次物资箱内容, 同一份配置同一个种子结果恒定, 便于联机校验与测试复现
        /// </summary>
        /// <param name="loot">物资箱配置</param>
        /// <param name="seed">随机种子</param>
        public static IReadOnlyList<LootDrop> Roll(LootContainerConfigSO loot, int seed)
        {
            if (loot == null)
            {
                QLog.Error("物资箱配置为空, 无法抽取掉落");
                return EMPTY_DROPS;
            }

            int totalTypeWeight = SumTypeWeight(loot.TypeEntries);
            if (totalTypeWeight <= 0)
            {
                QLog.Error($"物资箱 {loot.DisplayName} 没有任何可用的物品种类池");
                return EMPTY_DROPS;
            }

            Random random = new Random(seed);
            Dictionary<InventoryItemConfigSO, int> units = new Dictionary<InventoryItemConfigSO, int>();
            List<InventoryItemConfigSO> order = new List<InventoryItemConfigSO>();

            for (int roll = 0, rollCount = DrawRollCount(loot, random); roll < rollCount; roll++)
            {
                LootTypeEntry typeEntry = PickTypeEntry(loot.TypeEntries, totalTypeWeight, random);
                if (typeEntry == null) continue;

                LootEntry entry = PickEntry(typeEntry.Entries, typeEntry.ItemType, random);
                if (entry == null) continue;

                int count = DrawCount(entry, random);
                if (!units.TryGetValue(entry.Item, out int accumulated))
                {
                    accumulated = 0;
                    order.Add(entry.Item);
                }

                units[entry.Item] = accumulated + count;
            }

            return SplitIntoStacks(order, units);
        }

        //箱子层只统计带有效种类池且权重大于零的条目
        private static int SumTypeWeight(LootTypeEntry[] typeEntries)
        {
            if (typeEntries == null) return 0;

            int total = 0;
            for (int i = 0; i < typeEntries.Length; i++)
            {
                LootTypeEntry entry = typeEntries[i];
                if (entry == null || !entry.HasUsableEntries()) continue;
                total += entry.Weight;
            }

            return total;
        }

        //本次开箱抽几次
        private static int DrawRollCount(LootContainerConfigSO loot, Random random)
        {
            int min = loot.RollCountMin;
            int max = loot.RollCountMax < min ? min : loot.RollCountMax;
            return random.Next(min, max + 1);
        }

        //按箱子内种类权重摇中一个掉落池
        private static LootTypeEntry PickTypeEntry(LootTypeEntry[] typeEntries, int totalWeight, Random random)
        {
            int ticket = random.Next(totalWeight);

            for (int i = 0; i < typeEntries.Length; i++)
            {
                LootTypeEntry entry = typeEntries[i];
                if (entry == null || !entry.HasUsableEntries()) continue;

                ticket -= entry.Weight;
                if (ticket < 0) return entry;
            }

            return null;
        }

        //池内权重求和, 只接受与池分类一致的物品
        private static int SumEntryWeight(LootEntry[] entries, InventoryItemType itemType)
        {
            if (entries == null) return 0;

            int total = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                LootEntry entry = entries[i];
                if (entry == null || entry.Item == null || entry.Weight <= 0) continue;
                if (entry.ItemType != itemType) continue;
                total += entry.Weight;
            }

            return total;
        }

        //按池内物品权重摇中一条掉落
        private static LootEntry PickEntry(LootEntry[] entries, InventoryItemType itemType, Random random)
        {
            int totalWeight = SumEntryWeight(entries, itemType);
            if (totalWeight <= 0) return null;

            int ticket = random.Next(totalWeight);

            for (int i = 0; i < entries.Length; i++)
            {
                LootEntry entry = entries[i];
                if (entry == null || entry.Item == null || entry.Weight <= 0) continue;
                if (entry.ItemType != itemType) continue;

                ticket -= entry.Weight;
                if (ticket < 0) return entry;
            }

            return null;
        }

        //单条掉落摇出几件
        private static int DrawCount(LootEntry entry, Random random)
        {
            int min = entry.CountMin;
            int max = entry.CountMax < min ? min : entry.CountMax;
            return random.Next(min, max + 1);
        }

        //按命中顺序拆堆, 可堆叠物品超过上限就续上新一堆, 不可堆叠物品每件一堆
        private static LootDrop[] SplitIntoStacks(List<InventoryItemConfigSO> order,
            Dictionary<InventoryItemConfigSO, int> units)
        {
            List<LootDrop> drops = new List<LootDrop>(order.Count);

            for (int i = 0; i < order.Count; i++)
            {
                InventoryItemConfigSO item = order[i];
                int stackLimit = item.IsStackable ? item.MaxStackCount : 1;

                for (int remaining = units[item]; remaining > 0; remaining -= stackLimit)
                    drops.Add(new LootDrop(item, remaining > stackLimit ? stackLimit : remaining));
            }

            return drops.ToArray();
        }
    }
}
