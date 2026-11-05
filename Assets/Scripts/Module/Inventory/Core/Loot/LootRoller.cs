/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱掉落抽取器, 按权重把掉落表摇成一组待落子的物品
* │  类    名: LootRoller.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Loot;
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

            int totalWeight = SumWeight(loot.Entries);
            if (totalWeight <= 0)
            {
                QLog.Error($"物资箱 {loot.DisplayName} 没有任何可用掉落条目");
                return EMPTY_DROPS;
            }

            Random random = new Random(seed);
            Dictionary<InventoryItemConfigSO, int> units = new Dictionary<InventoryItemConfigSO, int>();
            List<InventoryItemConfigSO> order = new List<InventoryItemConfigSO>();

            for (int roll = 0, rollCount = DrawRollCount(loot, random); roll < rollCount; roll++)
            {
                LootEntry entry = PickEntry(loot.Entries, totalWeight, random);
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

        //全表权重求和, 空槽与零权条目不计入
        private static int SumWeight(LootEntry[] entries)
        {
            if (entries == null) return 0;

            int total = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                LootEntry entry = entries[i];
                if (entry == null || entry.Item == null || entry.Weight <= 0) continue;
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

        //按权重摇中一条掉落, 上限恒等于总和所以循环内必中, 返回 null 只为让编译器闭眼
        private static LootEntry PickEntry(LootEntry[] entries, int totalWeight, Random random)
        {
            int ticket = random.Next(totalWeight);

            for (int i = 0; i < entries.Length; i++)
            {
                LootEntry entry = entries[i];
                if (entry == null || entry.Item == null || entry.Weight <= 0) continue;

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
