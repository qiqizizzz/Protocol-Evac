/*
 * ┌──────────────────────────────────┐
 * │  描    述: 战备面板视图模型测试, 覆盖子载体装配、动态物资箱载体、表头名链路、按容器收敛与级联释放
 * │  类    名: QInventoryEquipmentPanelTests.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Text.RegularExpressions;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Loot;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Defines;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// EquipmentPanelViewModel 测试：一个容器槽位对应一张载体子视图模型, 验证槽位顺序、表头名与变更收敛
    /// </summary>
    public class QInventoryEquipmentPanelTests
    {
        private const int SLOT_COUNT = 4;
        private static readonly string[] m_slotNames = { "背包", "弹挂", "口袋", "安全箱" };

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private EquipmentPanelViewModel m_viewModel;
        private LootSearchConfigSO m_lootSearchConfig;
        private int[] m_containerIds;
        private int m_lootChangedCount;
        private IDisposable m_lootChangedSubscription;

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            // 用例夹具复用同一个实例, 计数器必须每次归零
            m_lootChangedCount = 0;
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();

            // 四张载体各配一个只在本用例内有效的容器号, 网格尺寸互不相同好区分
            m_containerIds = new int[SLOT_COUNT];
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                m_containerIds[i] = m_kit.NewContainerId();
                m_model.RegisterContainer(m_containerIds[i],
                    new InventoryContainerSpec(2 + i, 3, float.MaxValue, null, m_slotNames[i]));
            }

            m_lootSearchConfig = m_kit.CreateLootSearchConfig();
            m_viewModel = new EquipmentPanelViewModel(m_model, m_containerIds, m_lootSearchConfig);
            m_viewModel.OnBind();
            m_lootChangedSubscription = m_viewModel.LootContainerChanged.Subscribe(() => m_lootChangedCount++);

            // 真实流程里子 VM 由各自载体的作用域绑定, 这里逐个补上等价于面板摆好了四张载体
            for (int i = 0; i < m_viewModel.ContainerCount; i++)
                m_viewModel.GetContainer(i).OnBind();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_lootChangedSubscription != null)
            {
                m_lootChangedSubscription.Dispose();
                m_lootChangedSubscription = null;
            }

            for (int i = 0; i < m_viewModel.ContainerCount; i++)
                m_viewModel.GetContainer(i).OnUnbind();

            m_viewModel.OnUnbind();
            m_viewModel.OnDispose();
            m_viewModel.Dispose();
            m_model.OnUnregister();
            m_kit.Dispose();
        }

        #region 装配与槽位顺序
        [Test]
        public void Ctor_BuildsOneChildViewModelPerContainerIdInGivenOrder()
        {
            Assert.AreEqual(SLOT_COUNT, m_viewModel.ContainerCount);
            Assert.AreEqual(SLOT_COUNT, m_viewModel.Containers.Count);

            for (int i = 0; i < SLOT_COUNT; i++)
            {
                Assert.AreEqual(m_containerIds[i], m_viewModel.Containers[i].ContainerId, "载体 " + i + " 的容器号不符");
                Assert.AreSame(m_viewModel.Containers[i], m_viewModel.GetContainer(i), "GetContainer 必须返回同一个子视图模型");
            }
        }

        [Test]
        public void RoleSlotIds_MatchTheCarrierOrderBakedIntoThePanelPrefab()
        {
            Assert.AreEqual(SLOT_COUNT, InventoryContainerIds.ROLE_SLOT_IDS.Length);
            Assert.AreEqual(InventoryContainerIds.BACKPACK, InventoryContainerIds.ROLE_SLOT_IDS[0]);
            Assert.AreEqual(InventoryContainerIds.RIG, InventoryContainerIds.ROLE_SLOT_IDS[1]);
            Assert.AreEqual(InventoryContainerIds.POCKETS, InventoryContainerIds.ROLE_SLOT_IDS[2]);
            Assert.AreEqual(InventoryContainerIds.SAFEBOX, InventoryContainerIds.ROLE_SLOT_IDS[3]);
        }

        [Test]
        public void Bind_OfPanel_DoesNotBindChildViewModelsItself()
        {
            EquipmentPanelViewModel panel = new EquipmentPanelViewModel(m_model, m_containerIds);
            try
            {
                panel.OnBind();

                // 子 VM 的绑定归每张载体自己的作用域, 面板只负责装配与收尸
                for (int i = 0; i < panel.ContainerCount; i++)
                    Assert.IsFalse(panel.GetContainer(i).IsBound, "载体 " + i + " 不该被面板抢先绑定");
            }
            finally
            {
                panel.OnUnbind();
                panel.OnDispose();
                panel.Dispose();
            }
        }

        [Test]
        public void Bind_GivesEveryChildItsOwnGridSize()
        {
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                InventoryViewModel child = m_viewModel.GetContainer(i);
                int expectedSlots = (2 + i) * 3;

                Assert.AreEqual(expectedSlots, child.SlotCount, "载体 " + i + " 格子数不符");
                Assert.AreEqual(expectedSlots, child.Snapshot.Value.CellCount);
                Assert.AreEqual(m_containerIds[i], child.Snapshot.Value.ContainerId);
            }
        }
        #endregion

        #region 表头显示名链路
        [Test]
        public void Bind_FillsHeaderDisplayNameFromContainerSpec()
        {
            for (int i = 0; i < SLOT_COUNT; i++)
                Assert.AreEqual(m_slotNames[i], m_viewModel.GetContainer(i).Snapshot.Value.DisplayName, "载体 " + i + " 表头名不符");
        }

        [Test]
        public void DisplayName_ComesFromTheContainerSpecNotFromThePanel()
        {
            // 同一张模型上再开一个战利品容器, 表头名跟着规格走, 说明名字不是面板写死的
            int lootContainerId = m_kit.NewContainerId();
            m_model.RegisterContainer(lootContainerId, new InventoryContainerSpec(1, 1, float.MaxValue, null, "临时战利品箱"));

            EquipmentPanelViewModel lootPanel = new EquipmentPanelViewModel(m_model, new[] { lootContainerId });
            try
            {
                lootPanel.OnBind();
                lootPanel.GetContainer(0).OnBind();

                Assert.AreEqual("临时战利品箱", lootPanel.GetContainer(0).Snapshot.Value.DisplayName);
                Assert.AreEqual(lootContainerId, lootPanel.GetContainer(0).Snapshot.Value.ContainerId);
            }
            finally
            {
                lootPanel.GetContainer(0).OnUnbind();
                lootPanel.OnUnbind();
                lootPanel.OnDispose();
                lootPanel.Dispose();
                m_model.UnregisterContainer(lootContainerId);
            }
        }

        [Test]
        public void EmptySnapshot_UsesBlankDisplayNameForTheHeader()
        {
            Assert.IsEmpty(InventoryContainerVO.Empty.DisplayName);
            Assert.AreEqual(0, InventoryContainerVO.Empty.CellCount);
        }
        #endregion

        #region 容器号未注册
        // 没穿装备时容量容器本就不存在, 载体要安静地空着, 不能去模型里讨一条报错
        [Test]
        public void Bind_ContainerIdNotRegistered_BuildsEmptySnapshotSilently()
        {
            int missingContainerId = m_kit.NewContainerId();
            EquipmentPanelViewModel panel = new EquipmentPanelViewModel(m_model, new[] { missingContainerId });

            try
            {
                panel.OnBind();
                panel.GetContainer(0).OnBind();

                InventoryViewModel child = panel.GetContainer(0);
                Assert.IsFalse(child.Visible.Value, "容器未注册时载体该隐藏");
                Assert.AreEqual(0, child.SlotCount);
                Assert.AreEqual(0, child.Snapshot.Value.CellCount);
                Assert.IsEmpty(child.Snapshot.Value.DisplayName);
                Assert.IsEmpty(child.Snapshot.Value.Items);
            }
            finally
            {
                panel.GetContainer(0).OnUnbind();
                panel.OnUnbind();
                panel.OnDispose();
                panel.Dispose();
            }
        }
        #endregion

        #region 变更按容器收敛
        [Test]
        public void ModelChange_OnOneContainer_OnlyThatChildSnapshotAdvances()
        {
            const int target = 2;
            int itemId = m_kit.CreateItem(1, 1).ItemId;
            int[] versionsBefore = CaptureVersions();

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItem(m_containerIds[target], itemId, 1, Vector2Int.zero));

            int[] versionsAfter = CaptureVersions();
            Assert.AreEqual(versionsBefore[target] + 1, versionsAfter[target], "命中的载体快照该前进一版");

            for (int i = 0; i < SLOT_COUNT; i++)
            {
                InventoryViewModel child = m_viewModel.GetContainer(i);

                if (i == target)
                {
                    Assert.AreEqual(1, child.Snapshot.Value.Items.Count);
                    Assert.AreEqual(1, child.Snapshot.Value.UsedCellCount);
                    continue;
                }

                Assert.AreEqual(versionsBefore[i], versionsAfter[i], "载体 " + i + " 不该收到别的容器的变更");
                Assert.IsEmpty(child.Snapshot.Value.Items);
            }
        }

        [Test]
        public void ModelChange_OnUnboundChild_LeavesItsSnapshotStaleUntilRebind()
        {
            InventoryViewModel child = m_viewModel.GetContainer(1);
            int versionBefore = child.Snapshot.Value.Version;

            child.OnUnbind();
            int itemId = m_kit.CreateItem(1, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_containerIds[1], itemId, 1, Vector2Int.zero));

            Assert.AreEqual(versionBefore, child.Snapshot.Value.Version, "解绑期间不该收广播");

            child.OnBind();
            Assert.AreEqual(versionBefore + 1, child.Snapshot.Value.Version, "重新绑定要补回最新快照");
            Assert.AreEqual(1, child.Snapshot.Value.Items.Count);
        }
        #endregion

        #region 级联释放
        [Test]
        public void Dispose_OfPanel_ReleasesEveryChildViewModel()
        {
            InventoryViewModel[] children = new InventoryViewModel[SLOT_COUNT];
            for (int i = 0; i < SLOT_COUNT; i++) children[i] = m_viewModel.GetContainer(i);

            m_viewModel.OnUnbind();
            m_viewModel.OnDispose();
            m_viewModel.Dispose();

            Assert.IsTrue(m_viewModel.IsDisposed);
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                Assert.IsTrue(children[i].IsDisposed, "载体 " + i + " 的子视图模型没跟着释放");
                Assert.IsTrue(children[i].Snapshot.IsDisposed);
                Assert.AreEqual(0, children[i].SlotCount);
            }
        }

        [Test]
        public void Dispose_OfPanel_StopsEveryChildFromTheModelBroadcasts()
        {
            int itemId = m_kit.CreateItem(1, 1).ItemId;

            m_viewModel.OnUnbind();
            m_viewModel.OnDispose();
            m_viewModel.Dispose();

            // 面板收尸后模型再变化, 子 VM 若没退订就会对着已释放的属性写入并报错
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_containerIds[0], itemId, 1, Vector2Int.zero));
            Assert.AreEqual(1, m_model.GetContainerVO(m_containerIds[0]).Items.Count);
        }
        #endregion

        #region 右侧动态物资箱载体
        [Test]
        public void OpenLootContainer_BuildsDynamicChildOutsideTheFixedCarriers()
        {
            int lootContainerId = RegisterLootContainer("便利店纸箱", 3, 2);
            Assert.IsNull(m_viewModel.LootContainer, "没开箱时不该有动态载体");

            m_viewModel.OpenLootContainer(lootContainerId);
            // 真实流程里子 VM 由动态载体的作用域绑定, 绑定才把快照拉起来
            m_viewModel.LootContainer.OnBind();

            Assert.IsNotNull(m_viewModel.LootContainer);
            Assert.AreEqual(lootContainerId, m_viewModel.LootContainer.ContainerId);
            Assert.AreEqual(SLOT_COUNT, m_viewModel.ContainerCount, "动态载体不许挤进固定数组");
            Assert.AreEqual("便利店纸箱", m_viewModel.LootContainer.Snapshot.Value.DisplayName);
            Assert.AreEqual(6, m_viewModel.LootContainer.Snapshot.Value.CellCount);
            Assert.AreEqual(1, m_lootChangedCount, "开出一只箱子该通知视图一次");
        }

        [Test]
        public void OpenLootContainer_WithSearchRequirement_MarksTheLootViewAsSearching()
        {
            int lootContainerId = RegisterLootContainer("需要搜索的物资箱", 2, 2);
            AddLootItem(lootContainerId, 0, 0, InventoryItemRarity.Common);
            InventoryItemVO item = GetItemAt(lootContainerId, 0, 0);

            m_viewModel.OpenLootContainer(lootContainerId, true);

            Assert.IsTrue(m_viewModel.LootContainerSearching, "按 F 打开的物资箱必须进入未搜索阴影态");
            Assert.IsTrue(m_viewModel.IsLootItemSearching(item.InstanceId), "搜索前每一件物品都必须保持阴影");
        }

        [Test]
        public void OpenLootContainer_SearchQueue_OnlyTheCurrentItemOwnsTheIndicator()
        {
            int lootContainerId = RegisterLootContainer("当前搜索目标", 2, 1);
            InventoryItemVO later = AddLootItem(lootContainerId, 1, 0, InventoryItemRarity.Common);
            InventoryItemVO current = AddLootItem(lootContainerId, 0, 0, InventoryItemRarity.Common);

            m_viewModel.OpenLootContainer(lootContainerId, true);

            Assert.IsTrue(m_viewModel.IsLootItemSearchActive(current.InstanceId), "队首物品才拥有搜索图标");
            Assert.IsFalse(m_viewModel.IsLootItemSearchActive(later.InstanceId), "排队中的其他物品不能同时显示图标");

            m_viewModel.TickLootSearch(0.5f);

            Assert.IsFalse(m_viewModel.IsLootItemSearchActive(current.InstanceId), "揭晓后搜索图标必须离开该物品");
            Assert.IsTrue(m_viewModel.IsLootItemSearchActive(later.InstanceId), "下一件开始计时后才接管搜索图标");
        }

        [Test]
        public void OpenLootContainer_EmptySearchContainer_FinishesImmediately()
        {
            int lootContainerId = RegisterLootContainer("空物资箱", 2, 2);

            m_viewModel.OpenLootContainer(lootContainerId, true);

            Assert.IsFalse(m_viewModel.LootContainerSearching, "空箱没有可逐件揭晓的内容, 不能停在永久搜索态");
        }

        [Test]
        public void TickLootSearch_BeforeCommonDuration_RevealsNothing()
        {
            int lootContainerId = RegisterLootContainer("普通物资箱", 2, 1);
            InventoryItemVO first = AddLootItem(lootContainerId, 0, 0, InventoryItemRarity.Common);
            InventoryItemVO second = AddLootItem(lootContainerId, 1, 0, InventoryItemRarity.Common);
            m_viewModel.OpenLootContainer(lootContainerId, true);

            m_viewModel.TickLootSearch(0.49f);

            Assert.IsTrue(m_viewModel.IsLootItemSearching(first.InstanceId));
            Assert.IsTrue(m_viewModel.IsLootItemSearching(second.InstanceId));
        }

        [Test]
        public void TickLootSearch_AfterOneDuration_RevealsOnlyOneItem()
        {
            int lootContainerId = RegisterLootContainer("逐件揭晓", 2, 1);
            InventoryItemVO first = AddLootItem(lootContainerId, 0, 0, InventoryItemRarity.Common);
            InventoryItemVO second = AddLootItem(lootContainerId, 1, 0, InventoryItemRarity.Common);
            m_viewModel.OpenLootContainer(lootContainerId, true);

            m_viewModel.TickLootSearch(0.5f);

            Assert.IsFalse(m_viewModel.IsLootItemSearching(first.InstanceId), "第一件计时结束后只揭晓第一件");
            Assert.IsTrue(m_viewModel.IsLootItemSearching(second.InstanceId), "第二件必须继续隐藏并接着计时");
            Assert.IsTrue(m_viewModel.LootContainerSearching, "还有未揭晓物品时整箱仍处于搜索态");
        }

        [Test]
        public void TickLootSearch_UsesEachItemsOwnRarityDuration()
        {
            int lootContainerId = RegisterLootContainer("品质时长", 2, 1);
            InventoryItemVO common = AddLootItem(lootContainerId, 0, 0, InventoryItemRarity.Common);
            InventoryItemVO legendary = AddLootItem(lootContainerId, 1, 0, InventoryItemRarity.Legendary);
            m_viewModel.OpenLootContainer(lootContainerId, true);

            m_viewModel.TickLootSearch(0.5f);
            Assert.IsFalse(m_viewModel.IsLootItemSearching(common.InstanceId));
            Assert.IsTrue(m_viewModel.IsLootItemSearching(legendary.InstanceId));

            m_viewModel.TickLootSearch(1.9f);
            Assert.IsTrue(m_viewModel.IsLootItemSearching(legendary.InstanceId),
                "传说品质的搜索时长不能被普通品质的时长替代");

            m_viewModel.TickLootSearch(0.1f);
            Assert.IsFalse(m_viewModel.IsLootItemSearching(legendary.InstanceId));
            Assert.IsFalse(m_viewModel.LootContainerSearching);
        }

        [Test]
        public void TickLootSearch_LargeDelta_RevealsEveryItemWhoseDurationElapsed()
        {
            int lootContainerId = RegisterLootContainer("大步长", 2, 1);
            InventoryItemVO first = AddLootItem(lootContainerId, 0, 0, InventoryItemRarity.Common);
            InventoryItemVO second = AddLootItem(lootContainerId, 1, 0, InventoryItemRarity.Common);
            m_viewModel.OpenLootContainer(lootContainerId, true);

            m_viewModel.TickLootSearch(1.1f);

            Assert.IsFalse(m_viewModel.IsLootItemSearching(first.InstanceId));
            Assert.IsFalse(m_viewModel.IsLootItemSearching(second.InstanceId));
            Assert.IsFalse(m_viewModel.LootContainerSearching);
        }

        [Test]
        public void TickLootSearch_RevealsItemsInRowThenColumnOrder()
        {
            int lootContainerId = RegisterLootContainer("固定揭晓顺序", 2, 2);
            InventoryItemVO bottom = AddLootItem(lootContainerId, 1, 0, InventoryItemRarity.Common);
            InventoryItemVO lower = AddLootItem(lootContainerId, 0, 1, InventoryItemRarity.Common);
            InventoryItemVO topLeft = AddLootItem(lootContainerId, 0, 0, InventoryItemRarity.Common);
            m_viewModel.OpenLootContainer(lootContainerId, true);

            m_viewModel.TickLootSearch(0.5f);
            Assert.IsFalse(m_viewModel.IsLootItemSearching(topLeft.InstanceId));
            Assert.IsTrue(m_viewModel.IsLootItemSearching(lower.InstanceId));
            Assert.IsTrue(m_viewModel.IsLootItemSearching(bottom.InstanceId));

            m_viewModel.TickLootSearch(0.5f);
            Assert.IsFalse(m_viewModel.IsLootItemSearching(bottom.InstanceId));
            Assert.IsTrue(m_viewModel.IsLootItemSearching(lower.InstanceId));

            m_viewModel.TickLootSearch(0.5f);
            Assert.IsFalse(m_viewModel.IsLootItemSearching(lower.InstanceId));
            Assert.IsFalse(m_viewModel.LootContainerSearching);

            m_viewModel.TickLootSearch(0.5f);
            Assert.IsFalse(m_viewModel.LootContainerSearching);
        }

        [Test]
        public void TickLootSearch_RaisesOneChangeSignalPerRevealedItem()
        {
            int lootContainerId = RegisterLootContainer("搜索刷新信号", 2, 1);
            AddLootItem(lootContainerId, 0, 0, InventoryItemRarity.Common);
            AddLootItem(lootContainerId, 1, 0, InventoryItemRarity.Common);
            m_viewModel.OpenLootContainer(lootContainerId, true);

            int changedCount = 0;
            using IDisposable subscription = m_viewModel.LootSearchChanged.Subscribe(() => changedCount++);

            m_viewModel.TickLootSearch(0.5f);
            Assert.AreEqual(1, changedCount, "揭晓一件只刷新一次逐件阴影");

            m_viewModel.TickLootSearch(0.5f);
            Assert.AreEqual(2, changedCount, "最后一件揭晓并结束搜索态时再刷新一次");
        }

        [Test]
        public void CloseLootContainer_ClearsPendingItemSearch()
        {
            int lootContainerId = RegisterLootContainer("关闭清搜索", 2, 1);
            InventoryItemVO first = AddLootItem(lootContainerId, 0, 0, InventoryItemRarity.Common);
            InventoryItemVO second = AddLootItem(lootContainerId, 1, 0, InventoryItemRarity.Common);
            m_viewModel.OpenLootContainer(lootContainerId, true);
            m_viewModel.TickLootSearch(0.5f);

            m_viewModel.CloseLootContainer();

            Assert.IsFalse(m_viewModel.LootContainerSearching);
            Assert.IsFalse(m_viewModel.IsLootItemSearching(first.InstanceId));
            Assert.IsFalse(m_viewModel.IsLootItemSearching(second.InstanceId), "关箱后不能把下一件继续留在搜索队列");
        }

        [Test]
        public void OpenLootContainer_SwitchContainer_ClearsPreviousItemSearch()
        {
            int firstContainerId = RegisterLootContainer("第一只搜索箱", 1, 1);
            int secondContainerId = RegisterLootContainer("第二只普通箱", 1, 1);
            InventoryItemVO first = AddLootItem(firstContainerId, 0, 0, InventoryItemRarity.Common);
            InventoryItemVO second = AddLootItem(secondContainerId, 0, 0, InventoryItemRarity.Common);
            m_viewModel.OpenLootContainer(firstContainerId, true);

            m_viewModel.OpenLootContainer(secondContainerId, false);

            Assert.IsFalse(m_viewModel.LootContainerSearching);
            Assert.IsFalse(m_viewModel.IsLootItemSearching(first.InstanceId), "旧箱子的搜索队列必须清掉");
            Assert.IsFalse(m_viewModel.IsLootItemSearching(second.InstanceId), "不要求搜索的箱子直接显示物品");
        }

        [Test]
        public void OpenLootContainer_MissingSearchConfig_LogsErrorAndDoesNotStaySearching()
        {
            int lootContainerId = RegisterLootContainer("缺搜索配置", 1, 1);
            InventoryItemVO item = AddLootItem(lootContainerId, 0, 0, InventoryItemRarity.Common);
            EquipmentPanelViewModel panel = new EquipmentPanelViewModel(m_model, m_containerIds, null);

            try
            {
                LogAssert.Expect(LogType.Error, new Regex("搜索时长配置为空"));
                panel.OpenLootContainer(lootContainerId, true);

                Assert.IsFalse(panel.LootContainerSearching, "配置缺失必须结束搜索态, 不能把玩家永久锁在阴影里");
                Assert.IsFalse(panel.IsLootItemSearching(item.InstanceId));
            }
            finally
            {
                panel.OnUnbind();
                panel.OnDispose();
                panel.Dispose();
            }
        }

        [Test]
        public void TickLootSearch_ZeroOrNegativeDuration_RevealsImmediately()
        {
            LootSearchConfigSO config = m_kit.CreateLootSearchConfig(0f, 0f, 0f, 0f, 0f, -1f);
            EquipmentPanelViewModel panel = new EquipmentPanelViewModel(m_model, m_containerIds, config);
            try
            {
                for (int i = 0; i < panel.ContainerCount; i++) panel.GetContainer(i).OnBind();

                int lootContainerId = RegisterLootContainer("零时长", 2, 1);
                AddLootItem(lootContainerId, 0, 0, InventoryItemRarity.Common);
                AddLootItem(lootContainerId, 1, 0, InventoryItemRarity.Legendary);

                panel.OnBind();
                panel.OpenLootContainer(lootContainerId, true);
                panel.TickLootSearch(0f);

                Assert.IsFalse(panel.LootContainerSearching);
            }
            finally
            {
                for (int i = 0; i < panel.ContainerCount; i++) panel.GetContainer(i).OnUnbind();

                panel.OnUnbind();
                panel.OnDispose();
                panel.Dispose();
            }
        }

        [Test]
        public void OpenLootContainer_SameContainerTwice_KeepsTheSameChildViewModel()
        {
            int lootContainerId = RegisterLootContainer("重复开箱", 2, 2);
            m_viewModel.OpenLootContainer(lootContainerId);
            InventoryViewModel first = m_viewModel.LootContainer;

            m_viewModel.OpenLootContainer(lootContainerId);

            Assert.AreSame(first, m_viewModel.LootContainer, "同一只箱子重复打开不该重建视图模型");
            Assert.IsFalse(first.IsDisposed);
            Assert.AreEqual(1, m_lootChangedCount, "重复打开不该再通知视图");
        }

        [Test]
        public void OpenLootContainer_SwitchContainer_DisposesPreviousChild()
        {
            int firstContainerId = RegisterLootContainer("第一只", 2, 1);
            int secondContainerId = RegisterLootContainer("第二只", 2, 1);
            m_viewModel.OpenLootContainer(firstContainerId);
            InventoryViewModel previous = m_viewModel.LootContainer;

            m_viewModel.OpenLootContainer(secondContainerId);

            Assert.IsTrue(previous.IsDisposed, "换箱子时旧视图模型必须收干净");
            Assert.AreEqual(secondContainerId, m_viewModel.LootContainer.ContainerId);
            Assert.AreEqual(2, m_lootChangedCount);
        }

        [Test]
        public void CloseLootContainer_ClearsChildButKeepsModelContainer()
        {
            int lootContainerId = RegisterLootContainer("关箱留货", 2, 2);
            m_viewModel.OpenLootContainer(lootContainerId);
            m_viewModel.LootContainer.OnBind();

            int itemId = m_kit.CreateItem(1, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItemAuto(lootContainerId, itemId, 1));

            m_viewModel.CloseLootContainer();

            Assert.IsNull(m_viewModel.LootContainer);
            Assert.AreEqual(2, m_lootChangedCount, "开与关各通知视图一次");
            Assert.IsTrue(m_model.IsContainerRegistered(lootContainerId), "关箱只是收界面, 不该注销容器");
            Assert.AreEqual(1, m_model.GetContainerVO(lootContainerId).Items.Count, "箱内物品必须留在模型里");
        }

        [Test]
        public void CloseLootContainer_ClearsTheSearchingState()
        {
            int lootContainerId = RegisterLootContainer("搜索完关闭", 2, 2);
            m_viewModel.OpenLootContainer(lootContainerId, true);

            m_viewModel.CloseLootContainer();

            Assert.IsFalse(m_viewModel.LootContainerSearching, "关箱后不能把搜索态留给下一只容器");
        }

        // 已关闭的物资箱视图模型不能再接收转移结果写入已释放状态
        [Test]
        public void ApplyTransferResult_AfterLootChildDisposed_DoesNotWriteReleasedState()
        {
            int lootContainerId = RegisterLootContainer("释放守卫", 2, 1);
            m_viewModel.OpenLootContainer(lootContainerId);
            InventoryViewModel loot = m_viewModel.LootContainer;
            loot.OnBind();

            m_viewModel.CloseLootContainer();

            Assert.IsTrue(loot.IsDisposed);
            Assert.DoesNotThrow(() => loot.ApplyTransferResult(InventoryMoveResult.Success));
        }

        [Test]
        public void CloseLootContainer_WhenNothingOpen_ChangesNothing()
        {
            m_viewModel.CloseLootContainer();

            Assert.IsNull(m_viewModel.LootContainer);
            Assert.AreEqual(0, m_lootChangedCount, "没开箱时关闭不该广播");
        }

        [Test]
        public void CloseLootContainer_DropsDragSourceOwnedByTheLootChild()
        {
            int lootContainerId = RegisterLootContainer("拖拽收场", 2, 1);
            m_viewModel.OpenLootContainer(lootContainerId);
            m_viewModel.LootContainer.OnBind();
            m_viewModel.NotifyDragBegan(m_viewModel.LootContainer);
            Assert.AreSame(m_viewModel.LootContainer, m_viewModel.DragSource);

            m_viewModel.CloseLootContainer();

            Assert.IsNull(m_viewModel.DragSource, "旧载体收掉后不能再被当成拖拽来源");
        }

        [Test]
        public void OpenLootContainer_UnregisteredContainer_LogsErrorAndOpensNothing()
        {
            LogAssert.Expect(LogType.Error, new Regex("打开物资箱载体失败, 容器未注册"));

            m_viewModel.OpenLootContainer(m_kit.NewContainerId());

            Assert.IsNull(m_viewModel.LootContainer);
            Assert.AreEqual(0, m_lootChangedCount);
        }

        [Test]
        public void LootContainer_FlaggedAsLootCarrier_FixedCarriersAreNot()
        {
            int lootContainerId = RegisterLootContainer("开箱标记", 2, 2);
            m_viewModel.OpenLootContainer(lootContainerId);

            Assert.IsTrue(m_viewModel.LootContainer.IsLootContainer, "右侧动态载体就是开箱载体");
            for (int i = 0; i < m_viewModel.ContainerCount; i++)
                Assert.IsFalse(m_viewModel.GetContainer(i).IsLootContainer, "玩家自己的载体不是开箱载体");
        }

        [Test]
        public void HandleCrossContainerDrop_LastItemLeavesRegisteredLoot_KeepsTheCarrier()
        {
            int lootContainerId = RegisterLootContainer("搬空留壳", 2, 2);
            int itemId = m_kit.CreateItem(1, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItem(lootContainerId, itemId, 1, Vector2Int.zero));

            m_viewModel.OpenLootContainer(lootContainerId);
            InventoryViewModel loot = m_viewModel.LootContainer;
            loot.OnBind();

            loot.BeginDragAt(Vector2Int.zero);
            m_viewModel.GetContainer(0).PlaceDraggedItemAt(Vector2Int.zero);

            Assert.IsNotNull(m_viewModel.LootContainer, "物资箱有容量, 搬空也只是空网格, 右侧那一列要留着");
            Assert.AreEqual(1, m_lootChangedCount, "搬空不该多广播一次收箱");
            Assert.AreEqual(0, m_model.GetContainerVO(lootContainerId).Items.Count);
            Assert.AreEqual(1, m_model.GetContainerVO(m_containerIds[0]).Items.Count, "物品要落到固定载体里");
        }

        [Test]
        public void HandleCrossContainerDrop_LootContainerUnregistered_ClosesTheCarrier()
        {
            int lootContainerId = RegisterLootContainer("容器先没", 2, 2);
            int itemId = m_kit.CreateItem(1, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItem(lootContainerId, itemId, 1, Vector2Int.zero));

            m_viewModel.OpenLootContainer(lootContainerId);
            InventoryViewModel loot = m_viewModel.LootContainer;
            loot.OnBind();

            loot.BeginDragAt(Vector2Int.zero);
            // 地面堆在物品被搬空的同一帧被收掉, 视图侧不能再攥着这只已注销的容器
            m_model.UnregisterContainer(lootContainerId);
            LogAssert.Expect(LogType.Error, new Regex($"容器未注册：{lootContainerId}"));

            m_viewModel.GetContainer(0).PlaceDraggedItemAt(Vector2Int.zero);

            Assert.IsNull(m_viewModel.LootContainer, "容器都没了, 右侧载体必须跟着收");
            Assert.AreEqual(2, m_lootChangedCount);
        }

        [Test]
        public void CloseLootContainerIfRemoved_EmptyButRegistered_KeepsTheCarrier()
        {
            int lootContainerId = RegisterLootContainer("还剩货", 2, 2);
            int itemId = m_kit.CreateItem(1, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(lootContainerId, itemId, 1, Vector2Int.zero));
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(lootContainerId, itemId, 1, new Vector2Int(1, 0)));

            m_viewModel.OpenLootContainer(lootContainerId);
            m_viewModel.LootContainer.OnBind();

            int instanceId = m_model.GetContainerVO(lootContainerId).Items[0].InstanceId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryDropItem(lootContainerId, instanceId, out int groundContainerId));
            Assert.AreEqual(1, m_model.GetContainerVO(lootContainerId).Items.Count, "箱里该剩最后一件");

            // 别人家的容器不动右侧, 自己这只还剩一件也不收
            m_viewModel.CloseLootContainerIfRemoved(m_containerIds[0]);
            m_viewModel.CloseLootContainerIfRemoved(groundContainerId);

            Assert.IsNotNull(m_viewModel.LootContainer);
            Assert.AreEqual(1, m_lootChangedCount, "箱里还有货就不该有多余的收箱广播");

            // 最后一件也被搬走, 容器仍在册, 空网格照样留着
            int lastInstanceId = m_model.GetContainerVO(lootContainerId).Items[0].InstanceId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryDropItem(lootContainerId, lastInstanceId, out int lastGroundContainerId));
            m_viewModel.CloseLootContainerIfRemoved(lootContainerId);

            Assert.IsNotNull(m_viewModel.LootContainer, "物资箱掏空后仍然在册, 载体不该跟着收");
            Assert.AreEqual(1, m_lootChangedCount);
        }

        [Test]
        public void CloseLootContainerIfRemoved_Unregistered_ClosesTheCarrier()
        {
            int lootContainerId = RegisterLootContainer("堆被收回", 2, 2);
            int itemId = m_kit.CreateItem(1, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItem(lootContainerId, itemId, 1, Vector2Int.zero));

            m_viewModel.OpenLootContainer(lootContainerId);
            m_viewModel.LootContainer.OnBind();

            int instanceId = m_model.GetContainerVO(lootContainerId).Items[0].InstanceId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryDropItem(lootContainerId, instanceId, out _));
            // 地面堆掏空的同一帧会被收回注册, 右侧那列必须跟着收
            m_model.UnregisterContainer(lootContainerId);

            m_viewModel.CloseLootContainerIfRemoved(lootContainerId);

            Assert.IsNull(m_viewModel.LootContainer, "容器已被收回, 右侧载体必须跟着收");
            Assert.AreEqual(2, m_lootChangedCount);
        }

        [Test]
        public void TryStowLootItem_TakesRigThenPocketsThenBackpackInOrder()
        {
            // 胸挂与背包的容量靠容器装备撑起, 口袋是先天容量, 和真实装配处一致
            WearCapacityGear(InventoryContainerIds.CHESTRIG_SLOT, InventoryEquipSlot.ChestRig, 1, 1);
            RegisterPocketContainer(1, 1);
            WearCapacityGear(InventoryContainerIds.BACKPACK_SLOT, InventoryEquipSlot.Backpack, 1, 1);

            int lootContainerId = RegisterLootContainer("依次收纳", 3, 1);
            int itemId = m_kit.CreateItem(1, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(lootContainerId, itemId, 1, new Vector2Int(0, 0)));
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(lootContainerId, itemId, 1, new Vector2Int(1, 0)));
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(lootContainerId, itemId, 1, new Vector2Int(2, 0)));

            m_viewModel.OpenLootContainer(lootContainerId);
            m_viewModel.LootContainer.OnBind();

            int[] instanceIds = { m_model.GetContainerVO(lootContainerId).Items[0].InstanceId,
                                  m_model.GetContainerVO(lootContainerId).Items[1].InstanceId,
                                  m_model.GetContainerVO(lootContainerId).Items[2].InstanceId };

            for (int i = 0; i < instanceIds.Length; i++)
            {
                Assert.AreEqual(InventoryMoveResult.Success, m_viewModel.TryStowLootItem(lootContainerId, instanceIds[i]),
                    "第" + i + "件该收进玩家容器");
            }

            Assert.AreEqual(1, m_model.GetContainerVO(InventoryContainerIds.RIG).Items.Count, "第一件进胸挂");
            Assert.AreEqual(1, m_model.GetContainerVO(InventoryContainerIds.POCKETS).Items.Count, "第二件进口袋");
            Assert.AreEqual(1, m_model.GetContainerVO(InventoryContainerIds.BACKPACK).Items.Count, "第三件进背包");
            Assert.AreEqual(0, m_model.GetContainerVO(lootContainerId).Items.Count);
            Assert.IsNotNull(m_viewModel.LootContainer, "物资箱仍在册, 收纳完不该收掉右侧那一列");
            Assert.AreEqual(1, m_lootChangedCount);
        }

        [Test]
        public void TryStowLootItem_NoRoomAnywhere_KeepsItemInLoot()
        {
            WearCapacityGear(InventoryContainerIds.CHESTRIG_SLOT, InventoryEquipSlot.ChestRig, 1, 1);
            RegisterPocketContainer(1, 1);
            WearCapacityGear(InventoryContainerIds.BACKPACK_SLOT, InventoryEquipSlot.Backpack, 1, 1);

            int fillerId = m_kit.CreateItem(1, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItemAuto(InventoryContainerIds.RIG, fillerId, 1));
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItemAuto(InventoryContainerIds.POCKETS, fillerId, 1));
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItemAuto(InventoryContainerIds.BACKPACK, fillerId, 1));

            int lootContainerId = RegisterLootContainer("塞不下", 2, 1);
            int itemId = m_kit.CreateItem(1, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(lootContainerId, itemId, 1, Vector2Int.zero));

            m_viewModel.OpenLootContainer(lootContainerId);
            InventoryViewModel loot = m_viewModel.LootContainer;
            loot.OnBind();
            int instanceId = m_model.GetContainerVO(lootContainerId).Items[0].InstanceId;

            Assert.IsFalse(loot.TryQuickMoveToOwnContainer(instanceId),
                "玩家容器全满时收纳失败, 但物品绝不能落回地面");

            Assert.AreEqual(1, m_model.GetContainerVO(lootContainerId).Items.Count, "东西必须还留在箱里");
            Assert.IsNotNull(m_viewModel.LootContainer);
            Assert.IsFalse(loot.IsDragging, "收纳失败也要清掉拖拽身份, 否则下一次点击会串味");
        }

        [Test]
        public void TryStowLootItem_SkipsUnregisteredCandidates()
        {
            // 只穿背包: 胸挂与口袋没撑起容量时不该报错, 直接跳到下一档
            WearCapacityGear(InventoryContainerIds.BACKPACK_SLOT, InventoryEquipSlot.Backpack, 2, 1);
            Assert.IsFalse(m_model.IsContainerRegistered(InventoryContainerIds.RIG), "没穿胸挂就没有胸挂容量");
            Assert.IsFalse(m_model.IsContainerRegistered(InventoryContainerIds.POCKETS), "没配口袋底格就没有口袋容量");

            int lootContainerId = RegisterLootContainer("只有背包", 2, 1);
            int itemId = m_kit.CreateItem(1, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(lootContainerId, itemId, 1, Vector2Int.zero));

            m_viewModel.OpenLootContainer(lootContainerId);
            InventoryViewModel loot = m_viewModel.LootContainer;
            loot.OnBind();
            int instanceId = m_model.GetContainerVO(lootContainerId).Items[0].InstanceId;

            Assert.IsTrue(loot.TryQuickMoveToOwnContainer(instanceId), "没穿的档位跳过, 东西进背包");
            Assert.AreEqual(1, m_model.GetContainerVO(InventoryContainerIds.BACKPACK).Items.Count);
        }

        [Test]
        public void Dispose_OfPanel_ReleasesLootChildAndKeepsModelContainer()
        {
            int lootContainerId = RegisterLootContainer("面板收场", 2, 1);
            m_viewModel.OpenLootContainer(lootContainerId);
            m_viewModel.LootContainer.OnBind();
            InventoryViewModel lootChild = m_viewModel.LootContainer;

            m_viewModel.OnUnbind();
            m_viewModel.OnDispose();
            m_viewModel.Dispose();

            Assert.IsTrue(lootChild.IsDisposed, "面板收尸要连动态载体的子视图模型一起收");
            Assert.IsTrue(m_model.IsContainerRegistered(lootContainerId), "面板销毁不该注销物资箱容器");
        }

        // 注册一只只在本用例内有效的物资箱容器, 返回它的容器号
        private int RegisterLootContainer(string displayName, int width, int height)
        {
            int containerId = m_kit.NewContainerId();
            m_model.RegisterContainer(containerId,
                new InventoryContainerSpec(width, height, float.MaxValue, null, displayName));

            return containerId;
        }

        // 往装备槽塞一件容器装备, 模型顺势撑起它的容量容器: 卸下装备时容量会被收回, 所以收纳用例必须真穿
        private void WearCapacityGear(int equipSlotId, InventoryEquipSlot role, int width, int height)
        {
            InventoryItemConfigSO gear = m_kit.CreateContainerItem(m_kit.CreateContainer(width, height),
                equipSlot: role);

            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItemAuto(equipSlotId, gear.ItemId, 1),
                "容器装备穿戴失败：" + role);
        }

        // 口袋是先天常驻容量, 不经装备槽, 按规格直接注册
        private void RegisterPocketContainer(int width, int height)
        {
            m_model.RegisterContainer(InventoryContainerIds.POCKETS,
                new InventoryContainerSpec(width, height, float.MaxValue, null, "口袋"));
        }

        // 往物资箱指定格子放一件单格物品, 返回模型生成的渲染切片
        private InventoryItemVO AddLootItem(int containerId, int x, int y, InventoryItemRarity rarity)
        {
            int itemId = m_kit.CreateItem(1, 1, false, rarity: rarity).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItem(containerId, itemId, 1, new Vector2Int(x, y)));

            return GetItemAt(containerId, x, y);
        }

        // 按落点从容器快照里找出对应物品
        private InventoryItemVO GetItemAt(int containerId, int x, int y)
        {
            Vector2Int origin = new Vector2Int(x, y);
            InventoryContainerVO container = m_model.GetContainerVO(containerId);

            for (int i = 0; i < container.Items.Count; i++)
                if (container.Items[i].Origin == origin) return container.Items[i];

            Assert.Fail($"容器 {containerId} 的格子 {origin} 没有物品");
            return null;
        }
        #endregion

        // 取每张载体当前的快照版本号
        private int[] CaptureVersions()
        {
            int[] versions = new int[m_viewModel.ContainerCount];
            for (int i = 0; i < versions.Length; i++) versions[i] = m_viewModel.GetContainer(i).Snapshot.Value.Version;

            return versions;
        }

    }
}
