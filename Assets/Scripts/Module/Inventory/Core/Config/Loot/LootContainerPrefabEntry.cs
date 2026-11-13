/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱预制体目录条目, 声明预制体是否可开启及对应掉落配置
* │  类    名: LootContainerPrefabEntry.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using UnityEngine;

namespace Module.Inventory.Core.Config.Loot
{
    [Serializable]
    public sealed class LootContainerPrefabEntry
    {
        [Tooltip("物资箱外观预制体")]
        [SerializeField] private GameObject PrefabValue;

        [Tooltip("该预制体是否允许作为可搜刮物资箱生成")]
        [SerializeField] private bool CanOpenValue = true;

        [Tooltip("该物资箱使用的掉落配置")]
        [SerializeField] private LootContainerConfigSO LootConfigValue;

        [Tooltip("参与随机生成的权重, 0 表示不参与随机生成但仍可手工绑定")]
        [SerializeField, Min(0)] private int SpawnWeightValue = 1;

        public GameObject Prefab => PrefabValue;
        public bool CanOpen => CanOpenValue;
        public LootContainerConfigSO LootConfig => LootConfigValue;
        public int SpawnWeight => SpawnWeightValue;
    }
}
