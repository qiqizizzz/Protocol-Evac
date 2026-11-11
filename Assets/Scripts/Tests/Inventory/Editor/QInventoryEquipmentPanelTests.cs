/*
 * ┌──────────────────────────────────┐
 * │  描    述: 战备面板视图模型测试, 覆盖子载体装配、动态物资箱载体、表头名链路、按容器收敛与级联释放
 * │  类    名: QInventoryEquipmentPanelTests.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Text.RegularExpressions;
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

            m_viewModel = new EquipmentPanelViewModel(m_model, m_containerIds);
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
        public void HandleCrossContainerDrop_LastItemLeavesLoot_ClosesTheCarrier()
        {
            int lootContainerId = RegisterLootContainer("搬空收场", 2, 2);
            int itemId = m_kit.CreateItem(1, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItem(lootContainerId, itemId, 1, Vector2Int.zero));

            m_viewModel.OpenLootContainer(lootContainerId);
            InventoryViewModel loot = m_viewModel.LootContainer;
            loot.OnBind();

            loot.BeginDragAt(Vector2Int.zero);
            m_viewModel.GetContainer(0).PlaceDraggedItemAt(Vector2Int.zero);

            Assert.IsNull(m_viewModel.LootContainer, "开箱载体被搬空后右侧不该留一张空壳网格");
            Assert.AreEqual(2, m_lootChangedCount, "开一次箱收一次箱, 各广播一次");
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
        public void CloseLootContainerIfExhausted_OtherContainerOrStillFull_KeepsTheCarrier()
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

            // 别人家的容器空了不动右侧, 自己这只还剩一件也不收
            m_viewModel.CloseLootContainerIfExhausted(m_containerIds[0]);
            m_viewModel.CloseLootContainerIfExhausted(groundContainerId);

            Assert.IsNotNull(m_viewModel.LootContainer);
            Assert.AreEqual(1, m_lootChangedCount, "箱里还有货就不该有多余的收箱广播");
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
