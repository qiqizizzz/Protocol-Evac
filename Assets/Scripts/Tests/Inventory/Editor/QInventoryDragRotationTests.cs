/*
* ┌──────────────────────────────────┐
* │  描    述: 拖拽中旋转测试, 覆盖拖拽朝向状态机、落点朝向回退与转移契约里的首选朝向
* │  类    名: QInventoryDragRotationTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Defines;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;

namespace Tests.Inventory
{
    /// <summary>
    /// 拖拽中旋转测试：BeginDragAt 记录当前朝向、RotateDraggedItem 只在拖拽态推进本面板状态,
    /// 落点可行性与自动回退仍由规则层负责, 装备槽与不可旋转配置一律拒绝
    /// </summary>
    public class QInventoryDragRotationTests
    {
        private const int GRID_WIDTH = 4;
        private const int NO_DRAG = -1;
        private const int MAX_STACK = 60;
        private const int PRIMARY_WEAPON_SLOT = InventoryContainerIds.PRIMARY_WEAPON_SLOT;

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private int m_containerId;

        private readonly List<InventoryViewModel> m_viewModels = new List<InventoryViewModel>();

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();

            m_containerId = m_kit.NewContainerId();
            m_model.RegisterContainer(m_containerId, new InventoryContainerSpec(GRID_WIDTH, GRID_WIDTH, float.MaxValue));

            m_viewModels.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = m_viewModels.Count - 1; i >= 0; i--)
            {
                InventoryViewModel viewModel = m_viewModels[i];
                viewModel.OnUnbind();
                viewModel.OnDispose();
                viewModel.Dispose();
            }
            m_viewModels.Clear();

            m_model.OnUnregister();
            m_kit.Dispose();
        }

        #region 拖拽朝向状态
        [Test]
        public void BeginDragAt_RecordsCurrentRotation()
        {
            int instanceId = AddItem(2, 1, Vector2Int.zero);
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryRotateItem(m_containerId, instanceId));

            InventoryViewModel carrier = BindCarrier(m_containerId);
            carrier.BeginDragAt(Vector2Int.zero);

            Assert.AreEqual(instanceId, carrier.DraggingInstanceId);
            Assert.AreEqual(InventoryRotation.Rotate90, carrier.DragRotation, "拖起的初始朝向要跟随物品本体");
        }

        [Test]
        public void BeginDragAt_OnEmptyCell_KeepsNoDrag()
        {
            AddItem(2, 1, Vector2Int.zero);
            InventoryViewModel carrier = BindCarrier(m_containerId);

            carrier.BeginDragAt(new Vector2Int(2, 2));

            Assert.AreEqual(NO_DRAG, carrier.DraggingInstanceId);
            Assert.IsFalse(carrier.IsDragging);
            Assert.IsFalse(carrier.RotateDraggedItem());
        }

        [Test]
        public void RotateDraggedItem_WithoutDrag_ReturnsFalse()
        {
            InventoryViewModel carrier = BindCarrier(m_containerId);

            Assert.IsFalse(carrier.RotateDraggedItem());
            Assert.AreEqual(InventoryRotation.None, carrier.DragRotation);
        }

        [Test]
        public void RotateDraggedItem_CyclesFourOrientations()
        {
            AddItem(1, 1, Vector2Int.zero);
            InventoryViewModel carrier = BindCarrier(m_containerId);
            carrier.BeginDragAt(Vector2Int.zero);

            Assert.IsTrue(carrier.RotateDraggedItem());
            Assert.AreEqual(InventoryRotation.Rotate90, carrier.DragRotation);
            Assert.IsTrue(carrier.RotateDraggedItem());
            Assert.AreEqual(InventoryRotation.Rotate180, carrier.DragRotation);
            Assert.IsTrue(carrier.RotateDraggedItem());
            Assert.AreEqual(InventoryRotation.Rotate270, carrier.DragRotation);
            Assert.IsTrue(carrier.RotateDraggedItem());
            Assert.AreEqual(InventoryRotation.None, carrier.DragRotation, "转满一周回到原朝向");
        }

        [Test]
        public void RotateDraggedItem_LeavesPlacedItemAlone()
        {
            int instanceId = AddItem(2, 1, Vector2Int.zero);
            InventoryViewModel carrier = BindCarrier(m_containerId);
            carrier.BeginDragAt(Vector2Int.zero);

            carrier.RotateDraggedItem();

            Assert.AreEqual(InventoryRotation.None, ItemOf(instanceId).Rotation, "没落子之前物品本体不该转");
        }

        [Test]
        public void RotateDraggedItem_NonRotatableConfig_ReturnsFalse()
        {
            int itemId = m_kit.CreateItem(2, 1, false, MAX_STACK).ItemId;
            int instanceId = AddStack(itemId, MAX_STACK, Vector2Int.zero);
            InventoryViewModel carrier = BindCarrier(m_containerId);
            carrier.BeginDragAt(Vector2Int.zero);

            Assert.IsFalse(carrier.RotateDraggedItem());
            Assert.AreEqual(InventoryRotation.None, carrier.DragRotation);
        }

        [Test]
        public void RotateDraggedItem_InEquipSlot_ReturnsFalse()
        {
            int itemId = m_kit.CreateItem(1, 1, true, MAX_STACK, 0f, 0, InventoryItemRarity.None, null,
                InventoryEquipSlot.PrimaryWeapon).ItemId;
            AddStack(itemId, 10, Vector2Int.zero, PRIMARY_WEAPON_SLOT);

            InventoryViewModel carrier = BindCarrier(PRIMARY_WEAPON_SLOT);
            carrier.BeginDragAt(Vector2Int.zero);
            Assert.IsTrue(carrier.IsDragging);

            Assert.IsFalse(carrier.RotateDraggedItem(), "装备槽按单格收纳整件装备, 朝向没有意义");
            Assert.AreEqual(InventoryRotation.None, carrier.DragRotation);
        }

        [Test]
        public void CancelDrag_ResetsRotation()
        {
            AddItem(2, 1, Vector2Int.zero);
            InventoryViewModel carrier = BindCarrier(m_containerId);
            carrier.BeginDragAt(Vector2Int.zero);
            Assert.IsTrue(carrier.RotateDraggedItem());

            carrier.CancelDrag();

            Assert.IsFalse(carrier.IsDragging);
            Assert.AreEqual(InventoryRotation.None, carrier.DragRotation);
        }
        #endregion

        #region 落点朝向
        [Test]
        public void PlaceDraggedItemAt_RotatedDrag_FitsVerticalGap()
        {
            int instanceId = AddItem(2, 1, Vector2Int.zero);
            InventoryViewModel carrier = BindCarrier(m_containerId);
            carrier.BeginDragAt(Vector2Int.zero);
            Assert.IsTrue(carrier.RotateDraggedItem());

            carrier.PlaceDraggedItemAt(new Vector2Int(3, 0));

            InventoryItemVO placed = ItemOf(instanceId);
            Assert.AreEqual(new Vector2Int(3, 0), placed.Origin);
            Assert.AreEqual(InventoryRotation.Rotate90, placed.Rotation);
            Assert.AreEqual(InventoryMoveResult.Success, carrier.LastMoveResult);
        }

        [Test]
        public void PlaceDraggedItemAt_StaleRotation_AutoRotatesToFit()
        {
            int instanceId = AddItem(2, 1, Vector2Int.zero);
            InventoryViewModel carrier = BindCarrier(m_containerId);
            carrier.BeginDragAt(Vector2Int.zero);

            carrier.PlaceDraggedItemAt(new Vector2Int(3, 0));

            InventoryItemVO placed = ItemOf(instanceId);
            Assert.AreEqual(InventoryRotation.Rotate90, placed.Rotation, "横着放不下时规则层要自己换朝向");
            Assert.AreEqual(new Vector2Int(3, 0), placed.Origin);
        }

        [Test]
        public void PlaceDraggedItemAt_KeepsRotationWhenBothFit()
        {
            int instanceId = AddItem(2, 1, Vector2Int.zero);
            InventoryViewModel carrier = BindCarrier(m_containerId);
            carrier.BeginDragAt(Vector2Int.zero);

            carrier.PlaceDraggedItemAt(new Vector2Int(0, 2));

            Assert.AreEqual(InventoryRotation.None, ItemOf(instanceId).Rotation, "横竖都放得下时保持原样");
        }

        [Test]
        public void PlaceDraggedItemAt_Rejected_KeepsItemInPlace()
        {
            int instanceId = AddItem(2, 1, Vector2Int.zero);
            InventoryViewModel carrier = BindCarrier(m_containerId);
            carrier.BeginDragAt(Vector2Int.zero);
            Assert.IsTrue(carrier.RotateDraggedItem());

            carrier.PlaceDraggedItemAt(new Vector2Int(3, 3));

            Assert.AreEqual(InventoryMoveResult.OutOfBounds, carrier.LastMoveResult);
            Assert.IsFalse(carrier.IsDragging);
            Assert.AreEqual(InventoryRotation.None, carrier.DragRotation);
            Assert.AreEqual(Vector2Int.zero, ItemOf(instanceId).Origin);
            Assert.AreEqual(InventoryRotation.None, ItemOf(instanceId).Rotation);
        }

        [Test]
        public void PreviewDropAt_AfterDragRotation_MarksRotatedFootprint()
        {
            AddItem(2, 1, Vector2Int.zero);
            InventoryViewModel carrier = BindCarrier(m_containerId);
            carrier.BeginDragAt(Vector2Int.zero);
            Assert.IsTrue(carrier.RotateDraggedItem());

            carrier.PreviewDropAt(new Vector2Int(3, 0));

            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(carrier, 3, 0));
            Assert.AreEqual(InventoryCellMark.DropValid, MarkAt(carrier, 3, 1), "预览要按竖起来的足迹涂");
            Assert.AreEqual(InventoryCellMark.None, MarkAt(carrier, 2, 0));
            Assert.AreEqual(InventoryCellMark.None, MarkAt(carrier, 0, 0), "拖起来的原位不该留色");
        }
        #endregion

        #region 转移朝向契约
        [Test]
        public void TryTransferItem_PreferredRotation_AppliesFacing()
        {
            int instanceId = AddItem(2, 1, Vector2Int.zero);

            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryTransferItem(m_containerId, m_containerId,
                instanceId, new Vector2Int(0, 2), InventoryRotation.Rotate90));

            InventoryItemVO placed = ItemOf(instanceId);
            Assert.AreEqual(new Vector2Int(0, 2), placed.Origin);
            Assert.AreEqual(InventoryRotation.Rotate90, placed.Rotation);
            Assert.AreEqual(new Vector2Int(1, 2), placed.Size);
        }

        [Test]
        public void TryTransferItem_PreferredBlocked_FallsBackToOtherFacing()
        {
            int instanceId = AddItem(2, 1, Vector2Int.zero);
            AddItem(1, 1, new Vector2Int(2, 3));

            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryTransferItem(m_containerId, m_containerId,
                instanceId, new Vector2Int(2, 2), InventoryRotation.Rotate90));

            InventoryItemVO placed = ItemOf(instanceId);
            Assert.AreEqual(new Vector2Int(2, 2), placed.Origin);
            Assert.AreEqual(InventoryRotation.Rotate180, placed.Rotation, "竖着被挡住时按推进顺序回退到横放");
        }

        [Test]
        public void TryTransferItem_NonRotatableWithPreferred_ReportsNotRotatable()
        {
            int itemId = m_kit.CreateItem(2, 1, false).ItemId;
            int instanceId = AddItem(itemId, Vector2Int.zero);

            Assert.AreEqual(InventoryMoveResult.NotRotatable, m_model.TryTransferItem(m_containerId, m_containerId,
                instanceId, new Vector2Int(3, 0), InventoryRotation.Rotate90));

            Assert.AreEqual(Vector2Int.zero, ItemOf(instanceId).Origin);
        }

        [Test]
        public void PeekTransferItem_WithPreferredRotation_IsPureQuery()
        {
            int instanceId = AddItem(2, 1, Vector2Int.zero);
            int version = m_model.GetContainerVO(m_containerId).Version;

            InventoryMoveResult verdict = m_model.PeekTransferItem(m_containerId, m_containerId, instanceId,
                new Vector2Int(3, 0), InventoryRotation.Rotate90, out bool willSwap, out bool willMerge,
                out InventoryRotation rotation);

            Assert.AreEqual(InventoryMoveResult.Success, verdict);
            Assert.AreEqual(InventoryRotation.Rotate90, rotation);
            Assert.IsFalse(willSwap);
            Assert.IsFalse(willMerge);
            Assert.AreEqual(version, m_model.GetContainerVO(m_containerId).Version, "预判不该提交任何版本");
            Assert.AreEqual(Vector2Int.zero, ItemOf(instanceId).Origin);
        }
        #endregion

        // ==================== Private Function ====================
        // 按真实流程绑定一个载体视图模型, 收尾交给 TearDown
        private InventoryViewModel BindCarrier(int containerId)
        {
            InventoryViewModel viewModel = new InventoryViewModel(m_model, containerId);
            viewModel.OnBind();
            m_viewModels.Add(viewModel);
            return viewModel;
        }

        // 造一件可旋转的普通物品塞进容器, 回实例号
        private int AddItem(int width, int height, Vector2Int cell)
            => AddItem(m_kit.CreateItem(width, height, true, MAX_STACK).ItemId, cell);

        // 用指定物品号塞一份物品
        private int AddItem(int itemId, Vector2Int cell, int containerId)
        {
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(containerId, itemId, 1, cell));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(containerId, cell, out int instanceId), "放完查不到实例");
            return instanceId;
        }

        // 用指定物品号塞一份物品到默认容器
        private int AddItem(int itemId, Vector2Int cell) => AddItem(itemId, cell, m_containerId);

        // 往指定容器塞一堆同类物品
        private int AddStack(int itemId, int amount, Vector2Int cell, int containerId)
        {
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(containerId, itemId, amount, cell));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(containerId, cell, out int instanceId), "放完查不到实例");
            return instanceId;
        }

        private int AddStack(int itemId, int amount, Vector2Int cell) => AddStack(itemId, amount, cell, m_containerId);

        // 从模型取指定实例
        private InventoryItemVO ItemOf(int instanceId)
        {
            InventoryContainerVO container = m_model.GetContainerVO(m_containerId);
            Assert.IsTrue(container.TryGetItem(instanceId, out InventoryItemVO item), "实例 " + instanceId + " 不在容器");
            return item;
        }

        // 读某一格的着色语义, 网格统一按 4 列排
        private static InventoryCellMark MarkAt(InventoryViewModel viewModel, int x, int y)
            => viewModel.GetSlot(y * GRID_WIDTH + x).Mark.Value;
    }
}
