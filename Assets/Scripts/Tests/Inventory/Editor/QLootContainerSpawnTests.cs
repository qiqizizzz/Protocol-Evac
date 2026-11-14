/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱生成测试, 覆盖权重选择、稳定种子、目录校验与配置注入
* │  类    名: QLootContainerSpawnTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Module.Inventory.Core.Config.Loot;
using Module.Inventory.Generation;
using Module.Inventory.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// 物资箱生成测试：选择器保持纯计算, 控制器负责把目录结果装配到场景实例
    /// </summary>
    public class QLootContainerSpawnTests
    {
        private const string EMPTY_ENTITY_ID_PATTERN = "实体号为空";
        private const string EMPTY_SPAWN_ID_PATTERN = "物资箱生成点编号为空";

        private readonly List<GameObject> m_objects = new List<GameObject>();
        private readonly List<ScriptableObject> m_configs = new List<ScriptableObject>();
        private QInventoryTestKit m_kit;

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = m_objects.Count - 1; i >= 0; i--)
            {
                GameObject item = m_objects[i];
                if (item == null) continue;

                UnityEngine.Object.DestroyImmediate(item);
            }

            m_objects.Clear();

            for (int i = m_configs.Count - 1; i >= 0; i--)
            {
                ScriptableObject config = m_configs[i];
                if (config == null) continue;

                UnityEngine.Object.DestroyImmediate(config);
            }

            m_configs.Clear();
            m_kit.Dispose();
        }

        [Test]
        public void SelectWeighted_UsesOnlyPositiveWeightsAndIsDeterministic()
        {
            LootContainerPrefabEntry excluded = CreateEntry(null, 0);
            LootContainerPrefabEntry common = CreateEntry(null, 1);
            LootContainerPrefabEntry rare = CreateEntry(null, 3);
            LootContainerPrefabEntry[] entries = { excluded, common, rare };

            int commonCount = 0;
            int rareCount = 0;
            for (int seed = 0; seed < 512; seed++)
            {
                LootContainerPrefabEntry selected = LootContainerSelector.SelectWeighted(entries, seed);
                Assert.AreNotSame(excluded, selected, "零权重条目不能参与随机生成");
                Assert.AreEqual(selected, LootContainerSelector.SelectWeighted(entries, seed),
                    "同一种子必须稳定选中同一条目");

                if (ReferenceEquals(selected, common)) commonCount++;
                if (ReferenceEquals(selected, rare)) rareCount++;
            }

            Assert.Greater(commonCount, 0, "正权重条目必须都有机会被抽中");
            Assert.Greater(rareCount, 0, "正权重条目必须都有机会被抽中");
        }

        [Test]
        public void SelectWeighted_ReturnsNullWhenAllEntriesHaveZeroWeight()
        {
            LootContainerPrefabEntry first = CreateEntry(null, 0);
            LootContainerPrefabEntry second = CreateEntry(null, 0);

            LootContainerPrefabEntry selected =
                LootContainerSelector.SelectWeighted(new[] { first, second }, 12);

            Assert.IsNull(selected, "全部零权重时不应强行返回一个预制体");
        }

        [Test]
        public void CreateSeedAndSelectCount_AreStableWithinTheConfiguredRange()
        {
            int pointSeed = LootContainerSelector.CreateSeed("checkpoint_a_01", 20261113);
            int repeatedSeed = LootContainerSelector.CreateSeed("checkpoint_a_01", 20261113);
            int otherSeed = LootContainerSelector.CreateSeed("checkpoint_b_01", 20261113);

            int count = LootContainerSelector.SelectCount(1, 3, pointSeed);
            int repeatedCount = LootContainerSelector.SelectCount(1, 3, repeatedSeed);

            Assert.AreEqual(pointSeed, repeatedSeed, "同一生成点与世界种子的哈希必须稳定");
            Assert.AreNotEqual(pointSeed, otherSeed, "不同生成点应拥有不同随机序列");
            Assert.AreEqual(count, repeatedCount, "同一生成点的数量选择必须可复现");
            Assert.That(count, Is.InRange(1, 3), "生成数量必须落在配置闭区间内");
        }

        [Test]
        public void SelectScatterOffset_IsStableDistinctAndOnTheConfiguredRing()
        {
            const float radius = 1.5f;
            int pointSeed = LootContainerSelector.CreateSeed("scatter_point", 20261114);

            Vector3 first = LootContainerSelector.SelectScatterOffset(4, 0, radius, pointSeed);
            Vector3 repeated = LootContainerSelector.SelectScatterOffset(4, 0, radius, pointSeed);
            Vector3 second = LootContainerSelector.SelectScatterOffset(4, 1, radius, pointSeed);

            Assert.AreEqual(first, repeated, "同一种子与实例序号必须得到稳定散布位置");
            Assert.AreNotEqual(first, second, "同一生成点的不同实例不能重合");
            Assert.AreEqual(radius, new Vector2(first.x, first.z).magnitude, 0.001f,
                "多箱实例必须落在配置的环状散布半径上");
            Assert.AreEqual(0f, first.y, 0.001f, "散布偏移不能改变生成点高度");
        }

        [Test]
        public void Catalog_CollectOpenable_ReportsInvalidEntriesAndSkipsDisabledOnes()
        {
            GameObject prefab = TrackObject(new GameObject("LootSpawnCatalogPrefab"));
            LootContainerConfigSO loot = m_kit.CreateLoot(
                "测试物资箱", m_kit.CreateContainer(2, 2), 1, 2);

            LootContainerPrefabEntry disabled = CreateEntry(prefab, 1, false);
            LootContainerPrefabEntry missingPrefab = CreateEntry(null, 1);
            LootContainerPrefabEntry missingLoot = CreateEntry(prefab, 1);
            LootContainerPrefabEntry negativeWeight = CreateEntry(prefab, -1);
            negativeWeight = SetEntryLoot(negativeWeight, loot);
            LootContainerPrefabEntry valid = SetEntryLoot(CreateEntry(prefab, 1), loot);

            LootContainerCatalogConfigSO catalog = TrackConfig(
                ScriptableObject.CreateInstance<LootContainerCatalogConfigSO>());
            SetField(catalog, "EntriesValue", new[]
            {
                null, disabled, missingPrefab, missingLoot, negativeWeight, valid
            });

            LogAssert.Expect(LogType.Error, new Regex("第 0 项为空"));
            LogAssert.Expect(LogType.Error, new Regex("第 2 项未配置预制体"));
            LogAssert.Expect(LogType.Error, new Regex("第 3 项未配置掉落"));
            LogAssert.Expect(LogType.Error, new Regex("第 4 项生成权重不能为负数"));

            List<LootContainerPrefabEntry> results = new List<LootContainerPrefabEntry>();
            int count = catalog.CollectOpenable(results);

            Assert.AreEqual(1, count, "只有配置完整且未被禁用、权重非负的条目能进入结果");
            Assert.AreSame(valid, results[0]);
        }

        [Test]
        public void Generate_InjectsStableEntityIdAndLootConfigIntoTheSpawnedBox()
        {
            GameObject prefab = CreateInactiveLootPrefab("LootSpawnPrefab");
            LootContainerConfigSO loot = m_kit.CreateLoot(
                "生成测试物资箱", m_kit.CreateContainer(3, 2), 1, 1);
            LootContainerCatalogConfigSO catalog = CreateCatalog(prefab, loot);

            GameObject spawnRootObject = TrackObject(new GameObject("LootSpawnRoot"));
            LootContainerSpawnPoint spawnPoint = CreateSpawnPoint(spawnRootObject.transform,
                "test_point", new Vector3(3f, 1f, 5f), 1, 1);

            GameObject controllerObject = TrackObject(new GameObject("LootSpawnController"));
            LootContainerSpawnController controller =
                controllerObject.AddComponent<LootContainerSpawnController>();
            SetField(controller, "CatalogValue", catalog);
            SetField(controller, "SpawnRootValue", spawnRootObject.transform);
            SetField(controller, "WorldSeedValue", 20261113);

            int generatedCount = controller.Generate();

            Assert.AreEqual(1, generatedCount, "一个单实例生成点应创建一只物资箱");
            LootBoxController spawnedBox = controllerObject.GetComponentInChildren<LootBoxController>(true);
            Assert.IsNotNull(spawnedBox, "生成结果必须保留 LootBoxController");
            Assert.AreEqual("loot_test_point_01", spawnedBox.EntityId,
                "实体号必须由前缀、生成点编号和实例序号稳定组成");
            Assert.AreSame(loot, spawnedBox.LootConfig, "目录中的掉落配置必须注入生成实例");
            Assert.AreEqual(new Vector3(3f, 1f, 5f), spawnedBox.transform.position,
                "实例必须落在生成点的世界坐标");
            Assert.AreEqual("loot_test_point_01", spawnedBox.name, "层级名称应与实体号一致便于定位");
            Assert.AreSame(spawnRootObject.transform, spawnPoint.transform.parent,
                "生成点只提供数据, 生成过程不应替换或销毁它");

            int repeatedCount = controller.Generate();
            LootBoxController[] repeatedBoxes =
                controllerObject.GetComponentsInChildren<LootBoxController>(true);
            Assert.AreEqual(generatedCount, repeatedCount, "重复调用必须返回首次生成数量");
            Assert.AreEqual(1, repeatedBoxes.Length, "同一生成控制器不能重复创建物资箱");
        }

        // 生成点专属池优先于控制器全局目录
        [Test]
        public void Generate_UsesSpawnPointPoolInsteadOfDefaultCatalog()
        {
            GameObject defaultPrefab = CreateInactiveLootPrefab("LootSpawnDefaultPrefab");
            LootContainerConfigSO defaultLoot = m_kit.CreateLoot(
                "默认目录掉落", m_kit.CreateContainer(3, 2), 1, 1);
            LootContainerCatalogConfigSO defaultCatalog = CreateCatalog(defaultPrefab, defaultLoot);

            GameObject poolPrefab = CreateInactiveLootPrefab("LootSpawnPoolPrefab");
            LootContainerConfigSO poolLoot = m_kit.CreateLoot(
                "生成点池掉落", m_kit.CreateContainer(3, 2), 1, 1);
            LootContainerCatalogConfigSO pointPool = CreateCatalog(poolPrefab, poolLoot);

            GameObject spawnRootObject = TrackObject(new GameObject("LootSpawnPoolRoot"));
            LootContainerSpawnPoint spawnPoint = CreateSpawnPoint(spawnRootObject.transform,
                "pool_point", Vector3.zero, 1, 1);
            SetField(spawnPoint, "SpawnPoolValue", pointPool);

            GameObject controllerObject = TrackObject(new GameObject("LootSpawnPoolController"));
            LootContainerSpawnController controller =
                controllerObject.AddComponent<LootContainerSpawnController>();
            SetField(controller, "CatalogValue", defaultCatalog);
            SetField(controller, "SpawnRootValue", spawnRootObject.transform);

            int generatedCount = controller.Generate();
            LootBoxController spawnedBox =
                controllerObject.GetComponentInChildren<LootBoxController>(true);

            Assert.AreEqual(1, generatedCount, "配置专属池的生成点仍应正常生成");
            Assert.IsNotNull(spawnedBox, "生成点必须创建物资箱");
            Assert.AreSame(poolLoot, spawnedBox.LootConfig,
                "生成点配置专属池后不能继续使用控制器全局目录");
        }

        // 显式空池属于配置错误, 不能静默回退到全局目录
        [Test]
        public void Generate_WithEmptySpawnPool_ReportsErrorAndSkipsPoint()
        {
            GameObject defaultPrefab = CreateInactiveLootPrefab("LootSpawnEmptyPoolDefaultPrefab");
            LootContainerConfigSO defaultLoot = m_kit.CreateLoot(
                "空池回退默认掉落", m_kit.CreateContainer(3, 2), 1, 1);
            LootContainerCatalogConfigSO defaultCatalog = CreateCatalog(defaultPrefab, defaultLoot);
            LootContainerCatalogConfigSO emptyPool = TrackConfig(
                ScriptableObject.CreateInstance<LootContainerCatalogConfigSO>());

            GameObject spawnRootObject = TrackObject(new GameObject("LootSpawnEmptyPoolRoot"));
            LootContainerSpawnPoint spawnPoint = CreateSpawnPoint(spawnRootObject.transform,
                "empty_pool_point", Vector3.zero, 1, 1);
            SetField(spawnPoint, "SpawnPoolValue", emptyPool);

            GameObject controllerObject = TrackObject(new GameObject("LootSpawnEmptyPoolController"));
            LootContainerSpawnController controller =
                controllerObject.AddComponent<LootContainerSpawnController>();
            SetField(controller, "CatalogValue", defaultCatalog);
            SetField(controller, "SpawnRootValue", spawnRootObject.transform);

            LogAssert.Expect(LogType.Error, new Regex("生成点专属池为空.*empty_pool_point"));

            int generatedCount = controller.Generate();
            LootBoxController[] boxes =
                controllerObject.GetComponentsInChildren<LootBoxController>(true);

            Assert.AreEqual(0, generatedCount, "空池生成点必须跳过, 不能回退全局目录");
            Assert.AreEqual(0, boxes.Length, "空池生成点不能创建物资箱");
        }

        [Test]
        public void Generate_MultipleBoxes_UsesNonOverlappingScatterSlots()
        {
            GameObject prefab = CreateInactiveLootPrefab("LootSpawnMultiPrefab");
            LootContainerConfigSO loot = m_kit.CreateLoot(
                "多箱生成测试", m_kit.CreateContainer(3, 2), 1, 1);
            LootContainerCatalogConfigSO catalog = CreateCatalog(prefab, loot);

            GameObject spawnRootObject = TrackObject(new GameObject("LootSpawnMultiRoot"));
            Vector3 pointPosition = new Vector3(-2f, 0.5f, 4f);
            LootContainerSpawnPoint spawnPoint = CreateSpawnPoint(spawnRootObject.transform,
                "multi_point", pointPosition, 4, 4, 1.25f);

            GameObject controllerObject = TrackObject(new GameObject("LootSpawnMultiController"));
            LootContainerSpawnController controller =
                controllerObject.AddComponent<LootContainerSpawnController>();
            SetField(controller, "CatalogValue", catalog);
            SetField(controller, "SpawnRootValue", spawnRootObject.transform);
            SetField(controller, "WorldSeedValue", 20261114);

            int generatedCount = controller.Generate();
            LootBoxController[] boxes =
                controllerObject.GetComponentsInChildren<LootBoxController>(true);

            Assert.AreEqual(4, generatedCount, "固定四箱生成点必须创建四个实例");
            Assert.AreEqual(4, boxes.Length, "多箱生成结果数量必须与选择数量一致");
            for (int i = 0; i < boxes.Length; i++)
            {
                Vector3 planarOffset = boxes[i].transform.position - pointPosition;
                planarOffset.y = 0f;
                Assert.AreEqual(spawnPoint.ScatterRadius, planarOffset.magnitude, 0.001f,
                    "多箱实例必须落在生成点散布圈上");

                for (int j = i + 1; j < boxes.Length; j++)
                {
                    Assert.Greater(Vector3.Distance(boxes[i].transform.position, boxes[j].transform.position),
                        0.01f, "同一生成点的物资箱不能占用相同位置");
                }
            }
        }

        [Test]
        public void Generate_MultipleBoxesWithoutScatterRadius_ClampsToSingleInstance()
        {
            GameObject prefab = CreateInactiveLootPrefab("LootSpawnUnscatteredPrefab");
            LootContainerConfigSO loot = m_kit.CreateLoot(
                "无散布测试", m_kit.CreateContainer(3, 2), 1, 1);
            LootContainerCatalogConfigSO catalog = CreateCatalog(prefab, loot);

            GameObject spawnRootObject = TrackObject(new GameObject("LootSpawnUnscatteredRoot"));
            CreateSpawnPoint(spawnRootObject.transform,
                "unscattered_point", Vector3.zero, 2, 2);

            GameObject controllerObject = TrackObject(new GameObject("LootSpawnUnscatteredController"));
            LootContainerSpawnController controller =
                controllerObject.AddComponent<LootContainerSpawnController>();
            SetField(controller, "CatalogValue", catalog);
            SetField(controller, "SpawnRootValue", spawnRootObject.transform);

            LogAssert.Expect(LogType.Error, new Regex("未配置散布半径"));

            int generatedCount = controller.Generate();
            LootBoxController[] boxes =
                controllerObject.GetComponentsInChildren<LootBoxController>(true);

            Assert.AreEqual(1, generatedCount, "未配置散布半径时必须退化为单箱生成");
            Assert.AreEqual(1, boxes.Length, "不能把多箱实例生成在同一个位置");
        }

        private LootContainerCatalogConfigSO CreateCatalog(GameObject prefab, LootContainerConfigSO loot)
        {
            LootContainerCatalogConfigSO catalog = TrackConfig(
                ScriptableObject.CreateInstance<LootContainerCatalogConfigSO>());
            LootContainerPrefabEntry entry = SetEntryLoot(CreateEntry(prefab, 1), loot);
            SetField(catalog, "EntriesValue", new[] { entry });
            return catalog;
        }

        private GameObject CreateInactiveLootPrefab(string objectName)
        {
            LogAssert.Expect(LogType.Error, new Regex(EMPTY_ENTITY_ID_PATTERN));
            GameObject prefab = TrackObject(new GameObject(objectName));
            prefab.SetActive(false);
            prefab.AddComponent<LootBoxController>();
            return prefab;
        }

        private LootContainerSpawnPoint CreateSpawnPoint(Transform parent, string spawnId, Vector3 position,
            int minCount, int maxCount, float scatterRadius = 0f)
        {
            LogAssert.Expect(LogType.Error, new Regex(EMPTY_SPAWN_ID_PATTERN));
            GameObject pointObject = TrackObject(new GameObject("LootSpawnPoint"));
            pointObject.transform.SetParent(parent, false);
            pointObject.transform.position = position;
            pointObject.transform.rotation = Quaternion.Euler(0f, 30f, 0f);

            LootContainerSpawnPoint point = pointObject.AddComponent<LootContainerSpawnPoint>();
            SetField(point, "SpawnIdValue", spawnId);
            SetField(point, "MinCountValue", minCount);
            SetField(point, "MaxCountValue", maxCount);
            SetField(point, "ScatterRadiusValue", scatterRadius);
            return point;
        }

        private LootContainerPrefabEntry CreateEntry(GameObject prefab, int weight, bool canOpen = true)
        {
            LootContainerPrefabEntry entry = new LootContainerPrefabEntry();
            SetField(entry, "PrefabValue", prefab);
            SetField(entry, "SpawnWeightValue", weight);
            SetField(entry, "CanOpenValue", canOpen);
            return entry;
        }

        private static LootContainerPrefabEntry SetEntryLoot(LootContainerPrefabEntry entry,
            LootContainerConfigSO loot)
        {
            SetField(entry, "LootConfigValue", loot);
            return entry;
        }

        private GameObject TrackObject(GameObject item)
        {
            m_objects.Add(item);
            return item;
        }

        private T TrackConfig<T>(T config) where T : ScriptableObject
        {
            m_configs.Add(config);
            return config;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{target.GetType().Name} 缺少字段 {fieldName}");
            field.SetValue(target, value);
        }
    }
}
