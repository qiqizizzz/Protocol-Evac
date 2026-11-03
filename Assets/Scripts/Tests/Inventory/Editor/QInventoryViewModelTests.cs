/*
* ┌──────────────────────────────────┐
* │  描    述: 背包面板视图模型测试, 覆盖选中、旋转保朝向、放置失败与响应式联动
* │  类    名: QInventoryViewModelTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Text.RegularExpressions;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// InventoryViewModel 测试：不挂 View, 直接由 OnBind 驱动, 验证格子子 VM 与选中态的全部业务分支
    /// </summary>
    public class QInventoryViewModelTests
    {
        private const int NO_SELECTION = -1;
        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private InventoryViewModel m_viewModel;
        private int m_containerId;
        private int m_hintPulseCount;
        private IDisposable m_hintSubscription;

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel();
            m_model.OnRegister();

            m_containerId = m_kit.NewContainerId();
            m_model.RegisterContainer(m_containerId, 4, 4, float.MaxValue);

            m_hintPulseCount = 0;
            m_viewModel = new InventoryViewModel(m_model, m_containerId);
            m_hintSubscription = m_viewModel.HintPulse.Subscribe(() => m_hintPulseCount++);
            m_viewModel.OnBind();
        }

        [TearDown]
        public void TearDown()
        {
            // 与 ViewBase 的拆除顺序保持一致: 先解绑停广播, 再释放, 最后回收响应式成员
            m_hintSubscription.Dispose();
            m_viewModel.OnUnbind();
            m_viewModel.OnDispose();
            m_viewModel.Dispose();
            m_model.OnUnregister();
            m_kit.Dispose();
        }

        #region 装配与格子布局
        [Test]
        public void Bind_BuildsOneSlotViewModelPerCellInRowMajorOrder()
        {
            Assert.AreEqual(16, m_viewModel.SlotCount);
            Assert.AreEqual(16, m_viewModel.Snapshot.Value.CellCount);

            for (int index = 0; index < m_viewModel.SlotCount; index++)
                Assert.AreEqual(new Vector2Int(index % 4, index / 4), m_viewModel.GetSlot(index).Cell);
        }

        [Test]
        public void Bind_LeavesEveryHighlightOffUntilSomethingIsSelected()
        {
            for (int index = 0; index < m_viewModel.SlotCount; index++)
                Assert.IsFalse(m_viewModel.GetSlot(index).IsSelected.Value);

            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value);
            Assert.AreEqual(InventoryHintKind.Idle, m_viewModel.HintKind.Value);
            Assert.IsFalse(m_viewModel.CanRotate.Value);
            Assert.AreEqual(0, m_hintPulseCount);
        }
        #endregion

        #region 选中与反选
        [Test]
        public void InteractAt_OnOccupiedCell_SelectsItemAndLightsItsWholeFootprint()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(1, 2));
            m_hintPulseCount = 0;

            m_viewModel.InteractAt(new Vector2Int(1, 2));

            Assert.AreEqual(instanceId, m_viewModel.SelectedInstanceId.Value);
            Assert.AreEqual(InventoryHintKind.Selected, m_viewModel.HintKind.Value);
            Assert.IsTrue(m_viewModel.CanRotate.Value);
            Assert.AreEqual(1, m_hintPulseCount);

            AssertSelected(1, 2, true);
            AssertSelected(2, 2, true);
            AssertSelected(0, 2, false);
            AssertSelected(1, 3, false);
        }

        [Test]
        public void InteractAt_OnTheSameItemAgain_ClearsSelectionBackToIdle()
        {
            AddItem(2, 2, new Vector2Int(0, 0));
            m_viewModel.InteractAt(new Vector2Int(0, 0));
            m_viewModel.InteractAt(new Vector2Int(1, 1));

            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value);
            Assert.AreEqual(InventoryHintKind.Idle, m_viewModel.HintKind.Value);
            Assert.IsFalse(m_viewModel.CanRotate.Value);
            AssertSelected(0, 0, false);
        }

        [Test]
        public void InteractAt_OnAnotherItem_SwitchesHighlightInsteadOfAdding()
        {
            int first = AddItem(1, 1, new Vector2Int(0, 0));
            int second = AddItem(1, 1, new Vector2Int(3, 3));

            m_viewModel.InteractAt(new Vector2Int(0, 0));
            m_viewModel.InteractAt(new Vector2Int(3, 3));

            Assert.AreEqual(second, m_viewModel.SelectedInstanceId.Value);
            Assert.AreNotEqual(first, m_viewModel.SelectedInstanceId.Value);
            AssertSelected(0, 0, false);
            AssertSelected(3, 3, true);
        }

        [Test]
        public void InteractAt_OnEmptyCellWithoutSelection_IsFullySilent()
        {
            m_viewModel.InteractAt(new Vector2Int(2, 2));

            Assert.AreEqual(InventoryHintKind.Idle, m_viewModel.HintKind.Value);
            Assert.AreEqual(InventoryMoveResult.None, m_viewModel.LastMoveResult);
            Assert.AreEqual(0, m_hintPulseCount);
            Assert.AreEqual(0, m_model.GetContainerVO(m_containerId).Version);
        }

        [Test]
        public void SlotViewModel_NotifyClicked_RoutesIntoPanelSelection()
        {
            int instanceId = AddItem(1, 1, new Vector2Int(3, 0));

            m_viewModel.GetSlot(3).NotifyClicked();

            Assert.AreEqual(instanceId, m_viewModel.SelectedInstanceId.Value);
            AssertSelected(3, 0, true);
        }
        #endregion
        #region 旋转与放置
        [Test]
        public void RotateSelectedItem_ThenPlace_KeepsTheFacingThePlayerChose()
        {
            int instanceId = AddItem(1, 2, Vector2Int.zero);
            m_viewModel.InteractAt(Vector2Int.zero);

            Assert.AreEqual(instanceId, m_viewModel.SelectedInstanceId.Value);
            m_viewModel.RotateSelectedItem();
            Assert.AreEqual(InventoryRotation.Rotate90, ItemOf(instanceId).Rotation);

            m_viewModel.InteractAt(new Vector2Int(2, 2));

            InventoryItemVO placed = ItemOf(instanceId);
            Assert.AreEqual(new Vector2Int(2, 2), placed.Origin);
            Assert.AreEqual(InventoryRotation.Rotate90, placed.Rotation);
            Assert.AreEqual(new Vector2Int(2, 1), placed.Size);
            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value);
            Assert.AreEqual(InventoryHintKind.Placed, m_viewModel.HintKind.Value);
            AssertSelected(2, 2, false);
        }

        [Test]
        public void RotateSelectedItem_WithoutSelection_ChangesNothing()
        {
            m_viewModel.RotateSelectedItem();

            Assert.AreEqual(InventoryMoveResult.None, m_viewModel.LastMoveResult);
            Assert.AreEqual(InventoryHintKind.Idle, m_viewModel.HintKind.Value);
            Assert.AreEqual(0, m_hintPulseCount);
        }

        [Test]
        public void RotateSelectedItem_BlockedByNeighbour_ReportsFailureAndKeepsSelection()
        {
            AddItem(1, 1, new Vector2Int(1, 0));
            int instanceId = AddItem(1, 2, Vector2Int.zero);
            m_viewModel.InteractAt(Vector2Int.zero);
            m_hintPulseCount = 0;

            m_viewModel.RotateSelectedItem();

            Assert.AreEqual(InventoryMoveResult.Occupied, m_viewModel.LastMoveResult);
            Assert.AreEqual(InventoryHintKind.Failed, m_viewModel.HintKind.Value);
            Assert.AreEqual(1, m_hintPulseCount);
            Assert.AreEqual(instanceId, m_viewModel.SelectedInstanceId.Value);
            Assert.AreEqual(InventoryRotation.None, ItemOf(instanceId).Rotation);
        }

        [Test]
        public void RotateSelectedItem_ForFixedConfig_ReportsNotRotatable()
        {
            int itemId = m_kit.CreateItem(1, 2, canRotate: false).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_containerId, itemId, 1, Vector2Int.zero));
            int instanceId = ResolveInstanceId(Vector2Int.zero);

            m_viewModel.InteractAt(Vector2Int.zero);
            Assert.IsFalse(m_viewModel.CanRotate.Value);

            m_viewModel.RotateSelectedItem();
            Assert.AreEqual(InventoryMoveResult.NotRotatable, m_viewModel.LastMoveResult);
            Assert.AreEqual(InventoryHintKind.Failed, m_viewModel.HintKind.Value);
            Assert.AreEqual(instanceId, m_viewModel.SelectedInstanceId.Value);
        }

        [Test]
        public void PlaceSelectedItem_OntoClippedByNeighbour_ReportsFailureAndKeepsOriginalCells()
        {
            AddItem(1, 1, new Vector2Int(1, 1));
            int instanceId = AddItem(2, 1, Vector2Int.zero);
            m_viewModel.InteractAt(Vector2Int.zero);
            m_hintPulseCount = 0;

            m_viewModel.InteractAt(new Vector2Int(0, 1));

            Assert.AreEqual(InventoryMoveResult.Occupied, m_viewModel.LastMoveResult);
            Assert.AreEqual(InventoryHintKind.Failed, m_viewModel.HintKind.Value);
            Assert.AreEqual(1, m_hintPulseCount);
            Assert.AreEqual(instanceId, m_viewModel.SelectedInstanceId.Value);
            Assert.AreEqual(Vector2Int.zero, ItemOf(instanceId).Origin);
            AssertSelected(0, 0, true);
        }

        [Test]
        public void PlaceSelectedItem_IntoFreeArea_CommitsAndReleasesSelection()
        {
            int instanceId = AddItem(2, 2, Vector2Int.zero);
            m_viewModel.InteractAt(new Vector2Int(1, 1));
            m_viewModel.InteractAt(new Vector2Int(2, 2));

            InventoryItemVO moved = ItemOf(instanceId);
            Assert.AreEqual(new Vector2Int(2, 2), moved.Origin);
            Assert.AreEqual(InventoryRotation.None, moved.Rotation);
            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value);
            Assert.AreEqual(InventoryHintKind.Placed, m_viewModel.HintKind.Value);

            for (int index = 0; index < m_viewModel.SlotCount; index++)
                Assert.IsFalse(m_viewModel.GetSlot(index).IsSelected.Value);
        }

        [Test]
        public void ClearSelection_TurnsOffHighlightAndRotateAvailability()
        {
            AddItem(2, 2, new Vector2Int(1, 1));
            m_viewModel.InteractAt(new Vector2Int(2, 2));
            Assert.IsTrue(m_viewModel.CanRotate.Value);

            m_viewModel.ClearSelection();

            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value);
            Assert.AreEqual(InventoryHintKind.Idle, m_viewModel.HintKind.Value);
            Assert.IsFalse(m_viewModel.CanRotate.Value);

            for (int index = 0; index < m_viewModel.SlotCount; index++)
                Assert.IsFalse(m_viewModel.GetSlot(index).IsSelected.Value);
        }
        #endregion

        #region 与模型的联动
        [Test]
        public void ModelChange_WhileUnbound_StopsBroadcast_AndRebindResyncs()
        {
            m_viewModel.OnUnbind();
            Assert.AreEqual(16, m_viewModel.SlotCount);

            AddItem(1, 1, Vector2Int.zero);
            Assert.AreEqual(0, m_viewModel.Snapshot.Value.Version);

            m_viewModel.OnBind();
            Assert.AreEqual(1, m_viewModel.Snapshot.Value.Version);
            Assert.AreEqual(1, m_viewModel.Snapshot.Value.Items.Count);
        }

        [Test]
        public void ModelChange_OnAnotherContainer_DoesNotTouchThisPanel()
        {
            int otherContainerId = m_kit.NewContainerId();
            m_model.RegisterContainer(otherContainerId, 2, 2, float.MaxValue);
            int itemId = m_kit.CreateItem(1, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(otherContainerId, itemId, 1, Vector2Int.zero));

            Assert.AreEqual(0, m_viewModel.Snapshot.Value.Version);
            Assert.AreEqual(16, m_viewModel.SlotCount);
            Assert.AreEqual(m_containerId, m_viewModel.ContainerId);
        }

        [Test]
        public void ModelChange_ThatRemovesTheSelectedItem_DropsSelectionToIdle()
        {
            AddItem(2, 2, Vector2Int.zero);
            m_viewModel.InteractAt(new Vector2Int(1, 1));
            Assert.AreEqual(InventoryHintKind.Selected, m_viewModel.HintKind.Value);

            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));
            m_model.UnregisterContainer(m_containerId);

            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value);
            Assert.AreEqual(InventoryHintKind.Idle, m_viewModel.HintKind.Value);
            Assert.AreEqual(0, m_viewModel.SlotCount);
            Assert.IsFalse(m_viewModel.CanRotate.Value);
            Assert.IsEmpty(m_viewModel.Snapshot.Value.Items);
        }

        [Test]
        public void Dispose_ReleasesEveryChildSlotViewModel()
        {
            InventorySlotViewModel slot = m_viewModel.GetSlot(0);

            m_viewModel.OnUnbind();
            m_viewModel.OnDispose();
            m_viewModel.Dispose();

            Assert.IsTrue(m_viewModel.IsDisposed);
            Assert.AreEqual(0, m_viewModel.SlotCount);

            // 子格子的响应式高亮由父 VM 的 Bundle 托管, 父释放后一并失效
            Assert.IsTrue(slot.IsSelected.IsDisposed);
        }
        #endregion

        // 往模型塞一件测试物品, 返回它的运行时实例号
        private int AddItem(int width, int height, Vector2Int cell, bool canRotate = true)
        {
            int itemId = m_kit.CreateItem(width, height, canRotate).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_containerId, itemId, 1, cell));
            return ResolveInstanceId(cell);
        }

        // 按格子反查实例号
        private int ResolveInstanceId(Vector2Int cell)
        {
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, cell, out int instanceId));
            return instanceId;
        }

        // 从最新快照里取指定实例
        private InventoryItemVO ItemOf(int instanceId)
        {
            InventoryContainerVO vo = m_model.GetContainerVO(m_containerId);
            Assert.IsTrue(vo.TryGetItem(instanceId, out InventoryItemVO item));
            return item;
        }

        // 断言某个格子的高亮状态
        private void AssertSelected(int x, int y, bool expected)
        {
            Assert.AreEqual(expected, m_viewModel.GetSlot(y * 4 + x).IsSelected.Value, "格子 " + x + "," + y + " 高亮不符");
        }
    }
}
