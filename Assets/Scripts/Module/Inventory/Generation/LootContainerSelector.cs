/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱生成选择器, 用稳定种子完成数量与预制体权重抽取
* │  类    名: LootContainerSelector.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Module.Inventory.Core.Config.Loot;
using Utils.log;

namespace Module.Inventory.Generation
{
    /// <summary>
    /// 物资箱生成选择器：只计算结果, 不创建对象也不访问场景
    /// </summary>
    public static class LootContainerSelector
    {
        private const uint FNV_OFFSET_BASIS = 2166136261;
        private const uint FNV_PRIME = 16777619;
        private const int SPAWN_COUNT_SALT = 0x4C4F4F54;

        /// <summary>
        /// 按稳定键与世界种子生成可复现的随机种子
        /// </summary>
        /// <param name="key">稳定键, 一般是生成点编号</param>
        /// <param name="worldSeed">整局世界的生成种子</param>
        public static int CreateSeed(string key, int worldSeed)
        {
            if (string.IsNullOrEmpty(key))
            {
                QLog.Error("创建物资箱生成种子失败：稳定键为空");
                return 0;
            }

            uint hash = FNV_OFFSET_BASIS;
            AppendString(ref hash, key);
            AppendInt(ref hash, worldSeed);
            return ToNonNegativeSeed(hash);
        }

        /// <summary>
        /// 在已有种子上叠加实例序号, 为同一生成点的每只箱子分配独立种子
        /// </summary>
        /// <param name="seed">生成点种子</param>
        /// <param name="instanceIndex">从零开始的实例序号</param>
        public static int CreateInstanceSeed(int seed, int instanceIndex)
        {
            uint hash = FNV_OFFSET_BASIS;
            AppendInt(ref hash, seed);
            AppendInt(ref hash, instanceIndex);
            return ToNonNegativeSeed(hash);
        }

        /// <summary>
        /// 按种子在闭区间内选择稳定数量
        /// </summary>
        /// <param name="minCount">最少生成数量</param>
        /// <param name="maxCount">最多生成数量</param>
        /// <param name="seed">生成点种子</param>
        public static int SelectCount(int minCount, int maxCount, int seed)
        {
            if (minCount < 0)
            {
                QLog.Error($"选择物资箱生成数量失败：最少数量不能为负数：{minCount}");
                return 0;
            }

            if (maxCount < minCount)
            {
                QLog.Error($"选择物资箱生成数量失败：最多数量 {maxCount} 小于最少数量 {minCount}");
                return 0;
            }

            if (minCount == maxCount) return minCount;

            int countSeed = CreateInstanceSeed(seed, SPAWN_COUNT_SALT);
            Random random = new Random(countSeed);
            return random.Next(minCount, maxCount + 1);
        }

        /// <summary>
        /// 按目录权重选择一个可生成预制体, 全部为零权重时返回 null
        /// </summary>
        /// <param name="entries">已完成目录校验的候选条目</param>
        /// <param name="seed">本次抽取种子</param>
        public static LootContainerPrefabEntry SelectWeighted(IReadOnlyList<LootContainerPrefabEntry> entries,
            int seed)
        {
            if (entries == null)
            {
                QLog.Error("选择物资箱预制体失败：候选目录为空");
                return null;
            }

            long totalWeight = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                LootContainerPrefabEntry entry = entries[i];
                if (entry == null || entry.SpawnWeight <= 0) continue;

                totalWeight += entry.SpawnWeight;
            }

            if (totalWeight == 0) return null;

            Random random = new Random(seed);
            long roll = (long) (random.NextDouble() * totalWeight);
            long accumulatedWeight = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                LootContainerPrefabEntry entry = entries[i];
                if (entry == null || entry.SpawnWeight <= 0) continue;

                accumulatedWeight += entry.SpawnWeight;
                if (roll < accumulatedWeight) return entry;
            }

            return null;
        }

        // 把字符串按固定字节写入 FNV-1a 哈希, 保证不同进程得到相同种子
        private static void AppendString(ref uint hash, string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                hash ^= (byte) character;
                hash *= FNV_PRIME;
                hash ^= (byte) (character >> 8);
                hash *= FNV_PRIME;
            }
        }

        // 把整数的四个字节依次写入哈希
        private static void AppendInt(ref uint hash, int value)
        {
            unchecked
            {
                uint bits = (uint) value;
                for (int shift = 0; shift < 32; shift += 8)
                {
                    hash ^= (byte) (bits >> shift);
                    hash *= FNV_PRIME;
                }
            }
        }

        // 选择器种子需要直接喂给随机数, 统一限制为非负整数
        private static int ToNonNegativeSeed(uint hash) => (int) (hash & 0x7FFFFFFF);
    }
}
