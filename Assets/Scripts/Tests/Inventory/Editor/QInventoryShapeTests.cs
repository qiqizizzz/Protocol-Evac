/*
* ┌──────────────────────────────────┐
* │  描    述: 异形容器测试, 覆盖形状行编解码、实心掩码与格子实心联动
* │  类    名: QInventoryShapeTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Grid;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UI.Inventory;
using UI.Inventory.Editor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// 异形容器测试：形状行文本与掩码互转、网格实心判定、快照容量口径、格子 IsSolid 响应式联动
    /// </summary>
    public class QInventoryShapeTests
    {
        // 带一个洞的 4x3 测试网格, 洞在 (1,1), 实心 11 格, 外接 12 格
        private const int GRID_WIDTH = 4;
        private const int GRID_HEIGHT = 3;
        private const int HOLE_INDEX = 5;
        private static readonly string[] NOTCHED_ROWS = { "####", "#.##", "####" };
        private static readonly Vector2Int HOLE = new Vector2Int(1, 1);

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
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();

            m_containerId = m_kit.NewContainerId();
            m_model.RegisterContainer(m_containerId,
                new InventoryContainerSpec(GRID_WIDTH, GRID_HEIGHT, float.MaxValue, ShapeMask()));

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

        #region 形状行解析
        [Test]
        public void ParseShape_EmptyRows_ExpandsToFullRectangle()
        {
            bool[] byNull = InventoryContainerConfigSO.ParseShape(null, GRID_WIDTH, GRID_HEIGHT);
            bool[] byEmpty = InventoryContainerConfigSO.ParseShape(Array.Empty<string>(), GRID_WIDTH, GRID_HEIGHT);

            Assert.AreEqual(GRID_WIDTH * GRID_HEIGHT, byNull.Length);

            for (int i = 0; i < byNull.Length; i++)
            {
                Assert.IsTrue(byNull[i], "没配形状行就该整片实心: " + i);
                Assert.IsTrue(byEmpty[i]);
            }
        }

        [Test]
        public void ParseShape_HashAndDot_MapRowMajorWithTheFirstRowOnTop()
        {
            bool[] mask = InventoryContainerConfigSO.ParseShape(new[] { "#.#", "###", "###" }, 3, 3);

            Assert.AreEqual(9, mask.Length);
            Assert.IsTrue(mask[0]);
            Assert.IsFalse(mask[1], "第一行中间的点应解析成空洞");
            Assert.IsTrue(mask[2]);

            // 第二行整行实心, 说明 rows[0] 就是最上面一行, 全链路不做上下翻转
            Assert.IsTrue(mask[3]);
            Assert.IsTrue(mask[4]);
            Assert.IsTrue(mask[5]);
        }

        [Test]
        public void ParseShape_NotchedRows_LeavesExactlyOneHole()
        {
            bool[] mask = ShapeMask();

            int solid = 0;
            for (int i = 0; i < mask.Length; i++)
                if (mask[i]) solid++;

            Assert.AreEqual(11, solid);
            Assert.IsFalse(mask[HOLE_INDEX]);
        }

        [Test]
        public void ParseShape_RowCountMismatch_FallsBackToFullRectangle()
        {
            LogAssert.Expect(LogType.Error, new Regex("形状行数"));

            bool[] mask = InventoryContainerConfigSO.ParseShape(new[] { "####", "####" }, GRID_WIDTH, GRID_HEIGHT);

            for (int i = 0; i < mask.Length; i++) Assert.IsTrue(mask[i]);
        }

        [Test]
        public void ParseShape_RowWidthMismatch_FallsBackToFullRectangle()
        {
            LogAssert.Expect(LogType.Error, new Regex("与网格列数"));

            bool[] mask = InventoryContainerConfigSO.ParseShape(new[] { "####", "###", "####" }, GRID_WIDTH, GRID_HEIGHT);

            for (int i = 0; i < mask.Length; i++) Assert.IsTrue(mask[i]);
        }

        [Test]
        public void ParseShape_NullRow_CountsAsWidthMismatch()
        {
            LogAssert.Expect(LogType.Error, new Regex("与网格列数"));

            bool[] mask = InventoryContainerConfigSO.ParseShape(new[] { "####", null, "####" }, GRID_WIDTH, GRID_HEIGHT);

            Assert.IsTrue(mask[0]);
        }

        [Test]
        public void ParseShape_IllegalChar_CountsAsVoidAndLogs()
        {
            LogAssert.Expect(LogType.Error, new Regex("非法字符"));

            bool[] mask = InventoryContainerConfigSO.ParseShape(new[] { "###0", "####", "####" }, GRID_WIDTH, GRID_HEIGHT);

            Assert.IsFalse(mask[3]);
            Assert.IsTrue(mask[4]);
        }
        #endregion

        #region 形状行写回
        [Test]
        public void ToShapeRows_FullRectangle_ReturnsEmptyRows()
        {
            bool[] full = new bool[GRID_WIDTH * GRID_HEIGHT];
            for (int i = 0; i < full.Length; i++) full[i] = true;

            Assert.AreEqual(0, InventoryContainerConfigSO.ToShapeRows(full, GRID_WIDTH, GRID_HEIGHT).Length);
        }

        [Test]
        public void ToShapeRows_NullOrWrongLengthMask_ReturnsEmptyRows()
        {
            Assert.AreEqual(0, InventoryContainerConfigSO.ToShapeRows(null, GRID_WIDTH, GRID_HEIGHT).Length);
            Assert.AreEqual(0, InventoryContainerConfigSO.ToShapeRows(new bool[3], GRID_WIDTH, GRID_HEIGHT).Length);
        }

        [Test]
        public void ToShapeRows_HoleyMask_WritesOneRowPerGridLine()
        {
            Assert.AreEqual(NOTCHED_ROWS, InventoryContainerConfigSO.ToShapeRows(ShapeMask(), GRID_WIDTH, GRID_HEIGHT));
        }

        [Test]
        public void ToShapeRows_RoundTripsBackIntoTheSameMask()
        {
            string[] rows = { "##..", ".##." };
            bool[] parsed = InventoryContainerConfigSO.ParseShape(rows, 4, 2);
            string[] written = InventoryContainerConfigSO.ToShapeRows(parsed, 4, 2);

            Assert.AreEqual(rows, written);
            AssertMask(InventoryContainerConfigSO.ParseShape(written, 4, 2), parsed);
        }

        [Test]
        public void ContainerConfig_Mask_DecodesConfiguredRowsAndKeepsLegacyContainersFull()
        {
            InventoryContainerConfigSO notched = m_kit.CreateContainer(GRID_WIDTH, GRID_HEIGHT, "TEST_NOTCHED", NOTCHED_ROWS);
            InventoryContainerConfigSO legacy = m_kit.CreateContainer(8, 5, "TEST_LEGACY");

            AssertMask(notched.Mask, ShapeMask());
            Assert.AreEqual(40, legacy.Mask.Count);

            for (int i = 0; i < legacy.Mask.Count; i++)
                Assert.IsTrue(legacy.Mask[i], "老容器没写形状行, 必须保持满格");
        }
        #endregion

        #region 网格实心判定
        [Test]
        public void Grid_NullMask_IsFullySolid()
        {
            InventoryGrid grid = new InventoryGrid(GRID_WIDTH, GRID_HEIGHT);

            Assert.AreEqual(GRID_WIDTH * GRID_HEIGHT, grid.SolidCount);

            for (int y = 0; y < GRID_HEIGHT; y++)
            for (int x = 0; x < GRID_WIDTH; x++)
                Assert.IsTrue(grid.IsSolid(new Vector2Int(x, y)));
        }

        [Test]
        public void Grid_IsSolid_RejectsTheHoleEvenThoughItIsInsideTheBoundingBox()
        {
            InventoryGrid grid = new InventoryGrid(GRID_WIDTH, GRID_HEIGHT, ShapeMask());

            Assert.AreEqual(11, grid.SolidCount);
            Assert.IsTrue(grid.IsInside(HOLE));
            Assert.IsFalse(grid.IsSolid(HOLE));
            Assert.IsTrue(grid.IsSolid(Vector2Int.zero));
        }

        [Test]
        public void Grid_IsAreaInside_RejectsFootprintsLeaningOnTheHole()
        {
            InventoryGrid grid = new InventoryGrid(GRID_WIDTH, GRID_HEIGHT, ShapeMask());

            Assert.IsTrue(grid.IsAreaInside(new Vector2Int(0, 1), new Vector2Int(1, 2)));
            Assert.IsTrue(grid.IsAreaInside(new Vector2Int(2, 1), new Vector2Int(2, 1)));
            Assert.IsFalse(grid.IsAreaInside(new Vector2Int(1, 1), new Vector2Int(2, 1)));
            Assert.IsFalse(grid.IsAreaInside(new Vector2Int(1, 0), new Vector2Int(1, 2)));
        }

        [Test]
        public void Grid_CanPlace_RejectsPositionsCoveringTheHole()
        {
            InventoryGrid grid = new InventoryGrid(GRID_WIDTH, GRID_HEIGHT, ShapeMask());
            InventoryItemPlacement placement = new InventoryItemPlacement(m_kit.CreateInstance(m_kit.CreateItem(2, 1)), Vector2Int.zero);

            Assert.IsTrue(grid.CanPlace(placement, new Vector2Int(1, 0), InventoryRotation.None));
            Assert.IsFalse(grid.CanPlace(placement, new Vector2Int(1, 1), InventoryRotation.None));
            Assert.IsFalse(grid.CanPlace(placement, new Vector2Int(0, 1), InventoryRotation.None));
        }

        [Test]
        public void Grid_WrongMaskLength_LogsAndFallsBackToFullRectangle()
        {
            LogAssert.Expect(LogType.Error, new Regex("掩码长度"));

            InventoryGrid grid = new InventoryGrid(GRID_WIDTH, GRID_HEIGHT, new bool[] { true, false });

            Assert.AreEqual(GRID_WIDTH * GRID_HEIGHT, grid.SolidCount);
            Assert.IsTrue(grid.IsSolid(HOLE));
        }

        [Test]
        public void Grid_TryGetPlacementAt_HoleCellNeverHasAnOccupant()
        {
            InventoryGrid grid = new InventoryGrid(GRID_WIDTH, GRID_HEIGHT, ShapeMask());
            InventoryItemPlacement placement = AddToGrid(grid, 2, 1, new Vector2Int(0, 0));

            Assert.IsTrue(grid.TryGetPlacementAt(new Vector2Int(1, 0), out InventoryItemPlacement hit));
            Assert.AreSame(placement, hit);
            Assert.IsFalse(grid.TryGetPlacementAt(HOLE, out _));
        }
        #endregion

        #region 容器与快照链路
        [Test]
        public void Vo_CarriesTheMaskAndCountsOnlySolidCellsAsCapacity()
        {
            InventoryContainerVO vo = m_model.GetContainerVO(m_containerId);

            Assert.AreEqual(12, vo.SlotCount);
            Assert.AreEqual(11, vo.CellCount);
            AssertMask(vo.Mask, ShapeMask());
        }

        [Test]
        public void Vo_WithoutMaskSpec_KeepsBoundingBoxCapacity()
        {
            int plainId = m_kit.NewContainerId();
            m_model.RegisterContainer(plainId, new InventoryContainerSpec(GRID_WIDTH, GRID_HEIGHT, float.MaxValue));

            InventoryContainerVO vo = m_model.GetContainerVO(plainId);

            Assert.AreEqual(vo.SlotCount, vo.CellCount);
            Assert.AreEqual(GRID_WIDTH * GRID_HEIGHT, vo.Mask.Count);

            for (int i = 0; i < vo.Mask.Count; i++) Assert.IsTrue(vo.Mask[i]);
        }

        [Test]
        public void TryAddItem_OnSolidCells_SucceedsAndLeavesTheHoleFree()
        {
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItem(m_containerId, m_kit.CreateItem(2, 1).ItemId, 1, new Vector2Int(2, 1)));

            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, new Vector2Int(2, 1), out _));
            Assert.IsFalse(m_model.TryGetInstanceIdAt(m_containerId, HOLE, out _));
        }

        [Test]
        public void TryAddItem_FootprintCoveringTheHole_IsOutOfBounds()
        {
            int wideId = m_kit.CreateItem(2, 1).ItemId;
            int tallId = m_kit.CreateItem(1, 2).ItemId;

            Assert.AreEqual(InventoryMoveResult.OutOfBounds, m_model.TryAddItem(m_containerId, wideId, 1, HOLE));
            Assert.AreEqual(InventoryMoveResult.OutOfBounds, m_model.TryAddItem(m_containerId, tallId, 1, new Vector2Int(1, 0)));
        }

        [Test]
        public void TryMoveItem_IntoTheHole_IsRejectedAndKeepsTheItemInPlace()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.OutOfBounds,
                m_model.TryMoveItem(m_containerId, instanceId, HOLE, InventoryRotation.None));

            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(instanceId).Origin);
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, new Vector2Int(1, 0), out _));
        }

        [Test]
        public void TryRotateItem_WhenRotatedFootprintHitsTheHole_IsRejected()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(1, 0));

            Assert.AreEqual(InventoryMoveResult.OutOfBounds, m_model.TryRotateItem(m_containerId, instanceId));

            InventoryItemVO item = ItemOf(instanceId);
            Assert.AreEqual(InventoryRotation.None, item.Rotation);
            Assert.AreEqual(new Vector2Int(2, 1), item.Size);
        }
        #endregion

        #region 视图模型联动
        [Test]
        public void Slots_ExposeOneSolidFlagPerBoundingBoxCellInRowMajorOrder()
        {
            bool[] mask = ShapeMask();

            Assert.AreEqual(12, m_viewModel.SlotCount);

            for (int index = 0; index < m_viewModel.SlotCount; index++)
            {
                InventorySlotViewModel slot = m_viewModel.GetSlot(index);
                Assert.AreEqual(new Vector2Int(index % GRID_WIDTH, index / GRID_WIDTH), slot.Cell);
                Assert.AreEqual(mask[index], slot.IsSolid.Value, "格子 " + index + " 实心状态不符");
            }
        }

        [Test]
        public void InteractAt_OnSelectedItem_MarksItsWholeFootprintNextToTheHole()
        {
            AddItem(2, 1, new Vector2Int(2, 1));
            m_viewModel.InteractAt(new Vector2Int(2, 1));

            Assert.AreEqual(InventoryCellMark.Selected, m_viewModel.GetSlot(6).Mark.Value);
            Assert.AreEqual(InventoryCellMark.Selected, m_viewModel.GetSlot(7).Mark.Value);
            Assert.AreEqual(InventoryCellMark.None, m_viewModel.GetSlot(HOLE_INDEX).Mark.Value);
            Assert.IsFalse(m_viewModel.GetSlot(HOLE_INDEX).IsSolid.Value);
        }

        [Test]
        public void InteractAt_OnTheHole_WithItemSelected_ReportsFailedAndKeepsTheItem()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(0, 0));
            m_viewModel.InteractAt(new Vector2Int(0, 0));
            m_hintPulseCount = 0;

            m_viewModel.InteractAt(HOLE);

            Assert.AreEqual(InventoryHintKind.Failed, m_viewModel.HintKind.Value);
            Assert.AreEqual(1, m_hintPulseCount);
            Assert.AreEqual(instanceId, m_viewModel.SelectedInstanceId.Value);
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(instanceId).Origin);
        }

        [Test]
        public void ReRegister_WithAnotherShape_RebuildsSlotsAndSolidFlags()
        {
            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));
            m_model.UnregisterContainer(m_containerId);

            Assert.AreEqual(0, m_viewModel.SlotCount);

            m_model.RegisterContainer(m_containerId, new InventoryContainerSpec(GRID_WIDTH, GRID_HEIGHT, float.MaxValue,
                InventoryContainerConfigSO.ParseShape(new[] { "#..#", "#..#", "#..#" }, GRID_WIDTH, GRID_HEIGHT)));

            InventoryContainerVO vo = m_viewModel.Snapshot.Value;
            Assert.AreEqual(12, vo.SlotCount);
            Assert.AreEqual(6, vo.CellCount);
            Assert.IsTrue(m_viewModel.GetSlot(0).IsSolid.Value);
            Assert.IsFalse(m_viewModel.GetSlot(1).IsSolid.Value);
            Assert.IsTrue(m_viewModel.GetSlot(4).IsSolid.Value);
            Assert.IsFalse(m_viewModel.GetSlot(5).IsSolid.Value);
        }
        #endregion

        #region 绘制板纯逻辑
        [Test]
        public void CreateMask_StartsWithEverythingSolid()
        {
            bool[] mask = InventoryShapeBoard.CreateMask(GRID_WIDTH, GRID_HEIGHT);

            Assert.AreEqual(GRID_WIDTH * GRID_HEIGHT, mask.Length);
            Assert.AreEqual(GRID_WIDTH * GRID_HEIGHT, InventoryShapeBoard.CountSolid(mask));
        }

        [Test]
        public void CountSolid_OnNullMask_ReturnsZero()
        {
            Assert.AreEqual(0, InventoryShapeBoard.CountSolid(null));
        }

        [Test]
        public void FillAndInvert_MaskWholeBoard()
        {
            bool[] mask = InventoryShapeBoard.CreateMask(3, 2);

            InventoryShapeBoard.Fill(mask, false);
            Assert.AreEqual(0, InventoryShapeBoard.CountSolid(mask));

            InventoryShapeBoard.Invert(mask);
            Assert.AreEqual(6, InventoryShapeBoard.CountSolid(mask));

            InventoryShapeBoard.Fill(null, true);
            InventoryShapeBoard.Invert(null);
        }

        [Test]
        public void ResizeMask_KeepsTopLeftOverlapAndMakesNewCellsSolid()
        {
            // 2x2 里 (1,0) 是洞, 扩到 3x3 后洞要跟着左上角走, 新增的格全实心
            bool[] oldMask = { true, false, true, true };

            bool[] mask = InventoryShapeBoard.ResizeMask(oldMask, 2, 3, 3);

            Assert.AreEqual(9, mask.Length);
            Assert.IsTrue(mask[0]);
            Assert.IsFalse(mask[1], "旧洞在 (1,0), 换表后还应在 (1,0)");
            Assert.IsTrue(mask[2], "新增列默认实心");
            Assert.IsTrue(mask[3]);
            Assert.IsTrue(mask[4]);
            Assert.IsTrue(mask[5], "新增列默认实心");
            Assert.AreEqual(8, InventoryShapeBoard.CountSolid(mask));
        }

        [Test]
        public void ResizeMask_ShrinksAndStillFitsTheNewBoundingBox()
        {
            bool[] mask = InventoryShapeBoard.ResizeMask(ShapeMask(), GRID_WIDTH, 2, 2);

            Assert.AreEqual(4, mask.Length);
            // 旧洞在 (1,1), 缩到 2x2 后正好落在最后一格
            Assert.IsFalse(mask[3]);
            Assert.AreEqual(3, InventoryShapeBoard.CountSolid(mask));
        }

        [Test]
        public void ResizeMask_FromNullMask_ReturnsFullRectangle()
        {
            bool[] mask = InventoryShapeBoard.ResizeMask(null, 0, GRID_WIDTH, GRID_HEIGHT);

            Assert.AreEqual(GRID_WIDTH * GRID_HEIGHT, InventoryShapeBoard.CountSolid(mask));
        }

        [Test]
        public void BoardSize_LeavesOneGapAroundEveryCell()
        {
            Vector2 size = InventoryShapeBoard.BoardSize(GRID_WIDTH, GRID_HEIGHT);

            Assert.AreEqual(GRID_WIDTH * (InventoryShapeBoard.CELL_SIZE + InventoryShapeBoard.CELL_GAP)
                + InventoryShapeBoard.CELL_GAP, size.x, 0.001f);
            Assert.AreEqual(GRID_HEIGHT * (InventoryShapeBoard.CELL_SIZE + InventoryShapeBoard.CELL_GAP)
                + InventoryShapeBoard.CELL_GAP, size.y, 0.001f);
        }

        [Test]
        public void HitCellIndex_RoundTripsThroughCellRect()
        {
            Rect board = new Rect(37f, 51f, 0f, 0f);
            board.size = InventoryShapeBoard.BoardSize(GRID_WIDTH, GRID_HEIGHT);

            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    Vector2 center = InventoryShapeBoard.CellRect(board, x, y).center;
                    Assert.AreEqual(y * GRID_WIDTH + x, InventoryShapeBoard.HitCellIndex(board, center, GRID_WIDTH, GRID_HEIGHT),
                        "格子 " + x + "," + y + " 命中下标不符");
                }
            }
        }

        [Test]
        public void HitCellIndex_OutsideBoardOrOnLeadingEdge_ReturnsMinusOne()
        {
            Rect board = new Rect(37f, 51f, 0f, 0f);
            board.size = InventoryShapeBoard.BoardSize(GRID_WIDTH, GRID_HEIGHT);

            Assert.AreEqual(-1, InventoryShapeBoard.HitCellIndex(board, board.min - Vector2.one, GRID_WIDTH, GRID_HEIGHT));
            Assert.AreEqual(-1, InventoryShapeBoard.HitCellIndex(board, board.max + Vector2.one, GRID_WIDTH, GRID_HEIGHT));
            // 左上那一条留白不算格子, 免得擦边点到就误挖一格
            Assert.AreEqual(-1, InventoryShapeBoard.HitCellIndex(board, board.min, GRID_WIDTH, GRID_HEIGHT));
        }

        [Test]
        public void Stroke_CarvesOnDownAndKeepsPaintingUntilMouseUp()
        {
            InventoryShapeBoard board = new InventoryShapeBoard();
            board.Load(null, GRID_WIDTH, GRID_HEIGHT);

            Assert.IsTrue(board.ApplyStroke(HOLE_INDEX, InventoryShapeBoard.StrokePhase.Down));
            Assert.IsFalse(board.Shape[HOLE_INDEX], "按在实心格上应该是往下挖");

            Assert.IsTrue(board.ApplyStroke(9, InventoryShapeBoard.StrokePhase.Drag));
            Assert.IsFalse(board.Shape[9], "拖着刷应该沿用按下时定下的目标值");

            Assert.IsFalse(board.ApplyStroke(10, InventoryShapeBoard.StrokePhase.Up));
            Assert.IsFalse(board.ApplyStroke(10, InventoryShapeBoard.StrokePhase.Drag), "松手之后再拖不该继续改");
            Assert.IsTrue(board.Shape[10]);

            Assert.IsFalse(board.ApplyStroke(-1, InventoryShapeBoard.StrokePhase.Down), "没落在格子上直接忽略");
        }

        [Test]
        public void Stroke_DownOnVoidCellPaintsItSolid()
        {
            InventoryShapeBoard board = new InventoryShapeBoard();
            board.Load(ShapeMask(), GRID_WIDTH, GRID_HEIGHT);

            Assert.IsTrue(board.ApplyStroke(HOLE_INDEX, InventoryShapeBoard.StrokePhase.Down));
            Assert.IsTrue(board.Shape[HOLE_INDEX]);
            Assert.AreEqual(GRID_WIDTH * GRID_HEIGHT, board.SolidCount);
        }

        [Test]
        public void Board_RoundTripsThroughShapeRowsBackToTheSameMask()
        {
            InventoryShapeBoard board = new InventoryShapeBoard();
            board.Load(null, GRID_WIDTH, GRID_HEIGHT);

            board.ApplyStroke(HOLE_INDEX, InventoryShapeBoard.StrokePhase.Down);
            board.ApplyStroke(9, InventoryShapeBoard.StrokePhase.Up);

            string[] rows = InventoryContainerConfigSO.ToShapeRows(board.Shape, GRID_WIDTH, GRID_HEIGHT);
            bool[] parsed = InventoryContainerConfigSO.ParseShape(rows, GRID_WIDTH, GRID_HEIGHT);

            AssertMask(parsed, board.Shape);
            Assert.IsFalse(parsed[HOLE_INDEX]);
        }

        [Test]
        public void Board_ResizeAfterLoadKeepsTheCarvedHole()
        {
            InventoryShapeBoard board = new InventoryShapeBoard();
            board.Load(ShapeMask(), GRID_WIDTH, GRID_HEIGHT);

            board.Resize(5, GRID_HEIGHT);

            Assert.AreEqual(15, board.Shape.Length);
            // 洞跟着 (1,1) 这个坐标走, 行主序下标从 1*4+1 变成 1*5+1
            Assert.IsFalse(board.Shape[6], "加宽之后洞还应该在第二行第二列");
            Assert.IsTrue(board.Shape[HOLE_INDEX], "原来的下标换列宽以后已经是另一格了");
            Assert.AreEqual(14, board.SolidCount);
        }

        [Test]
        public void Board_LoadWithMismatchedMaskLength_FallsBackToFullRectangle()
        {
            InventoryShapeBoard board = new InventoryShapeBoard();
            board.Load(new bool[] { true, false }, GRID_WIDTH, GRID_HEIGHT);

            Assert.AreEqual(GRID_WIDTH * GRID_HEIGHT, board.SolidCount);
        }
        #endregion
        // 取测试形状的行主序掩码, 与配置资产里写死的三行保持一致
        private static bool[] ShapeMask()
            => InventoryContainerConfigSO.ParseShape(NOTCHED_ROWS, GRID_WIDTH, GRID_HEIGHT);

        // 逐格比对掩码, IReadOnlyList 与数组的整表比较不够直观
        private static void AssertMask(IReadOnlyList<bool> actual, bool[] expected)
        {
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Length, actual.Count);

            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], actual[i], "第 " + i + " 格实心状态不符");
        }

        // 直接往网格里塞一个占位, 用于剥离规则单测几何逻辑
        private InventoryItemPlacement AddToGrid(InventoryGrid grid, int width, int height, Vector2Int cell)
        {
            InventoryItemPlacement placement =
                new InventoryItemPlacement(m_kit.CreateInstance(m_kit.CreateItem(width, height)), cell);
            grid.Add(placement);
            return placement;
        }

        // 往模型塞一件测试物品, 返回它的运行时实例号
        private int AddItem(int width, int height, Vector2Int cell, bool canRotate = true)
        {
            int itemId = m_kit.CreateItem(width, height, canRotate).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_containerId, itemId, 1, cell));
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
    }
}
