/*
* ┌──────────────────────────────────┐
* │  描    述: 背包逻辑区域测试, 覆盖区域行解析、跨区占格、旋转与区域边框
* │  类    名: QInventoryRegionTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Text.RegularExpressions;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Grid;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// 逻辑区域测试：视觉相连但分区不同的格子不能被同一件物品跨越
    /// </summary>
    public class QInventoryRegionTests
    {
        private const int WIDTH = 4;
        private const int HEIGHT = 2;
        private static readonly string[] SPLIT_REGION_ROWS = { "AABB", "BBAA" };
        private static readonly string[] SINGLE_REGION_ROWS = { "AAAA", "AAAA" };

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private InventoryViewModel m_viewModel;
        private int m_containerId;
        private IDisposable m_hintSubscription;

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();

            m_containerId = m_kit.NewContainerId();
            m_model.RegisterContainer(m_containerId, CreateRegionSpec(SPLIT_REGION_ROWS));

            m_viewModel = new InventoryViewModel(m_model, m_containerId);
            m_hintSubscription = m_viewModel.HintPulse.Subscribe(() => { });
            m_viewModel.OnBind();
        }

        [TearDown]
        public void TearDown()
        {
            m_hintSubscription.Dispose();
            m_viewModel.OnUnbind();
            m_viewModel.OnDispose();
            m_viewModel.Dispose();
            m_model.OnUnregister();
            m_kit.Dispose();
        }

        [Test]
        public void ParseRegions_EmptyRows_UsesRegionZeroForSolidCellsAndMinusOneForHoles()
        {
            bool[] mask = InventoryContainerConfigSO.ParseShape(new[] { "#.#", "###" }, 3, 2);
            int[] regions = InventoryContainerConfigSO.ParseRegions(null, 3, 2, mask);

            Assert.AreEqual(0, regions[0]);
            Assert.AreEqual(-1, regions[1]);
            Assert.AreEqual(0, regions[2]);
            Assert.AreEqual(0, regions[3]);
            Assert.AreEqual(0, regions[4]);
            Assert.AreEqual(0, regions[5]);
        }

        [Test]
        public void ParseRegions_ValidRows_MapHashAndLettersByRowMajorOrder()
        {
            bool[] mask = InventoryContainerConfigSO.ParseShape(new[] { "####", "####" }, WIDTH, HEIGHT);
            int[] regions = InventoryContainerConfigSO.ParseRegions(SPLIT_REGION_ROWS, WIDTH, HEIGHT, mask);

            Assert.AreEqual(new[] { 1, 1, 2, 2, 2, 2, 1, 1 }, regions);
        }

        [Test]
        public void ParseRegions_RowCountMismatch_LogsAndFallsBackToRegionZero()
        {
            LogAssert.Expect(LogType.Error, new Regex("区域行数"));
            bool[] mask = InventoryContainerConfigSO.ParseShape(new[] { "###", "###" }, 3, 2);

            int[] regions = InventoryContainerConfigSO.ParseRegions(new[] { "AAB" }, 3, 2, mask);

            AssertAllSolidRegionsAreZero(regions, mask);
        }

        [Test]
        public void ParseRegions_RowWidthMismatch_LogsAndFallsBackToRegionZero()
        {
            LogAssert.Expect(LogType.Error, new Regex("区域长度"));
            bool[] mask = InventoryContainerConfigSO.ParseShape(new[] { "###", "###" }, 3, 2);

            int[] regions = InventoryContainerConfigSO.ParseRegions(new[] { "AAB", "BB" }, 3, 2, mask);

            AssertAllSolidRegionsAreZero(regions, mask);
        }

        [Test]
        public void ParseRegions_IllegalCharacter_LogsAndFallsBackToRegionZero()
        {
            LogAssert.Expect(LogType.Error, new Regex("非法区域字符"));
            bool[] mask = InventoryContainerConfigSO.ParseShape(new[] { "###", "###" }, 3, 2);

            int[] regions = InventoryContainerConfigSO.ParseRegions(new[] { "AA0", "BBB" }, 3, 2, mask);

            AssertAllSolidRegionsAreZero(regions, mask);
        }

        [Test]
        public void ParseRegions_ShapeMismatch_LogsAndFallsBackToRegionZero()
        {
            LogAssert.Expect(LogType.Error, new Regex("与形状掩码不一致"));
            bool[] mask = InventoryContainerConfigSO.ParseShape(new[] { "###", "###" }, 3, 2);

            int[] regions = InventoryContainerConfigSO.ParseRegions(new[] { "A.A", "BBB" }, 3, 2, mask);

            AssertAllSolidRegionsAreZero(regions, mask);
        }

        [Test]
        public void Grid_RegionTable_KeepsHolesAsMinusOneAndNormalizesMissingRegions()
        {
            bool[] mask = InventoryContainerConfigSO.ParseShape(new[] { "#.#", "###" }, 3, 2);
            int[] regions = InventoryContainerConfigSO.ParseRegions(new[] { "A.B", "AAA" }, 3, 2, mask);
            InventoryGrid grid = new InventoryGrid(3, 2, mask, regions);

            Assert.AreEqual(1, grid.GetRegionId(new Vector2Int(0, 0)));
            Assert.AreEqual(-1, grid.GetRegionId(new Vector2Int(1, 0)));
            Assert.AreEqual(2, grid.GetRegionId(new Vector2Int(2, 0)));

            InventoryGrid defaultGrid = new InventoryGrid(3, 2, mask);
            Assert.AreEqual(0, defaultGrid.GetRegionId(new Vector2Int(0, 0)));
            Assert.AreEqual(-1, defaultGrid.GetRegionId(new Vector2Int(1, 0)));
        }

        [Test]
        public void TryAddItem_WithinOneRegion_Succeeds()
        {
            int itemId = m_kit.CreateItem(2, 1).ItemId;

            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_containerId, itemId, 1, Vector2Int.zero));
        }

        [Test]
        public void TryAddItem_AcrossTwoRegions_IsRejected()
        {
            int horizontalId = m_kit.CreateItem(2, 1).ItemId;
            int verticalId = m_kit.CreateItem(1, 2).ItemId;

            Assert.AreEqual(InventoryMoveResult.OutOfBounds,
                m_model.TryAddItem(m_containerId, horizontalId, 1, new Vector2Int(1, 0)));
            Assert.AreEqual(InventoryMoveResult.OutOfBounds,
                m_model.TryAddItem(m_containerId, verticalId, 1, Vector2Int.zero));
        }

        [Test]
        public void TryRotateItem_WhenRotatedFootprintCrossesRegion_IsRejectedAndKeepsFacing()
        {
            int itemId = m_kit.CreateItem(2, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_containerId, itemId, 1, Vector2Int.zero));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, Vector2Int.zero, out int instanceId));

            Assert.AreEqual(InventoryMoveResult.OutOfBounds, m_model.TryRotateItem(m_containerId, instanceId));

            InventoryContainerVO vo = m_model.GetContainerVO(m_containerId);
            Assert.IsTrue(vo.TryGetItem(instanceId, out InventoryItemVO item));
            Assert.AreEqual(InventoryRotation.None, item.Rotation);
            Assert.AreEqual(new Vector2Int(2, 1), item.Size);
        }

        [Test]
        public void Slots_RegionEdges_DistinguishConnectedBoundaryAndNone()
        {
            InventorySlotViewModel topLeft = m_viewModel.GetSlot(0);
            InventorySlotViewModel topSecond = m_viewModel.GetSlot(1);
            InventorySlotViewModel bottomLeft = m_viewModel.GetSlot(4);

            Assert.AreEqual(InventorySlotEdgeKind.Boundary, topLeft.TopEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, topLeft.LeftEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Connected, topLeft.RightEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, topLeft.BottomEdgeKind.Value);

            Assert.AreEqual(InventorySlotEdgeKind.Boundary, topSecond.TopEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, topSecond.RightEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, topSecond.BottomEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Connected, topSecond.LeftEdgeKind.Value);

            Assert.AreEqual(InventorySlotEdgeKind.BoundaryPassive, bottomLeft.TopEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, bottomLeft.LeftEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Connected, bottomLeft.RightEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, bottomLeft.BottomEdgeKind.Value);
        }

        [Test]
        public void Slots_RegionChangeWithSameSize_RefreshesRegionAndEdgeState()
        {
            Assert.AreEqual(1, m_viewModel.GetSlot(0).RegionId.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, m_viewModel.GetSlot(1).RightEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, m_viewModel.GetSlot(0).BottomEdgeKind.Value);

            Assert.IsTrue(m_model.TryResizeContainer(m_containerId, CreateRegionSpec(SINGLE_REGION_ROWS)));

            for (int i = 0; i < m_viewModel.SlotCount; i++)
                Assert.AreEqual(1, m_viewModel.GetSlot(i).RegionId.Value);

            Assert.AreEqual(InventorySlotEdgeKind.Connected, m_viewModel.GetSlot(0).RightEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Connected, m_viewModel.GetSlot(0).BottomEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, m_viewModel.GetSlot(3).RightEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, m_viewModel.GetSlot(4).BottomEdgeKind.Value);
        }

        [Test]
        public void Slots_Hole_HasNoneEdgesAndSolidNeighboursMarkItAsBoundary()
        {
            bool[] mask = InventoryContainerConfigSO.ParseShape(new[] { "#.#" }, 3, 1);
            int[] regions = InventoryContainerConfigSO.ParseRegions(new[] { "A.A" }, 3, 1, mask);
            Assert.IsTrue(m_model.TryResizeContainer(m_containerId,
                new InventoryContainerSpec(3, 1, float.MaxValue, mask, regionIds: regions)));

            InventorySlotViewModel hole = m_viewModel.GetSlot(1);
            InventorySlotViewModel left = m_viewModel.GetSlot(0);
            InventorySlotViewModel right = m_viewModel.GetSlot(2);

            Assert.AreEqual(-1, hole.RegionId.Value);
            Assert.AreEqual(InventorySlotEdgeKind.None, hole.TopEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.None, hole.RightEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.None, hole.BottomEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.None, hole.LeftEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, left.RightEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, right.LeftEdgeKind.Value);
        }

        [Test]
        public void Slots_SameRegionAdjacency_IsConnectedOnBothSides()
        {
            InventorySlotViewModel topLeft = m_viewModel.GetSlot(0);
            InventorySlotViewModel topSecond = m_viewModel.GetSlot(1);

            Assert.AreEqual(InventorySlotEdgeKind.Connected, topLeft.RightEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Connected, topSecond.LeftEdgeKind.Value);
        }

        [Test]
        public void Slots_DifferentRegionAdjacency_DrawsBoundaryOnlyOnLeadingCell()
        {
            InventorySlotViewModel topSecond = m_viewModel.GetSlot(1);
            InventorySlotViewModel topThird = m_viewModel.GetSlot(2);
            InventorySlotViewModel topLeft = m_viewModel.GetSlot(0);
            InventorySlotViewModel bottomLeft = m_viewModel.GetSlot(4);

            Assert.AreEqual(InventorySlotEdgeKind.Boundary, topSecond.RightEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.BoundaryPassive, topThird.LeftEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, topLeft.BottomEdgeKind.Value);
            Assert.AreEqual(InventorySlotEdgeKind.BoundaryPassive, bottomLeft.TopEdgeKind.Value);
        }

        //构造区域容器规格, 用形状配置实际产出的区域表验证完整链路
        private InventoryContainerSpec CreateRegionSpec(string[] regionRows)
        {
            InventoryContainerConfigSO config = m_kit.CreateContainer(WIDTH, HEIGHT, "TEST_REGION", regionRows: regionRows);
            return new InventoryContainerSpec(config.Width, config.Height, float.MaxValue, config.Mask, regionIds: config.RegionIds);
        }

        //逐格确认实心区域全部回退为 0, 空洞保持 -1
        private static void AssertAllSolidRegionsAreZero(int[] regions, bool[] mask)
        {
            Assert.AreEqual(mask.Length, regions.Length);
            for (int i = 0; i < mask.Length; i++)
                Assert.AreEqual(mask[i] ? 0 : -1, regions[i], "第 " + i + " 格区域号不符");
        }
    }
}
