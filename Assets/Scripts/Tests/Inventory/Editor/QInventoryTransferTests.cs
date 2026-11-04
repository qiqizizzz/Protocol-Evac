/*
* ┌──────────────────────────────────┐
* │  描    述: 背包转移与交换测试, 覆盖同容器换位、双方足迹重叠、跨容器交换、超重与拖拽链路
* │  类    名: QInventoryTransferTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Rule.Extensions;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// TryTransferItem 与拖拽链路测试：模型层的转移/交换全部分支、单载体的拖起落点、战备面板的跨载体路由
    /// </summary>
    public class QInventoryTransferTests
    {
        private const int NO_DRAG = -1;

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

            // 两张 4x4 且不限重的容器, 需要别的几何或限重时在用例里另开容器号
            m_sourceId = m_kit.NewContainerId();
            m_targetId = m_kit.NewContainerId();
            Register(m_sourceId, 4, 4);
            Register(m_targetId, 4, 4);
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

        #region 同容器转移与交换
        [Test]
        public void Transfer_SameContainer_EmptyCell_MovesItemAndKeepsRotation()
        {
            int moving = AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));
            int version = m_model.GetContainerVO(m_sourceId).Version;

            // 先转成竖条, 转移不该把朝向洗回默认姿态
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryRotateItem(m_sourceId, moving));
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(m_sourceId, m_sourceId, moving, new Vector2Int(1, 2)));

            InventoryItemVO item = ItemOf(m_sourceId, moving);
            Assert.AreEqual(new Vector2Int(1, 2), item.Origin);
            Assert.AreEqual(InventoryRotation.Rotate90, item.Rotation);
            Assert.AreEqual(new Vector2Int(1, 2), item.Size);
            Assert.AreEqual(version + 2, m_model.GetContainerVO(m_sourceId).Version, "旋转与转移各提交一次");
        }

        [Test]
        public void Transfer_SameContainer_OccupiedCell_SwapsBothOrigins()
        {
            int moving = AddItem(m_sourceId, 2, 2, new Vector2Int(0, 0));
            int occupant = AddItem(m_sourceId, 2, 2, new Vector2Int(2, 2));

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(m_sourceId, m_sourceId, moving, new Vector2Int(2, 2)));

            Assert.AreEqual(new Vector2Int(2, 2), ItemOf(m_sourceId, moving).Origin);
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_sourceId, occupant).Origin);
            Assert.AreEqual(InventoryRotation.None, ItemOf(m_sourceId, occupant).Rotation, "被顶回的一方保持自身朝向");
        }

        [Test]
        public void Transfer_SameContainer_NewFootprintsOverlap_ReturnsOccupied()
        {
            // 5x3 的扁容器, 专门造「 mover 的新足迹压住 occupant 被顶回去的新足迹」
            int wideId = m_kit.NewContainerId();
            Register(wideId, 5, 3);

            int moving = AddItem(wideId, 2, 2, new Vector2Int(0, 0));
            int occupant = AddItem(wideId, 3, 1, new Vector2Int(2, 0));
            int version = m_model.GetContainerVO(wideId).Version;

            // 方块落到 (2,0) 后, 横条顶回 (0,0) 会横跨到 (2,0), 与方块的新足迹撞在一起
            Assert.AreEqual(InventoryMoveResult.Occupied,
                m_model.TryTransferItem(wideId, wideId, moving, new Vector2Int(2, 0)));

            // 被拒的交换必须一点没动, 否则网格里会留下双重占位
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(wideId, moving).Origin);
            Assert.AreEqual(new Vector2Int(2, 0), ItemOf(wideId, occupant).Origin);
            Assert.AreEqual(version, m_model.GetContainerVO(wideId).Version, "失败不该提交版本");
            Assert.IsTrue(m_model.TryGetInstanceIdAt(wideId, new Vector2Int(2, 0), out int hit));
            Assert.AreEqual(occupant, hit, "(2,0) 仍然只属于横条");
        }

        [Test]
        public void Transfer_SameContainer_OccupantPushedOutOfBounds_ReturnsOutOfBounds()
        {
            int moving = AddItem(m_sourceId, 1, 1, new Vector2Int(3, 3));
            int occupant = AddItem(m_sourceId, 3, 1, new Vector2Int(0, 0));

            // 方块落到 (0,0) 本身没问题, 但横条顶回 (3,3) 会伸出右边界
            Assert.AreEqual(InventoryMoveResult.OutOfBounds,
                m_model.TryTransferItem(m_sourceId, m_sourceId, moving, new Vector2Int(0, 0)));

            Assert.AreEqual(new Vector2Int(3, 3), ItemOf(m_sourceId, moving).Origin);
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_sourceId, occupant).Origin);
        }

        [Test]
        public void Transfer_SameContainer_OntoOwnOrigin_ReturnsSameSpot()
        {
            int moving = AddItem(m_sourceId, 2, 1, new Vector2Int(1, 1));
            int version = m_model.GetContainerVO(m_sourceId).Version;

            Assert.AreEqual(InventoryMoveResult.SameSpot,
                m_model.TryTransferItem(m_sourceId, m_sourceId, moving, new Vector2Int(1, 1)));
            Assert.AreEqual(version, m_model.GetContainerVO(m_sourceId).Version);
        }

        [Test]
        public void Transfer_SameContainer_SmallItemIntoBigItemFootprint_SwapsWhenBothFit()
        {
            // 8x5 大背包复刻 Play 手测布局: 步枪 5x2 在左上, 护甲 3x3 在右上, 子弹 1x1 在 (3,3)
            int wideId = m_kit.NewContainerId();
            Register(wideId, 8, 5);
            int rifle = AddItem(wideId, 5, 2, new Vector2Int(0, 0));
            AddItem(wideId, 3, 3, new Vector2Int(5, 0));
            int bullet = AddItem(wideId, 1, 1, new Vector2Int(3, 3));

            // 悬停预判: 子弹落 (0,0) 顶起步枪, 应报可交换
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.PeekTransferItem(wideId, wideId, bullet, new Vector2Int(0, 0), out bool willSwap));
            Assert.IsTrue(willSwap, "落点压住第三方物品, 预判就该报交换");

            // 真落地走同一条 ResolveTransfer 规则, 结论必须和预判一致
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(wideId, wideId, bullet, new Vector2Int(0, 0)));

            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(wideId, bullet).Origin);
            Assert.AreEqual(new Vector2Int(3, 3), ItemOf(wideId, rifle).Origin);
            Assert.IsTrue(m_model.TryGetInstanceIdAt(wideId, new Vector2Int(7, 4), out int rifleTail));
            Assert.AreEqual(rifle, rifleTail, "步枪顶回后 5x2 的尾巴应盖到 (7,4)");
        }

        [Test]
        public void Transfer_InstanceIdNotInSource_ReturnsSourceNotFound()
        {
            int elsewhere = AddItem(m_targetId, 1, 1, new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.SourceNotFound,
                m_model.TryTransferItem(m_sourceId, m_targetId, elsewhere, new Vector2Int(1, 1)));
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_targetId, elsewhere).Origin);
        }
        #endregion

        #region 跨容器转移与交换
        [Test]
        public void Transfer_CrossContainer_SameCoordinateOnOtherGrid_Succeeds()
        {
            int moving = AddItem(m_sourceId, 2, 2, new Vector2Int(0, 0));

            // 目标容器的 (0,0) 空着, 两张网格互不相干, 坐标相同不算冲突
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(m_sourceId, m_targetId, moving, new Vector2Int(0, 0)));

            AssertAbsent(m_sourceId, moving);
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_targetId, moving).Origin);
        }

        [Test]
        public void Transfer_CrossContainer_OccupiedCell_SwapsBetweenGrids()
        {
            int moving = AddItem(m_sourceId, 2, 2, new Vector2Int(0, 0));
            int occupant = AddItem(m_targetId, 2, 2, new Vector2Int(0, 0));

            // 两件物品坐标完全重叠但分属两张网格, 跨容器交换必须放行
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(m_sourceId, m_targetId, moving, new Vector2Int(0, 0)));

            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_targetId, moving).Origin);
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_sourceId, occupant).Origin, "横竖各回对方原来的坐标");
        }

        [Test]
        public void Transfer_CrossContainer_RotatedItemKeepsRotation()
        {
            int moving = AddItem(m_sourceId, 1, 3, new Vector2Int(0, 0));
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryRotateItem(m_sourceId, moving));

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(m_sourceId, m_targetId, moving, new Vector2Int(0, 0)));

            InventoryItemVO item = ItemOf(m_targetId, moving);
            Assert.AreEqual(InventoryRotation.Rotate90, item.Rotation);
            Assert.AreEqual(new Vector2Int(3, 1), item.Size);
        }

        [Test]
        public void Transfer_CrossContainer_TargetOverWeightLimit_ReturnsWeightExceeded()
        {
            int moving = AddItem(m_sourceId, 1, 1, new Vector2Int(0, 0), 5f);
            int tightId = m_kit.NewContainerId();
            Register(tightId, 4, 4, 1f);

            Assert.AreEqual(InventoryMoveResult.WeightExceeded,
                m_model.TryTransferItem(m_sourceId, tightId, moving, new Vector2Int(0, 0)));

            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_sourceId, moving).Origin);
        }

        [Test]
        public void Transfer_CrossContainer_TargetTooNarrow_ReturnsOutOfBounds()
        {
            int moving = AddItem(m_sourceId, 3, 1, new Vector2Int(0, 0));
            int smallId = m_kit.NewContainerId();
            Register(smallId, 4, 4);

            Assert.AreEqual(InventoryMoveResult.OutOfBounds,
                m_model.TryTransferItem(m_sourceId, smallId, moving, new Vector2Int(2, 0)));
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_sourceId, moving).Origin);
        }

        [Test]
        public void Transfer_UnregisteredSourceContainer_LogsErrorAndReturnsNone()
        {
            int missingId = m_kit.NewContainerId();
            int moving = AddItem(m_targetId, 1, 1, new Vector2Int(0, 0));

            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));
            Assert.AreEqual(InventoryMoveResult.None,
                m_model.TryTransferItem(missingId, m_targetId, moving, new Vector2Int(1, 1)));
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_targetId, moving).Origin);
        }

        [Test]
        public void Transfer_UnregisteredTargetContainer_LogsErrorAndReturnsNone()
        {
            int missingId = m_kit.NewContainerId();
            int moving = AddItem(m_sourceId, 1, 1, new Vector2Int(0, 0));

            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));
            Assert.AreEqual(InventoryMoveResult.None,
                m_model.TryTransferItem(m_sourceId, missingId, moving, new Vector2Int(1, 1)));
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_sourceId, moving).Origin);
        }
        #endregion

        #region 单载体拖拽
        [Test]
        public void BeginDragAt_OnEmptyCell_StartsNoDrag()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);

            viewModel.BeginDragAt(new Vector2Int(3, 3));

            Assert.AreEqual(NO_DRAG, viewModel.DraggingInstanceId);
        }

        [Test]
        public void PlaceDraggedItemAt_OnEmptyCell_MovesItemAndHintsPlaced()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            int moving = AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));

            viewModel.BeginDragAt(new Vector2Int(0, 0));
            Assert.AreEqual(moving, viewModel.DraggingInstanceId);

            viewModel.PlaceDraggedItemAt(new Vector2Int(1, 2));

            Assert.AreEqual(InventoryMoveResult.Success, viewModel.LastMoveResult);
            Assert.AreEqual(InventoryHintKind.Placed, viewModel.HintKind.Value);
            Assert.AreEqual(NO_DRAG, viewModel.DraggingInstanceId);
            Assert.IsTrue(viewModel.Snapshot.Value.TryGetItem(moving, out InventoryItemVO view));
            Assert.AreEqual(new Vector2Int(1, 2), view.Origin);
        }

        [Test]
        public void PlaceDraggedItemAt_OnAnotherItem_SwapsBothInSameContainer()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            int moving = AddItem(m_sourceId, 2, 2, new Vector2Int(0, 0));
            int occupant = AddItem(m_sourceId, 2, 2, new Vector2Int(2, 2));

            viewModel.BeginDragAt(new Vector2Int(0, 0));
            viewModel.PlaceDraggedItemAt(new Vector2Int(2, 2));

            Assert.AreEqual(InventoryMoveResult.Success, viewModel.LastMoveResult);
            Assert.AreEqual(new Vector2Int(2, 2), ItemOf(m_sourceId, moving).Origin);
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_sourceId, occupant).Origin);
        }

        [Test]
        public void PlaceDraggedItemAt_OntoBiggerItem_PreviewsSwapThenCommits()
        {
            // 整条 VM 拖拽链钉死不对称交换: 起拖 -> 悬停亮交换色 -> 落地换位
            int wideId = m_kit.NewContainerId();
            Register(wideId, 8, 5);
            InventoryViewModel viewModel = BindCarrier(wideId);
            int rifle = AddItem(wideId, 5, 2, new Vector2Int(0, 0));
            AddItem(wideId, 3, 3, new Vector2Int(5, 0));
            int bullet = AddItem(wideId, 1, 1, new Vector2Int(3, 3));

            viewModel.BeginDragAt(new Vector2Int(3, 3));
            Assert.AreEqual(bullet, viewModel.DraggingInstanceId);

            viewModel.PreviewDropAt(new Vector2Int(0, 0));
            Assert.AreEqual(InventoryCellMark.DropSwap, viewModel.GetSlot(0).Mark.Value, "悬停 (0,0) 应亮交换色");

            viewModel.PlaceDraggedItemAt(new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.Success, viewModel.LastMoveResult);
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(wideId, bullet).Origin);
            Assert.AreEqual(new Vector2Int(3, 3), ItemOf(wideId, rifle).Origin);
            Assert.AreEqual(NO_DRAG, viewModel.DraggingInstanceId, "落地成功必须销掉拖拽账");
            Assert.AreEqual(InventoryHintKind.Placed, viewModel.HintKind.Value);
        }

        [Test]
        public void PlaceDraggedItemAt_OntoOwnOrigin_TreatsAsSameSpotAndEndsDrag()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            int moving = AddItem(m_sourceId, 2, 1, new Vector2Int(1, 1));

            viewModel.BeginDragAt(new Vector2Int(1, 1));
            viewModel.PlaceDraggedItemAt(new Vector2Int(1, 1));

            Assert.AreEqual(InventoryMoveResult.SameSpot, viewModel.LastMoveResult);
            Assert.IsTrue(viewModel.LastMoveResult.IsSuccessOrSame());
            Assert.AreEqual(InventoryHintKind.Placed, viewModel.HintKind.Value);
            Assert.AreEqual(NO_DRAG, viewModel.DraggingInstanceId);
            Assert.AreEqual(new Vector2Int(1, 1), ItemOf(m_sourceId, moving).Origin);
        }

        [Test]
        public void CancelDrag_WithoutDropping_LeavesItemInPlace()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            int moving = AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));
            int version = m_model.GetContainerVO(m_sourceId).Version;

            viewModel.BeginDragAt(new Vector2Int(0, 0));
            viewModel.CancelDrag();

            Assert.AreEqual(NO_DRAG, viewModel.DraggingInstanceId);
            Assert.AreEqual(version, m_model.GetContainerVO(m_sourceId).Version);
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_sourceId, moving).Origin);
        }

        [Test]
        public void PlaceDraggedItemAt_WithoutDraggingAnything_ChangesNothing()
        {
            InventoryViewModel viewModel = BindCarrier(m_sourceId);
            int other = AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));
            int version = m_model.GetContainerVO(m_sourceId).Version;

            // 独立载体没有上层面板, 别人拖过来的东西落到这里只能被丢弃
            viewModel.PlaceDraggedItemAt(new Vector2Int(1, 2));

            Assert.AreEqual(version, m_model.GetContainerVO(m_sourceId).Version);
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_sourceId, other).Origin);
            Assert.AreEqual(InventoryHintKind.Idle, viewModel.HintKind.Value);
        }
        #endregion

        #region 战备面板跨载体
        [Test]
        public void Panel_DropFromAnotherCarrier_MovesItemAcross()
        {
            EquipmentPanelViewModel panel = BuildPanel(m_sourceId, m_targetId);
            InventoryViewModel left = panel.GetContainer(0);
            InventoryViewModel right = panel.GetContainer(1);
            int moving = AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));

            left.BeginDragAt(new Vector2Int(0, 0));
            right.PlaceDraggedItemAt(new Vector2Int(1, 2));

            AssertAbsent(m_sourceId, moving);
            Assert.AreEqual(new Vector2Int(1, 2), ItemOf(m_targetId, moving).Origin);
            Assert.AreEqual(NO_DRAG, left.DraggingInstanceId, "来源面板的拖拽账要跟着销掉");
            Assert.AreEqual(InventoryHintKind.Placed, left.HintKind.Value, "落点结果回灌给发起方");
        }

        [Test]
        public void Panel_DropOntoOccupiedCell_SwapsBetweenCarriers()
        {
            EquipmentPanelViewModel panel = BuildPanel(m_sourceId, m_targetId);
            InventoryViewModel left = panel.GetContainer(0);
            InventoryViewModel right = panel.GetContainer(1);
            int moving = AddItem(m_sourceId, 2, 2, new Vector2Int(0, 0));
            int occupant = AddItem(m_targetId, 2, 2, new Vector2Int(1, 1));

            left.BeginDragAt(new Vector2Int(0, 0));
            right.PlaceDraggedItemAt(new Vector2Int(1, 1));

            Assert.AreEqual(new Vector2Int(1, 1), ItemOf(m_targetId, moving).Origin);
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_sourceId, occupant).Origin);
            Assert.AreEqual(NO_DRAG, left.DraggingInstanceId);
        }

        [Test]
        public void Panel_DropAfterCancelDrag_IsIgnored()
        {
            EquipmentPanelViewModel panel = BuildPanel(m_sourceId, m_targetId);
            InventoryViewModel left = panel.GetContainer(0);
            InventoryViewModel right = panel.GetContainer(1);
            int moving = AddItem(m_sourceId, 2, 1, new Vector2Int(0, 0));
            int sourceVersion = m_model.GetContainerVO(m_sourceId).Version;
            int targetVersion = m_model.GetContainerVO(m_targetId).Version;

            left.BeginDragAt(new Vector2Int(0, 0));
            left.CancelDrag();
            right.PlaceDraggedItemAt(new Vector2Int(1, 2));

            Assert.AreEqual(sourceVersion, m_model.GetContainerVO(m_sourceId).Version);
            Assert.AreEqual(targetVersion, m_model.GetContainerVO(m_targetId).Version, "销过账的拖拽不该再落进别的容器");
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_sourceId, moving).Origin);
            Assert.AreEqual(InventoryHintKind.Idle, right.HintKind.Value);
        }

        [Test]
        public void Panel_DragEndedByAnotherCarrier_KeepsTheDragSource()
        {
            EquipmentPanelViewModel panel = BuildPanel(m_sourceId, m_targetId);
            InventoryViewModel left = panel.GetContainer(0);
            InventoryViewModel right = panel.GetContainer(1);
            int moving = AddItem(m_sourceId, 1, 1, new Vector2Int(2, 2));

            left.BeginDragAt(new Vector2Int(2, 2));
            // 别的载体来销账, 不能把 left 的拖拽记录抹掉
            panel.NotifyDragEnded(right);

            right.PlaceDraggedItemAt(new Vector2Int(0, 0));

            AssertAbsent(m_sourceId, moving);
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_targetId, moving).Origin);
        }
        #endregion

        // ==================== Private Function ====================
        // 注册一张容器, 默认不限重且满格矩形
        private void Register(int containerId, int width, int height, float weightLimit = float.MaxValue)
            => m_model.RegisterContainer(containerId, new InventoryContainerSpec(width, height, weightLimit));

        // 按真实流程绑定一个载体视图模型, 收尾交给 TearDown
        private InventoryViewModel BindCarrier(int containerId)
        {
            InventoryViewModel viewModel = new InventoryViewModel(m_model, containerId);
            viewModel.OnBind();
            m_viewModels.Add(viewModel);
            return viewModel;
        }

        // 组一面只挂两个载体的战备面板, 子载体按 View 作用域的等价流程各自绑定
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
        private int AddItem(int containerId, int width, int height, Vector2Int cell, float unitWeight = 0f)
        {
            int itemId = m_kit.CreateItem(width, height, true, 1, unitWeight).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(containerId, itemId, 1, cell));

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

        // 断言实例已经离开该容器
        private void AssertAbsent(int containerId, int instanceId)
            => Assert.IsFalse(m_model.GetContainerVO(containerId).Contains(instanceId),
                "实例 " + instanceId + " 还赖在容器 " + containerId);
    }
}