/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱生成控制器, 收集生成点并按目录权重创建带唯一实体号的箱子
* │  类    名: LootContainerSpawnController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Inventory.Core.Config.Loot;
using Module.Inventory.Interaction;
using UnityEngine;
using Utils.log;

namespace Module.Inventory.Generation
{
    /// <summary>
    /// 物资箱生成控制器：只负责调度生成点、目录选择结果与物资箱配置注入
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LootContainerSpawnController : MonoBehaviour
    {
        private const string ENTITY_ID_PREFIX = "loot";

        [Header("配置")]
        [Tooltip("物资箱预制体与掉落配置目录")]
        [SerializeField] private LootContainerCatalogConfigSO CatalogValue;

        [Tooltip("所有物资箱生成点的父节点")]
        [SerializeField] private Transform SpawnRootValue;

        [Tooltip("同一世界种子会生成同一批物资箱")]
        [SerializeField] private int WorldSeedValue = 1;

        private readonly List<LootContainerPrefabEntry> m_openableEntries =
            new List<LootContainerPrefabEntry>();

        private readonly HashSet<string> m_spawnIds = new HashSet<string>();

        private bool m_hasGenerated;
        private int m_generatedCount;

        private void Start()
        {
            Generate();
        }

        /// <summary>
        /// 按当前目录与生成点创建物资箱, 返回成功实例数量
        /// </summary>
        public int Generate()
        {
            if (m_hasGenerated) return m_generatedCount;

            if (CatalogValue == null)
            {
                QLog.Error("物资箱生成失败：未配置物资箱目录");
                return 0;
            }

            if (SpawnRootValue == null)
            {
                QLog.Error("物资箱生成失败：未配置生成点根节点");
                return 0;
            }

            CatalogValue.CollectOpenable(m_openableEntries);
            if (m_openableEntries.Count == 0)
            {
                QLog.Error("物资箱生成失败：目录中没有配置完整的可开启条目");
                return 0;
            }

            LootContainerSpawnPoint[] spawnPoints =
                SpawnRootValue.GetComponentsInChildren<LootContainerSpawnPoint>(true);
            if (spawnPoints.Length == 0)
            {
                QLog.Error($"物资箱生成失败：{SpawnRootValue.name} 下没有生成点");
                return 0;
            }

            m_spawnIds.Clear();
            int generatedCount = 0;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                LootContainerSpawnPoint spawnPoint = spawnPoints[i];
                if (!TryRegisterSpawnPoint(spawnPoint)) continue;

                generatedCount += GenerateAt(spawnPoint);
            }

            m_hasGenerated = true;
            m_generatedCount = generatedCount;
            return m_generatedCount;
        }

        // 同一场景只允许一个生成点使用同一编号, 否则实体号会冲突
        private bool TryRegisterSpawnPoint(LootContainerSpawnPoint spawnPoint)
        {
            if (string.IsNullOrWhiteSpace(spawnPoint.SpawnId))
            {
                QLog.Error($"跳过物资箱生成点：编号为空, {spawnPoint.name}");
                return false;
            }

            if (m_spawnIds.Add(spawnPoint.SpawnId)) return true;

            QLog.Error($"跳过物资箱生成点：编号重复 {spawnPoint.SpawnId}, {spawnPoint.name}");
            return false;
        }

        // 单个生成点的数量与实例创建由这里统一调度
        private int GenerateAt(LootContainerSpawnPoint spawnPoint)
        {
            if (!spawnPoint.HasValidCountRange()) return 0;

            int pointSeed = LootContainerSelector.CreateSeed(spawnPoint.SpawnId, WorldSeedValue);
            int spawnCount = LootContainerSelector.SelectCount(
                spawnPoint.MinCount, spawnPoint.MaxCount, pointSeed);
            int generatedCount = 0;
            for (int i = 0; i < spawnCount; i++)
            {
                int instanceSeed = LootContainerSelector.CreateInstanceSeed(pointSeed, i);
                LootContainerPrefabEntry entry = LootContainerSelector.SelectWeighted(m_openableEntries, instanceSeed);
                if (entry == null)
                {
                    QLog.Error($"物资箱生成失败：目录中没有正权重条目, {spawnPoint.SpawnId}");
                    break;
                }

                if (!TryCreateBox(spawnPoint, entry, i)) continue;
                generatedCount++;
            }

            return generatedCount;
        }

        // 每个实例先注入实体号与掉落, 再确保进入激活状态触发 Awake
        private bool TryCreateBox(LootContainerSpawnPoint spawnPoint, LootContainerPrefabEntry entry, int instanceIndex)
        {
            if (!entry.Prefab.TryGetComponent(out LootBoxController _))
            {
                QLog.Error($"物资箱预制体缺少 LootBoxController：{entry.Prefab.name}");
                return false;
            }

            Transform spawnTransform = spawnPoint.transform;
            GameObject boxObject = Instantiate(entry.Prefab, spawnTransform.position, spawnTransform.rotation, transform);
            LootBoxController boxController = boxObject.GetComponent<LootBoxController>();
            string instanceName = BuildInstanceName(spawnPoint.SpawnId, instanceIndex);
            boxObject.name = instanceName;
            boxController.Configure(instanceName, entry.LootConfig);
            if (!boxObject.activeSelf) boxObject.SetActive(true);

            return true;
        }

        // 实体号与层级名称共用稳定编号, 重启后仍可复现
        private static string BuildInstanceName(string spawnId, int instanceIndex)
            => $"{ENTITY_ID_PREFIX}_{spawnId}_{instanceIndex + 1:00}";
    }
}
