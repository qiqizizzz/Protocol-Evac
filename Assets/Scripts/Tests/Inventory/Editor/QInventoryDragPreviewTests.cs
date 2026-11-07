/*
* ┌──────────────────────────────────┐
* │  描    述: 背包落点预览测试, 覆盖转移预判契约与格子着色状态机
* │  类    名: QInventoryDragPreviewTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;

namespace Tests.Inventory
{
    /// <summary>
    /// 落点预览测试：PeekTransferItem 的纯查询契约、格子 InventoryCellMark 状态机的画与擦、跨载体拖拽的来源解析
    /// </summary>
    public class QInventoryDragPreviewTests
    {
        private const int NO_DRAG = -1;
        private const int GRID_WIDTH = 4;

        // 挖一个洞的 4x3 网格, 洞在 (1,1)
        private const int HOLE_INDEX = 5;
        private static readonly Vector2Int HOLE = new Vector2Int(1, 1);

        private readonly List<InventoryViewModel> m_viewModels = new List<InventoryViewModel>();
        private readonly List<EquipmentPanelViewModel> m_panels = new List<EquipmentPanelViewModel>();

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private int m_sourceId;
        private int m_targetId;

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();

            // 两张 4x4 且不限重的容器, 需要别的几何时在用例里另开容器号
            m_sourceId = m_kit.NewContainerId();
            m_targetId = m_kit.NewContainerId();
            Register(m_sourceId, GRID_WIDTH, GRID_WIDTH);
            Register(m_targetId, GRID_WIDTH, GRID_WIDTH);
        }

        [TearDown]
        public void TearDown()
        {
            // 面板的 OnDisposeCore 会级联收尸子载体, 所以先拆面板再拆散装的载体
            for (int i = 0; i < m_panels.Count; i++)
            {
                EquipmentPanelViewModel panel = m_panels[i];
                for (int slot = 0; slot < panel.ContainerCount; slot++)
                    panel.GetContainer(slot).OnUnbind();

                panel.OnUnbind();
                panel.OnDispose();
                panel.Dispose();
            }
            m_panels.Clear();

            for (int i = 0; i < m_viewModels.Count; i++)
            {
                m_viewModels[i].OnUnbind();
                m_viewModels[i].OnDispose();
                m_viewModels[i].Dispose();
            }
            m_viewModels.Clear();

            m_model.OnUnregister();
            m_kit.Dispose();
        }

        #region 单载体落点预览
        [Test]
        public void PreviewDropAt_EmptyCell_MarksWholeFootprintDropValid()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));

            viewModel.BeginDragAt(new Vector2Int(0, 0));
            viewModel.PreviewDropAt(new Vector2Int(1, 2));

            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(viewModel, 1, 2));
            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(viewModel, 2, 2));
            Assert.AreEqual(InventoryCellMark.None, MarkAt(viewModel, 0, 0), "拖起来的原位不该留色");
            Assert.AreEqual(InventoryCellMark.None, MarkAt(viewModel, 0, 2));
        }

        [Test]
        public void PreviewDropAt_OntoOwnOrigin_KeepsEveryCellPlain()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            AddItem(m_sourceId, 2, 2, new Vector2Int(1, 1));

            viewModel.BeginDragAt(new Vector2Int(1, 1));
            viewModel.PreviewDropAt(new Vector2Int(1, 1));

            AssertAllMarksNone(viewModel, "落回原位不算落点, 不该刷一片绿");
        }

        [Test]
        public void PreviewDropAt_PartiallyOverOwnFootprint_StillMarksDropValid()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            AddItem(m_sourceId, 2, 2, new Vector2Int(0, 0));

            viewModel.BeginDragAt(new Vector2Int(0, 0));
            viewModel.PreviewDropAt(new Vector2Int(1, 1));

            // 自己压自己不算交换, 模型忽略自身占位后给的是可直落
            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(viewModel, 1, 1));
            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(viewModel, 2, 1));
            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(viewModel, 1, 2));
            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(viewModel, 2, 2));
        }

        [Test]
        public void PreviewDropAt_OverAnotherItem_MarksWholeFootprintDropSwap()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            AddItem(m_sourceId, 2, 2, new Vector2Int(0, 0));
            AddItem(m_sourceId, 2, 2, new Vector2Int(2, 2));

            viewModel.BeginDragAt(new Vector2Int(0, 0));
            viewModel.PreviewDropAt(new Vector2Int(2, 2));

            Assert.AreEqual(InventoryCellMark.DropSwap, MarkAt(viewModel, 2, 2));
            Assert.AreEqual(InventoryCellMark.DropSwap, MarkAt(viewModel, 3, 2));
            Assert.AreEqual(InventoryCellMark.DropSwap, MarkAt(viewModel, 2, 3));
            Assert.AreEqual(InventoryCellMark.DropSwap, MarkAt(viewModel, 3, 3));
        }

        [Test]
        public void PreviewDropAt_SwapWouldBreakLayout_MarksDropInvalid()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            AddItem(m_sourceId, 1, 1, new Vector2Int(3, 3));
            AddItem(m_sourceId, 3, 1, new Vector2Int(0, 0));

            viewModel.BeginDragAt(new Vector2Int(3, 3));
            // 方块落下本身没问题, 但横条顶回 (3,3) 会伸出右边界
            viewModel.PreviewDropAt(new Vector2Int(0, 0));

            Assert.AreEqual(InventoryCellMark.DropInvalid, MarkAt(viewModel, 0, 0));
        }

        [Test]
        public void PreviewDropAt_OverHole_MarksHoleCellDropInvalid()
        {
            int notchedId = m_kit.NewContainerId();
            RegisterNotched(notchedId);
            InventoryViewModel viewModel = BindCarrier(notchedId);
            AddItem(notchedId, 2, 1, new Vector2Int(0, 0));

            viewModel.BeginDragAt(new Vector2Int(0, 0));
            viewModel.PreviewDropAt(HOLE);

            Assert.AreEqual(InventoryCellMark.DropInvalid, viewModel.GetSlot(HOLE_INDEX).Mark.Value,
                "空洞格也要画出被拒的红, 玩家才知道这落不行");
            Assert.AreEqual(InventoryCellMark.DropInvalid, MarkAt(viewModel, 2, 1));
        }

        [Test]
        public void PreviewDropAt_WithoutDragOrSelection_KeepsEveryCellPlain()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));

            viewModel.PreviewDropAt(new Vector2Int(2, 2));

            AssertAllMarksNone(viewModel, "手上没东西就不该有落点预览");
        }

        [Test]
        public void PreviewDropAt_WithSelectionOnly_MarksDropValid()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));

            // 不走拖拽, 只用点选也应拿到同一套落点反馈
            viewModel.InteractAt(new Vector2Int(0, 0));
            viewModel.PreviewDropAt(new Vector2Int(2, 3));

            Assert.AreEqual(InventoryCellMark.Selected, MarkAt(viewModel, 0, 0));
            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(viewModel, 2, 3));
            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(viewModel, 3, 3));
        }

        [Test]
        public void PreviewDropAt_OverlappingSelectedFootprint_PreviewWinsOnSharedCells()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            AddItem(m_sourceId, 2, 2, new Vector2Int(0, 0));

            viewModel.InteractAt(new Vector2Int(0, 0));
            viewModel.PreviewDropAt(new Vector2Int(1, 1));

            // 一格只有一个颜色, 预览盖在选中之上
            Assert.AreEqual(InventoryCellMark.Selected, MarkAt(viewModel, 0, 0));
            Assert.AreEqual(InventoryCellMark.Selected, MarkAt(viewModel, 1, 0));
            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(viewModel, 1, 1));
        }

        [Test]
        public void ClearDropPreview_OutsidePreviewRegion_KeepsThePreview()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            AddItem(m_sourceId, 2, 2, new Vector2Int(0, 0));

            viewModel.BeginDragAt(new Vector2Int(0, 0));
            viewModel.PreviewDropAt(new Vector2Int(2, 2));
            // 多格物品内部挪动时的伪 exit, 不该把预览闪掉
            viewModel.ClearDropPreview(new Vector2Int(0, 0));

            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(viewModel, 2, 2));
        }

        [Test]
        public void ClearDropPreview_InsidePreviewRegion_ErasesEveryMark()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            AddItem(m_sourceId, 2, 2, new Vector2Int(0, 0));

            viewModel.BeginDragAt(new Vector2Int(0, 0));
            viewModel.PreviewDropAt(new Vector2Int(2, 2));
            viewModel.ClearDropPreview(new Vector2Int(2, 2));

            AssertAllMarksNone(viewModel, "指针已经离开落点");
        }

        [Test]
        public void PlaceDraggedItemAt_AfterPreview_LeavesNoResidualMarks()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            int moving = AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));

            viewModel.BeginDragAt(new Vector2Int(0, 0));
            viewModel.PreviewDropAt(new Vector2Int(2, 2));
            viewModel.PlaceDraggedItemAt(new Vector2Int(2, 2));

            Assert.AreEqual(new Vector2Int(2, 2), ItemOf(m_sourceId, moving).Origin);
            AssertAllMarksNone(viewModel, "落子完成后预览必须擦干净");
        }

        [Test]
        public void CancelDrag_AfterPreview_ClearsDragAndMarks()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            int moving = AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));

            viewModel.BeginDragAt(new Vector2Int(0, 0));
            viewModel.PreviewDropAt(new Vector2Int(1, 3));
            viewModel.CancelDrag();

            Assert.AreEqual(NO_DRAG, viewModel.DraggingInstanceId);
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_sourceId, moving).Origin, "取消拖拽不该动过模型");
            AssertAllMarksNone(viewModel, "取消拖拽后不该留预览");
        }
        #endregion

        #region 跨载体落点预览
        [Test]
        public void Panel_PreviewDropAt_OnTargetCarrier_MarksTargetCells()
        {
            EquipmentPanelViewModel panel = BuildPanel(m_sourceId, m_targetId);
            InventoryViewModel left = panel.GetContainer(0);
            InventoryViewModel right = panel.GetContainer(1);
            AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));

            left.BeginDragAt(new Vector2Int(0, 0));
            right.PreviewDropAt(new Vector2Int(1, 1));

            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(right, 1, 1));
            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(right, 2, 1));
            AssertAllMarksNone(left, "来源面板不该跟着画落点");
        }

        [Test]
        public void Panel_PreviewDropAt_TargetTooSmall_MarksDropInvalid()
        {
            int smallId = m_kit.NewContainerId();
            Register(smallId, 2, 2);
            EquipmentPanelViewModel panel = BuildPanel(m_sourceId, smallId);
            InventoryViewModel left = panel.GetContainer(0);
            InventoryViewModel right = panel.GetContainer(1);
            AddItem(m_sourceId, 3, 1, new Vector2Int(0, 0));

            left.BeginDragAt(new Vector2Int(0, 0));
            right.PreviewDropAt(Vector2Int.zero);

            Assert.AreEqual(InventoryCellMark.DropInvalid, right.GetSlot(0).Mark.Value);
            Assert.AreEqual(InventoryCellMark.DropInvalid, right.GetSlot(1).Mark.Value, "物品足迹覆盖到的格全都要涂");
            Assert.AreEqual(InventoryCellMark.None, right.GetSlot(2).Mark.Value, "预览不该越过物品足迹的行");
        }

        [Test]
        public void Panel_DropAfterPreview_ClearsMarksOnBothCarriers()
        {
            EquipmentPanelViewModel panel = BuildPanel(m_sourceId, m_targetId);
            InventoryViewModel left = panel.GetContainer(0);
            InventoryViewModel right = panel.GetContainer(1);
            int moving = AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));

            left.BeginDragAt(new Vector2Int(0, 0));
            right.PreviewDropAt(new Vector2Int(1, 1));
            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(right, 1, 1));

            right.PlaceDraggedItemAt(new Vector2Int(1, 1));

            Assert.AreEqual(new Vector2Int(1, 1), ItemOf(m_targetId, moving).Origin);
            AssertAllMarksNone(right, "落地后目标面板不该留预览");
            AssertAllMarksNone(left, "落地后来源面板不该留预览");
        }
        #endregion

        #region 转移预判契约
        [Test]
        public void PeekTransferItem_EmptyTarget_ReportsSuccessWithoutSwap()
        {
            int moving = AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));

            InventoryMoveResult verdict = m_model.PeekTransferItem(m_sourceId, m_targetId, moving,
                new Vector2Int(1, 1), out bool willSwap, out InventoryRotation adopted);

            Assert.AreEqual(InventoryMoveResult.Success, verdict);
            Assert.IsFalse(willSwap);
            Assert.AreEqual(InventoryRotation.None, adopted, "能原样放下就不该动朝向");
        }

        [Test]
        public void PeekTransferItem_OccupiedTarget_ReportsSuccessWithSwap()
        {
            int moving = AddItem(m_sourceId, 1, 1, new Vector2Int(0, 0));
            AddItem(m_sourceId, 1, 1, new Vector2Int(2, 2));

            InventoryMoveResult verdict = m_model.PeekTransferItem(m_sourceId, m_sourceId, moving,
                new Vector2Int(2, 2), out bool willSwap, out InventoryRotation adopted);

            Assert.AreEqual(InventoryMoveResult.Success, verdict);
            Assert.IsTrue(willSwap, "顶开第三方就该预报为交换");
            Assert.AreEqual(InventoryRotation.None, adopted);
        }

        [Test]
        public void PeekTransferItem_RejectedByRules_KeepsTheRejectionVerdict()
        {
            int notchedId = m_kit.NewContainerId();
            RegisterNotched(notchedId);
            int moving = AddItem(notchedId, 2, 1, new Vector2Int(0, 0));
            int version = m_model.GetContainerVO(notchedId).Version;

            Assert.AreEqual(InventoryMoveResult.OutOfBounds,
                m_model.PeekTransferItem(notchedId, notchedId, moving, HOLE, out bool willSwap, out _));
            Assert.IsFalse(willSwap);
            Assert.AreEqual(version, m_model.GetContainerVO(notchedId).Version);

            // 预判和真落必须给同一个结论, 否则玩家会看到绿灯却放不下
            Assert.AreEqual(InventoryMoveResult.OutOfBounds,
                m_model.TryTransferItem(notchedId, notchedId, moving, HOLE));
        }

        [Test]
        public void PeekTransferItem_UnknownContainer_ReportsNoneWithoutLogging()
        {
            int moving = AddItem(m_sourceId, 1, 1, new Vector2Int(0, 0));
            int missingId = m_kit.NewContainerId();

            // 悬停每帧都在问, 这里必须静默, 一旦走 QLog 就会刷屏
            Assert.AreEqual(InventoryMoveResult.None,
                m_model.PeekTransferItem(missingId, m_sourceId, moving, Vector2Int.zero, out bool _, out _));
            Assert.AreEqual(InventoryMoveResult.None,
                m_model.PeekTransferItem(m_sourceId, missingId, moving, Vector2Int.zero, out _, out _));
        }

        [Test]
        public void PeekTransferItem_IsPureQuery_KeepsVersionAndOrigins()
        {
            int moving = AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));
            int version = m_model.GetContainerVO(m_sourceId).Version;

            for (int y = 0; y < GRID_WIDTH; y++)
            for (int x = 0; x < GRID_WIDTH; x++)
                m_model.PeekTransferItem(m_sourceId, m_sourceId, moving, new Vector2Int(x, y), out bool _, out _);

            Assert.AreEqual(version, m_model.GetContainerVO(m_sourceId).Version, "预判不该提交任何版本");
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_sourceId, moving).Origin);
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_sourceId, new Vector2Int(1, 0), out int stillThere));
            Assert.AreEqual(moving, stillThere);
        }
        #endregion

        // ==================== Private Function ====================
        // 注册一张不限重的矩形容器
        private void Register(int containerId, int width, int height)
            => m_model.RegisterContainer(containerId, new InventoryContainerSpec(width, height, float.MaxValue));

        // 注册一张 4x3 带洞的异形容器
        private void RegisterNotched(int containerId)
            => m_model.RegisterContainer(containerId, new InventoryContainerSpec(GRID_WIDTH, 3, float.MaxValue,
                InventoryContainerConfigSO.ParseShape(new[] { "####", "#.##", "####" }, GRID_WIDTH, 3)));

        // 按真实流程绑定一个载体视图模型, 收尾交给 TearDown
        private InventoryViewModel BindCarrier(int containerId)
        {
            InventoryViewModel viewModel = new InventoryViewModel(m_model, containerId);
            viewModel.OnBind();
            m_viewModels.Add(viewModel);
            return viewModel;
        }

        // 组一面战备面板, 子载体按 View 作用域的等价流程各自绑定
        private EquipmentPanelViewModel BuildPanel(params int[] containerIds)
        {
            EquipmentPanelViewModel panel = new EquipmentPanelViewModel(m_model, containerIds);
            m_panels.Add(panel);

            panel.OnBind();
            for (int i = 0; i < panel.ContainerCount; i++)
                panel.GetContainer(i).OnBind();

            return panel;
        }

        // 往指定容器塞一件物品并回它的实例号
        private int AddItem(int containerId, int width, int height, Vector2Int cell)
        {
            int itemId = m_kit.CreateItem(width, height, true, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(containerId, itemId, 1, cell));
            return ResolveAt(containerId, cell);
        }

        // 按格子正查实例号
        private int ResolveAt(int containerId, Vector2Int cell)
        {
            Assert.IsTrue(m_model.TryGetInstanceIdAt(containerId, cell, out int instanceId), "放完查不到实例");
            return instanceId;
        }

        // 从指定容器的最新快照里取实例
        private InventoryItemVO ItemOf(int containerId, int instanceId)
        {
            InventoryContainerVO container = m_model.GetContainerVO(containerId);
            Assert.IsTrue(container.TryGetItem(instanceId, out InventoryItemVO item),
                "实例 " + instanceId + " 不在容器 " + containerId);
            return item;
        }

        // 读某一格的着色语义, 网格统一按 4 列排
        private static InventoryCellMark MarkAt(InventoryViewModel viewModel, int x, int y)
            => viewModel.GetSlot(y * GRID_WIDTH + x).Mark.Value;

        // 整盘检查所有格子的着色语义
        private static void AssertAllMarksNone(InventoryViewModel viewModel, string message)
        {
            for (int index = 0; index < viewModel.SlotCount; index++)
                Assert.AreEqual(InventoryCellMark.None, viewModel.GetSlot(index).Mark.Value,
                    message + " : 格子 " + index);
        }
    }
}
