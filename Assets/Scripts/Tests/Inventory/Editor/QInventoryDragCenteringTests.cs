/*
 * ┌──────────────────────────────────┐
 * │  描    述: 拖拽跟随测试, 覆盖指针偏移折算、拖影对中、格阵锚点、锚点收边与真实矩形摆放
 * │  类    名: QInventoryDragCenteringTests.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using NUnit.Framework;
using UI.Inventory.View.Tools;
using UnityEngine;

namespace Tests.Inventory
{
    /// <summary>
    /// 拖拽跟随测试：拖影中心连续跟随指针不产生格阵跳变, 锚点格折算与收边与实际落点同源,
    /// 并用真实矩形校验摆放结果与枢轴、缩放无关
    /// </summary>
    public class QInventoryDragCenteringTests
    {
        private const float CELL = 60f;
        private const float SPACING = 4f;
        private const int GRID_WIDTH = 10;
        private const int GRID_HEIGHT = 6;
        private const float EPSILON = 1e-3f;

        private static readonly Vector2Int m_gridCount = new Vector2Int(GRID_WIDTH, GRID_HEIGHT);
        private static readonly Vector2 m_step = new Vector2(CELL + SPACING, CELL + SPACING);

        // 记录用真实矩形搭出来的临时对象
        private readonly List<GameObject> m_spawned = new List<GameObject>();

        // 搭一层物品层: 左上角对齐首格左上角, 并在其下挂一个占格大小的物品矩形
        private RectTransform SpawnLayer(Vector2 layerSize, Vector2 itemPivot, Vector2 itemSize, float scale)
        {
            GameObject layerGo = new GameObject("Layout_Item", typeof(RectTransform));
            m_spawned.Add(layerGo);
            RectTransform layer = layerGo.GetComponent<RectTransform>();
            layer.pivot = new Vector2(0f, 1f);
            layer.sizeDelta = layerSize;
            layer.localScale = Vector3.one * scale;
            layer.ForceUpdateRectTransforms();

            GameObject itemGo = new GameObject("UIInventoryItem", typeof(RectTransform));
            itemGo.transform.SetParent(layer, false);
            RectTransform item = itemGo.GetComponent<RectTransform>();
            item.pivot = itemPivot;
            item.sizeDelta = itemSize;
            item.ForceUpdateRectTransforms();

            return layer;
        }

        // 与视图层同款摆放: 把物品矩形左上角落到物品层的连续偏移上
        private static void PlaceGhost(RectTransform layer, RectTransform ghost, Vector2 ghostOffset)
        {
            Rect layerRect = layer.rect;
            Vector3 cornerWorld = layer.TransformPoint(
                new Vector3(layerRect.xMin + ghostOffset.x, layerRect.yMax + ghostOffset.y, 0f));
            Rect rect = ghost.rect;
            Vector3 pivotToCorner = ghost.TransformPoint(new Vector3(rect.xMin, rect.yMax, 0f)) - ghost.position;

            ghost.position = cornerWorld - pivotToCorner;
        }

        // 断言世界坐标在容差内重合
        private static void AssertWorldPoint(Vector3 expected, Vector3 actual, float delta)
        {
            Assert.AreEqual(expected.x, actual.x, delta);
            Assert.AreEqual(expected.y, actual.y, delta);
            Assert.AreEqual(expected.z, actual.z, delta);
        }

        // 物品矩形中心的世界坐标
        private static Vector3 GhostWorldCenter(RectTransform ghost)
        {
            Rect rect = ghost.rect;

            return ghost.TransformPoint(new Vector3(rect.xMin + rect.width * 0.5f,
                rect.yMin + rect.height * 0.5f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < m_spawned.Count; i++)
                if (m_spawned[i] != null) Object.DestroyImmediate(m_spawned[i]);

            m_spawned.Clear();
        }

        // 占格外接矩形尺寸
        private static Vector2 FootprintSize(Vector2Int footprint) => new Vector2(
            footprint.x * CELL + (footprint.x - 1) * SPACING,
            footprint.y * CELL + (footprint.y - 1) * SPACING);

        // 指针落在指定格子中心时, 相对首格左上角的像素偏移
        private static Vector2 CellCenterOffset(Vector2Int cell) => new Vector2(
            cell.x * m_step.x + CELL * 0.5f,
            -(cell.y * m_step.y + CELL * 0.5f));

        // 锚点格占格外接矩形的中心, 相对首格左上角的像素偏移
        private static Vector2 GhostCenterOffset(Vector2Int anchor, Vector2Int footprint)
        {
            Vector2 size = FootprintSize(footprint);

            return new Vector2(anchor.x * m_step.x + size.x * 0.5f,
                -(anchor.y * m_step.y + size.y * 0.5f));
        }

        // 首格左上角为原点的物品层矩形
        private static Rect OriginLayerRect() => new Rect(0f, -GridSize(m_gridCount).y,
            GridSize(m_gridCount).x, GridSize(m_gridCount).y);

        // 网格外接矩形尺寸
        private static Vector2 GridSize(Vector2Int gridCount) => new Vector2(
            gridCount.x * CELL + (gridCount.x - 1) * SPACING,
            gridCount.y * CELL + (gridCount.y - 1) * SPACING);

        // 断言两个像素偏移在容差内相等
        private static void AssertOffset(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, EPSILON);
            Assert.AreEqual(expected.y, actual.y, EPSILON);
        }

        [Test]
        public void PointerOffset_LayerNotAtOrigin_SubtractsTopLeftCorner()
        {
            Rect layer = new Rect(-250f, -400f, 500f, 400f);

            AssertOffset(new Vector2(0f, 0f),
                DragAnchorCentering.PointerOffset(layer, new Vector2(layer.xMin, layer.yMax)));
            AssertOffset(new Vector2(100f, -50f),
                DragAnchorCentering.PointerOffset(layer, new Vector2(layer.xMin + 100f, layer.yMax - 50f)));
        }

        [Test]
        public void CellCornerOffset_MovesRightAndDown()
        {
            AssertOffset(Vector2.zero, DragAnchorCentering.CellCornerOffset(Vector2Int.zero, m_step));
            AssertOffset(new Vector2(2 * m_step.x, -3 * m_step.y),
                DragAnchorCentering.CellCornerOffset(new Vector2Int(2, 3), m_step));
        }

        [Test]
        public void GhostOffset_PutsGhostCenterOnPointer()
        {
            Vector2 footprint = FootprintSize(new Vector2Int(3, 2));
            Vector2 pointerOffset = new Vector2(300f, -200f);

            Vector2 ghostOffset = DragAnchorCentering.GhostOffset(pointerOffset, footprint);
            Vector2 ghostCenter = ghostOffset + DragAnchorCentering.CenterToCornerOffset(footprint);

            AssertOffset(pointerOffset, ghostCenter);
        }

        [Test]
        public void GhostOffset_TracksOnePixelStepsWithoutLatticeJump()
        {
            Vector2 size = FootprintSize(Vector2Int.one);
            Vector2 start = new Vector2(180f, -300f);
            Vector2 previous = DragAnchorCentering.GhostOffset(start, size);

            // 指针逐像素移动, 拖影必须逐像素跟随, 不能攒到格阵边界才跳一格
            for (int i = 1; i <= 12; i++)
            {
                Vector2 current = DragAnchorCentering.GhostOffset(start + Vector2.right * i, size);
                Assert.AreEqual(1f, current.x - previous.x, EPSILON);
                Assert.AreEqual(0f, current.y - previous.y, EPSILON);
                previous = current;
            }
        }

        [Test]
        public void LatticeAnchor_OneByOneAtCellCenter_ReturnsThatCell()
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    Vector2 ghostOffset = DragAnchorCentering.GhostOffset(CellCenterOffset(cell),
                        FootprintSize(Vector2Int.one));

                    Assert.AreEqual(cell,
                        DragAnchorCentering.LatticeAnchor(ghostOffset, m_step),
                        $"格子 {cell} 的中心应当折算回自身");
                }
            }
        }

        [Test]
        public void LatticeAnchor_OddFootprint_CentersOnPointer()
        {
            Vector2Int footprint = new Vector2Int(3, 3);
            Vector2Int anchor = new Vector2Int(4, 3);
            // 指针压在中间那格上, 占格向四周各让出一格, 锚点落在左上角
            Vector2 ghostOffset = DragAnchorCentering.GhostOffset(CellCenterOffset(anchor),
                FootprintSize(footprint));

            Assert.AreEqual(new Vector2Int(anchor.x - 1, anchor.y - 1),
                DragAnchorCentering.LatticeAnchor(ghostOffset, m_step));
        }

        [Test]
        public void LatticeAnchor_EvenFootprint_KeepsLatticeAlignment()
        {
            Vector2Int footprint = new Vector2Int(2, 2);

            Assert.AreEqual(new Vector2Int(2, 0), DragAnchorCentering.LatticeAnchor(
                DragAnchorCentering.CellCornerOffset(new Vector2Int(2, 0), m_step), m_step));
            Assert.AreEqual(new Vector2Int(3, 1), DragAnchorCentering.LatticeAnchor(
                DragAnchorCentering.CellCornerOffset(new Vector2Int(3, 1), m_step), m_step));
        }

        [Test]
        public void LatticeAnchor_FlipsCellWhenGhostCrossesHalfStep()
        {
            Vector2 size = FootprintSize(Vector2Int.one);
            // 指针在 0 格中心, 拖影左上角落在 0 格角点
            Assert.AreEqual(Vector2Int.zero, DragAnchorCentering.LatticeAnchor(
                DragAnchorCentering.GhostOffset(CellCenterOffset(Vector2Int.zero), size), m_step));
            // 指针越过 0 格与 1 格的中线, 拖影左上角越过 1 格角点, 锚点随之翻转
            Assert.AreEqual(new Vector2Int(1, 0), DragAnchorCentering.LatticeAnchor(
                DragAnchorCentering.GhostOffset(new Vector2(m_step.x + 1f, -CELL * 0.5f), size), m_step));
        }

        [Test]
        public void LatticeAnchor_DegenerateStep_ReturnsOrigin()
        {
            Assert.AreEqual(Vector2Int.zero,
                DragAnchorCentering.LatticeAnchor(new Vector2(300f, -300f), Vector2.zero));
            Assert.AreEqual(Vector2Int.zero,
                DragAnchorCentering.LatticeAnchor(new Vector2(300f, -300f), new Vector2(0f, 64f)));
        }

        [Test]
        public void ClampAnchor_OutsideGrid_ClampsToEdge()
        {
            Vector2Int footprint = new Vector2Int(2, 2);

            Assert.AreEqual(new Vector2Int(0, 0),
                DragAnchorCentering.ClampAnchor(new Vector2Int(-3, -3), footprint, m_gridCount));
            Assert.AreEqual(new Vector2Int(GRID_WIDTH - 2, GRID_HEIGHT - 2),
                DragAnchorCentering.ClampAnchor(new Vector2Int(99, 99), footprint, m_gridCount));
        }

        [Test]
        public void ClampAnchor_OversizedFootprint_FallsBackToOrigin()
        {
            Assert.AreEqual(Vector2Int.zero,
                DragAnchorCentering.ClampAnchor(new Vector2Int(4, 4), new Vector2Int(20, 20), m_gridCount));
        }

        [Test]
        public void ClampAnchor_ZeroFootprint_TreatedAsSingleCell()
        {
            Assert.AreEqual(new Vector2Int(5, 5),
                DragAnchorCentering.ClampAnchor(new Vector2Int(5, 5), Vector2Int.zero, m_gridCount));
            Assert.AreEqual(new Vector2Int(GRID_WIDTH - 1, GRID_HEIGHT - 1),
                DragAnchorCentering.ClampAnchor(new Vector2Int(99, 99), Vector2Int.zero, m_gridCount));
        }

        [Test]
        public void DropAnchor_AcrossGrid_StaysInsideAndMatchesPointerCell()
        {
            Vector2Int footprint = new Vector2Int(3, 2);
            Vector2 size = FootprintSize(footprint);

            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    Vector2 pointerOffset = new Vector2(x * m_step.x + CELL * 0.5f, -(y * m_step.y + CELL * 0.5f));
                    Vector2 ghostOffset = DragAnchorCentering.GhostOffset(pointerOffset, size);
                    Vector2Int anchor = DragAnchorCentering.ClampAnchor(
                        DragAnchorCentering.LatticeAnchor(ghostOffset, m_step), footprint, m_gridCount);

                    Assert.GreaterOrEqual(anchor.x, 0);
                    Assert.GreaterOrEqual(anchor.y, 0);
                    Assert.LessOrEqual(anchor.x, GRID_WIDTH - footprint.x);
                    Assert.LessOrEqual(anchor.y, GRID_HEIGHT - footprint.y);
                }
            }
        }

        [Test]
        public void DropAnchor_RoundTrip_RecoversSourceAnchor()
        {
            Vector2Int footprint = new Vector2Int(2, 3);

            for (int y = 0; y <= GRID_HEIGHT - footprint.y; y++)
            {
                for (int x = 0; x <= GRID_WIDTH - footprint.x; x++)
                {
                    Vector2Int anchor = new Vector2Int(x, y);
                    Vector2 ghostOffset = DragAnchorCentering.GhostOffset(
                        GhostCenterOffset(anchor, footprint), FootprintSize(footprint));

                    Assert.AreEqual(anchor, DragAnchorCentering.LatticeAnchor(ghostOffset, m_step),
                        $"占格 {footprint} 落在锚点 {anchor} 时, 拖影中心对准指针后应当折回同一锚点");
                    AssertOffset(DragAnchorCentering.CellCornerOffset(anchor, m_step), ghostOffset);
                }
            }
        }

        [Test]
        public void PointerOffset_LayerAtOrigin_MatchesLocalPoint()
        {
            AssertOffset(new Vector2(120f, -240f),
                DragAnchorCentering.PointerOffset(OriginLayerRect(), new Vector2(120f, -240f)));
        }

        [Test]
        public void GhostPlacement_OneByOne_RectCenterSitsOnPointer()
        {
            RectTransform layer = SpawnLayer(GridSize(m_gridCount), new Vector2(0f, 1f),
                FootprintSize(Vector2Int.one), 1f);
            RectTransform ghost = layer.GetChild(0) as RectTransform;
            Vector2Int cell = new Vector2Int(4, 2);
            Vector2 pointerLocal = CellCenterOffset(cell);

            Vector2 ghostOffset = DragAnchorCentering.GhostOffset(
                DragAnchorCentering.PointerOffset(layer.rect, pointerLocal), ghost.rect.size);
            PlaceGhost(layer, ghost, ghostOffset);

            AssertWorldPoint(layer.TransformPoint(new Vector3(pointerLocal.x, pointerLocal.y, 0f)),
                GhostWorldCenter(ghost), EPSILON);
            Assert.AreEqual(cell, DragAnchorCentering.LatticeAnchor(ghostOffset, m_step));
        }

        [Test]
        public void GhostPlacement_WideFootprint_RectCenterSitsOnPointer()
        {
            Vector2Int footprint = new Vector2Int(5, 1);
            RectTransform layer = SpawnLayer(GridSize(m_gridCount), new Vector2(0f, 1f),
                FootprintSize(footprint), 1f);
            RectTransform ghost = layer.GetChild(0) as RectTransform;
            Vector2Int anchor = new Vector2Int(1, 3);
            Vector2 pointerLocal = GhostCenterOffset(anchor, footprint);

            Vector2 ghostOffset = DragAnchorCentering.GhostOffset(
                DragAnchorCentering.PointerOffset(layer.rect, pointerLocal), ghost.rect.size);
            PlaceGhost(layer, ghost, ghostOffset);

            AssertWorldPoint(layer.TransformPoint(new Vector3(pointerLocal.x, pointerLocal.y, 0f)),
                GhostWorldCenter(ghost), EPSILON);
            Assert.AreEqual(anchor, DragAnchorCentering.LatticeAnchor(ghostOffset, m_step));
        }

        [Test]
        public void GhostPlacement_CustomPivot_StillSitsOnPointer()
        {
            RectTransform layer = SpawnLayer(GridSize(m_gridCount), new Vector2(0.3f, 0.7f),
                FootprintSize(new Vector2Int(2, 3)), 1f);
            RectTransform ghost = layer.GetChild(0) as RectTransform;
            Vector2 pointerLocal = new Vector2(180f, -205f);

            Vector2 ghostOffset = DragAnchorCentering.GhostOffset(
                DragAnchorCentering.PointerOffset(layer.rect, pointerLocal), ghost.rect.size);
            PlaceGhost(layer, ghost, ghostOffset);

            AssertWorldPoint(layer.TransformPoint(new Vector3(pointerLocal.x, pointerLocal.y, 0f)),
                GhostWorldCenter(ghost), EPSILON);
        }

        [Test]
        public void GhostPlacement_ScaledLayer_StillSitsOnPointer()
        {
            RectTransform layer = SpawnLayer(GridSize(m_gridCount), new Vector2(0f, 1f),
                FootprintSize(new Vector2Int(3, 2)), 0.35f);
            RectTransform ghost = layer.GetChild(0) as RectTransform;
            Vector2 pointerLocal = new Vector2(260f, -130f);

            Vector2 ghostOffset = DragAnchorCentering.GhostOffset(
                DragAnchorCentering.PointerOffset(layer.rect, pointerLocal), ghost.rect.size);
            PlaceGhost(layer, ghost, ghostOffset);

            AssertWorldPoint(layer.TransformPoint(new Vector3(pointerLocal.x, pointerLocal.y, 0f)),
                GhostWorldCenter(ghost), 0.01f);
        }

        [Test]
        public void GhostPlacement_PointerNearGridEdge_AnchorStaysInsideGrid()
        {
            Vector2Int footprint = new Vector2Int(2, 2);
            RectTransform layer = SpawnLayer(GridSize(m_gridCount), new Vector2(0f, 1f),
                FootprintSize(footprint), 1f);
            RectTransform ghost = layer.GetChild(0) as RectTransform;
            Vector2 pointerLocal = new Vector2(GridSize(m_gridCount).x - CELL * 0.5f,
                -(GridSize(m_gridCount).y - CELL * 0.5f));

            Vector2 ghostOffset = DragAnchorCentering.GhostOffset(
                DragAnchorCentering.PointerOffset(layer.rect, pointerLocal), ghost.rect.size);
            Vector2Int anchor = DragAnchorCentering.ClampAnchor(
                DragAnchorCentering.LatticeAnchor(ghostOffset, m_step), footprint, m_gridCount);

            Assert.AreEqual(new Vector2Int(GRID_WIDTH - 2, GRID_HEIGHT - 2), anchor);
            Assert.LessOrEqual(anchor.x + footprint.x - 1, GRID_WIDTH - 1);
            Assert.LessOrEqual(anchor.y + footprint.y - 1, GRID_HEIGHT - 1);
        }
    }
}
