/*
* ┌──────────────────────────────────┐
* │  描    述: 逻辑区域绘制板测试, 覆盖加载、拖刷、形状同步与字母行写回
* │  类    名: QInventoryRegionBoardTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Inventory.Core.Config;
using NUnit.Framework;
using UI.Inventory.Editor;

namespace Tests.Inventory
{
    /// <summary>
    /// 逻辑区域绘制板测试：区域绘制只发生在实心格，并能稳定写回 RegionRowsValue
    /// </summary>
    public class QInventoryRegionBoardTests
    {
        private const int WIDTH = 4;
        private const int HEIGHT = 2;
        private const int HOLE_INDEX = 1;

        private static readonly bool[] SHAPE =
            InventoryContainerConfigSO.ParseShape(new[] { "#.##", "####" }, WIDTH, HEIGHT);

        [Test]
        public void Load_EmptyRows_UsesHashForSolidCellsAndVoidForHoles()
        {
            InventoryRegionBoard board = new InventoryRegionBoard();

            board.Load(null, SHAPE, WIDTH, HEIGHT);

            Assert.AreEqual(0, board.Regions[0]);
            Assert.AreEqual(-1, board.Regions[HOLE_INDEX]);
            Assert.AreEqual(0, board.Regions[2]);
            Assert.AreEqual("#.##", InventoryRegionBoard.ToMarkRow(board.Regions, 0, WIDTH));
        }

        [Test]
        public void ApplyStroke_PaintsSolidCellsAndKeepsTheDownRegionAcrossDrag()
        {
            InventoryRegionBoard board = new InventoryRegionBoard();
            board.Load(null, SHAPE, WIDTH, HEIGHT);

            Assert.IsTrue(board.ApplyStroke(0, InventoryRegionBoard.StrokePhase.Down, 2));
            Assert.IsTrue(board.ApplyStroke(2, InventoryRegionBoard.StrokePhase.Drag, 5));

            Assert.AreEqual(2, board.Regions[0]);
            Assert.AreEqual(2, board.Regions[2]);
            Assert.IsFalse(board.ApplyStroke(HOLE_INDEX, InventoryRegionBoard.StrokePhase.Drag, 5));
            Assert.AreEqual(-1, board.Regions[HOLE_INDEX]);

            board.ApplyStroke(0, InventoryRegionBoard.StrokePhase.Up, 0);
            Assert.IsFalse(board.ApplyStroke(3, InventoryRegionBoard.StrokePhase.Drag, 5));
        }

        [Test]
        public void ApplyStroke_WithDefaultRegion_RestoresHash()
        {
            InventoryRegionBoard board = new InventoryRegionBoard();
            board.Load(new[] { 1, -1, 2, 2, 2, 2, 2, 2 }, SHAPE, WIDTH, HEIGHT);

            Assert.IsTrue(board.ApplyStroke(0, InventoryRegionBoard.StrokePhase.Down, 0));
            Assert.AreEqual(0, board.Regions[0]);
        }

        [Test]
        public void SyncMask_ClearsNewHolesAndKeepsRegionsOnRemainingSolidCells()
        {
            InventoryRegionBoard board = new InventoryRegionBoard();
            board.Load(new[] { 1, 1, 2, 2, 2, 2, 2, 1 }, null, WIDTH, HEIGHT);

            bool[] mask = { true, false, true, true, true, true, true, false };
            board.SyncMask(mask);

            Assert.AreEqual(new[] { 1, -1, 2, 2, 2, 2, 2, -1 }, board.Regions);
        }

        [Test]
        public void ToRegionRows_RoundTripsThroughParseRegions()
        {
            InventoryRegionBoard board = new InventoryRegionBoard();
            board.Load(null, SHAPE, WIDTH, HEIGHT);
            board.ApplyStroke(0, InventoryRegionBoard.StrokePhase.Down, 1);
            board.ApplyStroke(2, InventoryRegionBoard.StrokePhase.Down, 2);
            board.ApplyStroke(3, InventoryRegionBoard.StrokePhase.Down, 2);

            string[] rows = InventoryRegionBoard.ToRegionRows(board.Regions, WIDTH, HEIGHT);
            int[] parsed = InventoryContainerConfigSO.ParseRegions(rows, WIDTH, HEIGHT, SHAPE);

            Assert.AreEqual(new[] { "A.BB", "####" }, rows);
            Assert.AreEqual(board.Regions, parsed);
        }

        [Test]
        public void Resize_KeepsTopLeftRegionAssignmentsAndDefaultsNewCellsToHash()
        {
            InventoryRegionBoard board = new InventoryRegionBoard();
            board.Load(new[] { 1, 1, 2, 2, 2, 2, 2, 1 }, null, WIDTH, HEIGHT);

            board.SyncMask(InventoryRegionBoard.CreateMask(5, 3), 5, 3);

            Assert.AreEqual(15, board.Regions.Length);
            Assert.AreEqual(1, board.Regions[0]);
            Assert.AreEqual(1, board.Regions[1]);
            Assert.AreEqual(2, board.Regions[2]);
            Assert.AreEqual(0, board.Regions[4]);
            Assert.AreEqual(0, board.Regions[14]);
        }
    }
}
