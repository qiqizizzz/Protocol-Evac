/*
* ┌──────────────────────────────────┐
* │  描    述: 野外物资箱测试, 覆盖权重抽取的确定性、拆堆规则与模型开箱链路
* │  类    名: QInventoryLootTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Loot;
using Module.Inventory.Core.Defines;
using Module.Inventory.Core.Loot;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// 野外物资箱测试：抽取器按权重摇出掉落清单, 模型负责把清单落成运行时容器并管住同一份配置的重开语义
    /// </summary>
    public class QInventoryLootTests
    {
        private const int SEED = 20261105;

        //两只互不相干的物资箱实体, 用来验证同配置不串货
        private const string ENTITY_A = "Props/Plastic_Box_03";
        private const string ENTITY_B = "Props/Plastic_Box_03 (1)";

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();
        }

        [TearDown]
        public void TearDown()
        {
            m_model.OnUnregister();
            m_kit.Dispose();
        }

        #region 抽取器
        [Test]
        public void Roll_SameSeed_RepeatsTheIdenticalDropList()
        {
            LootContainerConfigSO loot = BuildSupplyBox(3, 5);

            Assert.AreEqual(SignatureOf(LootRoller.Roll(loot, SEED)), SignatureOf(LootRoller.Roll(loot, SEED)),
                "同一种子必须摇出完全一致的掉落, 否则联机两端无法对齐");
        }

        [Test]
        public void Roll_DifferentSeeds_ChangeTheDropList()
        {
            LootContainerConfigSO loot = BuildSupplyBox(3, 5);
            string baseline = SignatureOf(LootRoller.Roll(loot, SEED));

            bool changed = false;
            for (int offset = 1; offset <= 8 && !changed; offset++)
                changed = SignatureOf(LootRoller.Roll(loot, SEED + offset)) != baseline;

            Assert.IsTrue(changed, "换了 8 个种子掉落清单一次都没变, 抽取疑似写死");
        }

        [Test]
        public void Roll_HeavyWeightEntry_DominatesTheBox()
        {
            InventoryItemConfigSO staple = m_kit.CreateItem(1, 1);
            InventoryItemConfigSO luxury = m_kit.CreateItem(1, 1);
            LootContainerConfigSO loot = m_kit.CreateLoot("权重箱", m_kit.CreateContainer(5, 5, "测试网格"),
                40, 40, m_kit.CreateLootEntry(staple, 900), m_kit.CreateLootEntry(luxury, 1));

            int stapleCount = 0;
            int luxuryCount = 0;
            IReadOnlyList<LootDrop> drops = LootRoller.Roll(loot, SEED);
            for (int i = 0; i < drops.Count; i++)
            {
                if (drops[i].Item == staple) stapleCount += drops[i].Count;
                else luxuryCount += drops[i].Count;
            }

            Assert.Greater(stapleCount, luxuryCount * 100, "千倍权重差应该让常备货彻底压过稀有货");
        }

        [Test]
        public void Roll_ZeroWeightEntry_IsNeverDrawn()
        {
            InventoryItemConfigSO active = m_kit.CreateItem(1, 1);
            InventoryItemConfigSO disabled = m_kit.CreateItem(1, 1);
            LootContainerConfigSO loot = m_kit.CreateLoot("停用箱", m_kit.CreateContainer(5, 5, "测试网格"),
                6, 6, m_kit.CreateLootEntry(active, 1), m_kit.CreateLootEntry(disabled, 0));

            IReadOnlyList<LootDrop> drops = LootRoller.Roll(loot, SEED);

            Assert.IsNotEmpty(drops);
            for (int i = 0; i < drops.Count; i++)
                Assert.AreNotEqual(disabled.ItemId, drops[i].Item.ItemId, "零权条目不该被摇中");
        }

        [Test]
        public void Roll_StackOverflow_SplitsIntoSeveralStacks()
        {
            InventoryItemConfigSO ammo = m_kit.CreateItem(1, 1, true, 5);
            LootContainerConfigSO loot = m_kit.CreateLoot("弹药箱", m_kit.CreateContainer(5, 5, "测试网格"),
                1, 1, m_kit.CreateLootEntry(ammo, 1, 12, 12));

            IReadOnlyList<LootDrop> drops = LootRoller.Roll(loot, SEED);

            int total = 0;
            for (int i = 0; i < drops.Count; i++)
            {
                Assert.LessOrEqual(drops[i].Count, ammo.MaxStackCount, "单堆不能超过配置上限");
                total += drops[i].Count;
            }

            Assert.AreEqual(3, drops.Count, "12 发按上限 5 应该拆成 3 堆");
            Assert.AreEqual(12, total, "拆堆不能丢件数");
        }

        [Test]
        public void Roll_NonStackableCountRange_DropsThatManySinglePieces()
        {
            InventoryItemConfigSO rifle = m_kit.CreateItem(2, 1);
            LootContainerConfigSO loot = m_kit.CreateLoot("军火箱", m_kit.CreateContainer(6, 4, "测试网格"),
                2, 2, m_kit.CreateLootEntry(rifle, 1, 3, 3));

            IReadOnlyList<LootDrop> drops = LootRoller.Roll(loot, SEED);

            Assert.AreEqual(6, drops.Count, "两次命中各 3 件不可堆叠物品应该落成 6 条");
            for (int i = 0; i < drops.Count; i++) Assert.AreEqual(1, drops[i].Count);
        }

        [Test]
        public void Roll_CountBounds_StaysInsideTheConfiguredRange()
        {
            InventoryItemConfigSO canned = m_kit.CreateItem(1, 1, true, 99);
            LootContainerConfigSO loot = m_kit.CreateLoot("罐头箱", m_kit.CreateContainer(4, 3, "测试网格"),
                1, 1, m_kit.CreateLootEntry(canned, 1, 2, 4));

            for (int seed = SEED; seed < SEED + 20; seed++)
            {
                int total = 0;
                IReadOnlyList<LootDrop> drops = LootRoller.Roll(loot, seed);
                for (int i = 0; i < drops.Count; i++) total += drops[i].Count;

                Assert.GreaterOrEqual(total, 2, "单次命中不能低于最少件数");
                Assert.LessOrEqual(total, 4, "单次命中不能超过最多件数");
            }
        }

        [Test]
        public void Roll_NullLoot_ReturnsNothing()
        {
            LogAssert.Expect(LogType.Error, new Regex("物资箱配置为空"));

            Assert.IsEmpty(LootRoller.Roll(null, SEED));
        }

        [Test]
        public void Roll_EmptyEntryTable_ReturnsNothing()
        {
            LootContainerConfigSO loot = m_kit.CreateLoot("空箱", m_kit.CreateContainer(4, 3, "测试网格"), 3, 5);
            LogAssert.Expect(LogType.Error, new Regex("没有任何可用掉落条目"));

            Assert.IsEmpty(LootRoller.Roll(loot, SEED));
        }
        #endregion

        #region 运行时容器分配
        [Test]
        public void CreateRuntimeContainer_AllocatesIdsAboveTheLootRange()
        {
            int containerId = m_model.CreateRuntimeContainer(new InventoryContainerSpec(3, 2, float.MaxValue, null, "临时容器"));

            Assert.GreaterOrEqual(containerId, InventoryContainerIds.LOOT, "运行时容器号要从 LOOT 段起分配");
            Assert.IsTrue(m_model.IsContainerRegistered(containerId));
        }

        [Test]
        public void CreateRuntimeContainer_ConsecutiveCalls_NeverShareAnId()
        {
            InventoryContainerSpec spec = new InventoryContainerSpec(2, 2, float.MaxValue);

            int first = m_model.CreateRuntimeContainer(spec);
            int second = m_model.CreateRuntimeContainer(spec);

            Assert.AreNotEqual(first, second, "分配器把同一个号发给两只容器, 后面必然串货");
            Assert.IsTrue(m_model.IsContainerRegistered(first));
            Assert.IsTrue(m_model.IsContainerRegistered(second));
        }

        [Test]
        public void CreateRuntimeContainer_AfterUnregister_ReusesTheFreedId()
        {
            InventoryContainerSpec spec = new InventoryContainerSpec(2, 2, float.MaxValue);
            int first = m_model.CreateRuntimeContainer(spec);
            int keep = m_model.CreateRuntimeContainer(spec);

            m_model.UnregisterContainer(first);
            int reused = m_model.CreateRuntimeContainer(spec);

            Assert.AreEqual(first, reused, "空出来的容器号要被下一只容器接走");
            Assert.AreNotEqual(keep, reused, "仍在注册的容器号不许被抢");
        }

        [Test]
        public void CreateRuntimeContainer_ZeroSize_ReturnsInvalidId()
        {
            LogAssert.Expect(LogType.Error, new Regex("容器规格无效"));

            Assert.Less(m_model.CreateRuntimeContainer(new InventoryContainerSpec(0, 3, float.MaxValue)), 0);
        }
        #endregion

        #region 模型开箱
        [Test]
        public void OpenLootContainer_TwoEntitiesWithSameConfig_DoNotShareContainer()
        {
            LootContainerConfigSO loot = BuildSupplyBox(3, 5);

            int boxA = m_model.OpenLootContainer(ENTITY_A, loot, SEED);
            int boxB = m_model.OpenLootContainer(ENTITY_B, loot, SEED);

            Assert.AreNotEqual(boxA, boxB, "两只同配置箱子共用一个容器, 玩家翻一只就等于翻完全部");

            m_model.TryAddItem(boxA, m_kit.CreateItem(1, 1).ItemId, 1, Vector2Int.zero);
            Assert.AreNotEqual(m_model.GetContainerVO(boxA).Items.Count, m_model.GetContainerVO(boxB).Items.Count,
                "往一只箱子里塞东西, 另一只不许跟着变");
        }

        [Test]
        public void OpenLootContainer_SameEntityTwice_KeepsTheOpenedContainer()
        {
            LootContainerConfigSO loot = BuildSupplyBox(3, 5);
            int opened = m_model.OpenLootContainer(ENTITY_A, loot, SEED);

            Assert.AreEqual(opened, m_model.OpenLootContainer(ENTITY_A, loot, SEED), "同一只箱子重开必须沿用旧容器");
        }

        [Test]
        public void OpenLootContainer_EmptyEntityId_ReturnsInvalidId()
        {
            LootContainerConfigSO loot = BuildSupplyBox(1, 2);
            LogAssert.Expect(LogType.Error, new Regex("物资箱实体号为空"));

            Assert.Less(m_model.OpenLootContainer(string.Empty, loot, SEED), 0);
            Assert.Less(m_model.OpenLootContainer(null, loot, SEED), 0);
        }

        [Test]
        public void OpenLootContainer_ChangingLootWhileOpen_IsRejected()
        {
            LootContainerConfigSO supply = BuildSupplyBox(3, 5);
            LootContainerConfigSO weapon = BuildWeaponBox(2, 3);
            int opened = m_model.OpenLootContainer(ENTITY_A, supply, SEED);

            LogAssert.Expect(LogType.Error, new Regex("未关闭就更换掉落配置"));
            Assert.Less(m_model.OpenLootContainer(ENTITY_A, weapon, SEED), 0,
                "同一只箱子开着时换表会让残货对不上配置, 必须拒开而不是静默沿用");
            Assert.AreEqual(opened, m_model.GetContainerVO(opened).ContainerId, "被拒绝的重开不许动到已开的箱子");
        }

        [Test]
        public void OpenLootContainer_RegistersTheGridDescribedByLootConfig()
        {
            LootContainerConfigSO loot = BuildSupplyBox(3, 5);

            int containerId = m_model.OpenLootContainer(ENTITY_A, loot, SEED);

            Assert.GreaterOrEqual(containerId, InventoryContainerIds.LOOT, "物资箱容器号要从 LOOT 段起分配");

            InventoryContainerVO snapshot = m_model.GetContainerVO(containerId);
            Assert.AreEqual(loot.Container.Width, snapshot.Width);
            Assert.AreEqual(loot.Container.Height, snapshot.Height);
            Assert.AreEqual("补给箱", snapshot.DisplayName, "表头名字优先取物资箱自身配置");
            Assert.IsNotEmpty(snapshot.Items);
        }

        [Test]
        public void OpenLootContainer_KeepsGridNameWhenLootConfigHasNoName()
        {
            LootContainerConfigSO loot = m_kit.CreateLoot(string.Empty, m_kit.CreateContainer(4, 3, "野战补给箱"),
                1, 2, m_kit.CreateLootEntry(m_kit.CreateItem(1, 1), 1));

            int containerId = m_model.OpenLootContainer(ENTITY_A, loot, SEED);

            Assert.AreEqual("野战补给箱", m_model.GetContainerVO(containerId).DisplayName);
        }

        [Test]
        public void OpenLootContainer_NeverDropsItemsOutsideTheEntryTable()
        {
            LootContainerConfigSO loot = BuildSupplyBox(3, 6);
            HashSet<int> allowed = CollectEntryItemIds(loot);

            int containerId = m_model.OpenLootContainer(ENTITY_A, loot, SEED);

            IReadOnlyList<InventoryItemVO> items = m_model.GetContainerVO(containerId).Items;
            for (int i = 0; i < items.Count; i++) Assert.IsTrue(allowed.Contains(items[i].ItemId));
        }

        [Test]
        public void OpenLootContainer_ReopensTheSameContainerWhileItStaysRegistered()
        {
            LootContainerConfigSO loot = BuildSupplyBox(3, 5);
            int opened = m_model.OpenLootContainer(ENTITY_A, loot, SEED);
            int residue = m_model.GetContainerVO(opened).Items.Count;

            int reopened = m_model.OpenLootContainer(ENTITY_A, loot, SEED + 9999);

            Assert.AreEqual(opened, reopened, "没关闭的物资箱重开必须沿用旧容器, 让玩家继续翻残货");
            Assert.AreEqual(residue, m_model.GetContainerVO(reopened).Items.Count, "重开不许刷新箱内已有的东西");
        }

        [Test]
        public void OpenLootContainer_AfterUnregisterReRollsInsteadOfStealingTheReusedId()
        {
            LootContainerConfigSO closed = BuildSupplyBox(3, 5);
            LootContainerConfigSO neighbour = BuildWeaponBox(2, 3);
            int firstId = m_model.OpenLootContainer(ENTITY_A, closed, SEED);

            m_model.UnregisterContainer(firstId);
            int neighbourId = m_model.OpenLootContainer(ENTITY_B, neighbour, SEED);
            Assert.AreEqual(firstId, neighbourId, "空出来的容器号应该被下一只箱子复用");

            int reRolledId = m_model.OpenLootContainer(ENTITY_A, closed, SEED);
            Assert.AreNotEqual(neighbourId, reRolledId, "关箱时没清掉物资箱映射, 会让重开的箱子认领别人的容器");
            AssertContentsMatchTable(neighbourId, neighbour);
            AssertContentsMatchTable(reRolledId, closed);
        }

        [Test]
        public void OpenLootContainer_SkipsItemsThatCannotFit()
        {
            InventoryItemConfigSO oversized = m_kit.CreateItem(4, 4);
            InventoryItemConfigSO fitting = m_kit.CreateItem(1, 1);
            LootContainerConfigSO loot = m_kit.CreateLoot("小箱子", m_kit.CreateContainer(2, 2, "测试网格"),
                4, 4, m_kit.CreateLootEntry(oversized, 1), m_kit.CreateLootEntry(fitting, 1));

            int containerId = m_model.OpenLootContainer(ENTITY_A, loot, SEED);

            IReadOnlyList<InventoryItemVO> items = m_model.GetContainerVO(containerId).Items;
            Assert.IsNotEmpty(items, "塞不下的东西被跳过, 能塞下的还要照常进箱");
            for (int i = 0; i < items.Count; i++) Assert.AreEqual(fitting.ItemId, items[i].ItemId);
        }

        [Test]
        public void OpenLootContainer_LeavesWeightAccountingToTheCharacter()
        {
            InventoryItemConfigSO heavy = m_kit.CreateItem(1, 1, true, 1, 9999f);
            LootContainerConfigSO loot = m_kit.CreateLoot("重货箱", m_kit.CreateContainer(3, 2, "测试网格"),
                3, 3, m_kit.CreateLootEntry(heavy, 1));

            int containerId = m_model.OpenLootContainer(ENTITY_A, loot, SEED);

            InventoryContainerVO snapshot = m_model.GetContainerVO(containerId);
            Assert.AreEqual(float.MaxValue, snapshot.WeightLimit, "物资箱自身不设负重上限");
            Assert.Greater(snapshot.TotalWeight, 0f, "重物照常入箱, 超重判定留给角色整体");
        }

        [Test]
        public void OpenLootContainer_RaisesOnChangedForTheNewContainer()
        {
            LootContainerConfigSO loot = BuildSupplyBox(3, 5);
            List<int> changedIds = new List<int>();
            m_model.OnChanged += changedIds.Add;

            int containerId = m_model.OpenLootContainer(ENTITY_A, loot, SEED);

            Assert.Contains(containerId, changedIds, "新开的物资箱要广播出去, 界面才知道该刷一张新网格");
        }

        [Test]
        public void OpenLootContainer_NullConfig_ReturnsInvalidId()
        {
            LogAssert.Expect(LogType.Error, new Regex("物资箱配置为空"));

            Assert.Less(m_model.OpenLootContainer(ENTITY_A, null, SEED), 0);
        }

        [Test]
        public void OpenLootContainer_MissingGrid_ReturnsInvalidId()
        {
            LootContainerConfigSO loot = m_kit.CreateLoot("缺网格的箱子", null, 1, 2,
                m_kit.CreateLootEntry(m_kit.CreateItem(1, 1), 1));
            LogAssert.Expect(LogType.Error, new Regex("缺少网格定义"));

            Assert.Less(m_model.OpenLootContainer(ENTITY_A, loot, SEED), 0);
        }
        #endregion

        #region 测试脚手架
        // 造一只常规补给箱, 掉落物全部放得进 4x3 网格
        private LootContainerConfigSO BuildSupplyBox(int rollCountMin, int rollCountMax)
        {
            return m_kit.CreateLoot("补给箱", m_kit.CreateContainer(4, 3, "野战补给箱"), rollCountMin, rollCountMax,
                m_kit.CreateLootEntry(m_kit.CreateItem(1, 1, true, 5), 35, 1, 4),
                m_kit.CreateLootEntry(m_kit.CreateItem(1, 1), 25, 1, 3),
                m_kit.CreateLootEntry(m_kit.CreateItem(2, 1), 20),
                m_kit.CreateLootEntry(m_kit.CreateItem(2, 2), 10));
        }

        // 造一只武器箱, 掉落尺寸比补给箱大, 用来区分两只箱子的内容
        private LootContainerConfigSO BuildWeaponBox(int rollCountMin, int rollCountMax)
        {
            return m_kit.CreateLoot("武器箱", m_kit.CreateContainer(6, 4, "军火网格"), rollCountMin, rollCountMax,
                m_kit.CreateLootEntry(m_kit.CreateItem(5, 2), 15),
                m_kit.CreateLootEntry(m_kit.CreateItem(2, 1), 30),
                m_kit.CreateLootEntry(m_kit.CreateItem(1, 2, true, 5), 25, 1, 3));
        }

        // 断言箱内物品全部出自指定物资箱的掉落表
        private void AssertContentsMatchTable(int containerId, LootContainerConfigSO loot)
        {
            HashSet<int> allowed = CollectEntryItemIds(loot);
            IReadOnlyList<InventoryItemVO> items = m_model.GetContainerVO(containerId).Items;

            Assert.IsNotEmpty(items);
            for (int i = 0; i < items.Count; i++)
                Assert.IsTrue(allowed.Contains(items[i].ItemId), "容器 " + containerId + " 混进了别的箱子的物品");
        }

        // 把掉落清单压成可比较的字符串指纹
        private static string SignatureOf(IReadOnlyList<LootDrop> drops)
        {
            StringBuilder signature = new StringBuilder();

            for (int i = 0; i < drops.Count; i++)
                signature.Append(drops[i].Item.ItemId).Append(':').Append(drops[i].Count).Append(';');

            return signature.ToString();
        }

        // 收集掉落表里的物品号, 用来断言箱内不会冒出表外的东西
        private static HashSet<int> CollectEntryItemIds(LootContainerConfigSO loot)
        {
            HashSet<int> itemIds = new HashSet<int>();
            LootEntry[] entries = loot.Entries;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] == null || entries[i].Item == null) continue;
                itemIds.Add(entries[i].Item.ItemId);
            }

            return itemIds;
        }
        #endregion
    }
}
