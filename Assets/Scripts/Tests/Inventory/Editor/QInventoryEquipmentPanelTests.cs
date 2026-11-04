/*
 * ┌──────────────────────────────────┐
 * │  描    述: 战备面板视图模型测试, 覆盖子载体装配、表头名链路、按容器收敛与级联释放
 * │  类    名: QInventoryEquipmentPanelTests.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

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

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
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

            // 真实流程里子 VM 由各自载体的作用域绑定, 这里逐个补上等价于面板摆好了四张载体
            for (int i = 0; i < m_viewModel.ContainerCount; i++)
                m_viewModel.GetContainer(i).OnBind();
        }

        [TearDown]
        public void TearDown()
        {
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

        #region 容器号非法
        [Test]
        public void Bind_ContainerIdNotRegistered_BuildsEmptySnapshotAndLogsError()
        {
            int missingContainerId = m_kit.NewContainerId();
            EquipmentPanelViewModel panel = new EquipmentPanelViewModel(m_model, new[] { missingContainerId });
            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));

            try
            {
                panel.OnBind();
                panel.GetContainer(0).OnBind();

                InventoryViewModel child = panel.GetContainer(0);
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

        // 取每张载体当前的快照版本号
        private int[] CaptureVersions()
        {
            int[] versions = new int[m_viewModel.ContainerCount];
            for (int i = 0; i < versions.Length; i++) versions[i] = m_viewModel.GetContainer(i).Snapshot.Value.Version;

            return versions;
        }

    }
}