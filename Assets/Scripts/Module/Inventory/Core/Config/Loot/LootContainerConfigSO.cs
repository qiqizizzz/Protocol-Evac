/*
* ┌──────────────────────────────────┐
* │  描    述: 野外物资箱配置, 一份网格加一张带权重的掉落表
* │  类    名: LootContainerConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using UnityEngine;

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
    }
}
