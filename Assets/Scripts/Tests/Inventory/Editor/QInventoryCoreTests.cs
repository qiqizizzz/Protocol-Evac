/*
* ┌──────────────────────────────────┐
* │  描    述: 背包纯逻辑层测试, 覆盖网格几何、规则判定与容器事务
* │  类    名: QInventoryCoreTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Text.RegularExpressions;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Grid;
using Module.Inventory.Core.Rule;
using Module.Inventory.Core.Rule.Extensions;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// Core 层测试：网格占用与边界、规则操作码、容器版本号与变更事件
    /// </summary>
    public class QInventoryCoreTests
    {
        private QInventoryTestKit m_kit;

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
        }

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }

        #region 网格几何
        [Test]
        public void Grid_IsInside_AcceptsOnlyCellsWithinBounds()
        {
            InventoryGrid grid = new InventoryGrid(4, 2);

            Assert.IsTrue(grid.IsInside(new Vector2Int(0, 0)));
            Assert.IsTrue(grid.IsInside(new Vector2Int(3, 1)));
            Assert.IsFalse(grid.IsInside(new Vector2Int(4, 0)));
            Assert.IsFalse(grid.IsInside(new Vector2Int(0, 2)));
            Assert.IsFalse(grid.IsInside(new Vector2Int(-1, 0)));
        }

        [Test]
        public void Grid_IsAreaInside_RejectsAnyPartLeavingTheGrid()
        {
            InventoryGrid grid = new InventoryGrid(4, 2);
            Vector2Int size = new Vector2Int(2, 2);

            Assert.IsTrue(grid.IsAreaInside(new Vector2Int(2, 0), size));
            Assert.IsFalse(grid.IsAreaInside(new Vector2Int(3, 0), size));
            Assert.IsFalse(grid.IsAreaInside(new Vector2Int(2, 1), size));
            Assert.IsFalse(grid.IsAreaInside(new Vector2Int(-1, 0), size));
        }

        [Test]
        public void Grid_GetCellIndex_UsesRowMajorOrder()
        {
            InventoryGrid grid = new InventoryGrid(4, 2);

            Assert.AreEqual(0, grid.GetCellIndex(new Vector2Int(0, 0)));
            Assert.AreEqual(3, grid.GetCellIndex(new Vector2Int(3, 0)));
            Assert.AreEqual(4, grid.GetCellIndex(new Vector2Int(0, 1)));
            Assert.AreEqual(7, grid.GetCellIndex(new Vector2Int(3, 1)));
        }

        [Test]
        public void Grid_AddThenTryGetPlacementAt_ResolvesEveryCoveredCell()
        {
            InventoryGrid grid = new InventoryGrid(4, 4);
            InventoryItemPlacement placement = AddToGrid(grid, m_kit.CreateItem(2, 1), new Vector2Int(1, 0));

            Assert.IsTrue(grid.TryGetPlacementAt(new Vector2Int(1, 0), out InventoryItemPlacement first));
            Assert.IsTrue(grid.TryGetPlacementAt(new Vector2Int(2, 0), out InventoryItemPlacement second));
            Assert.AreSame(placement, first);
            Assert.AreSame(placement, second);

            Assert.IsFalse(grid.TryGetPlacementAt(new Vector2Int(0, 0), out _));
            Assert.IsFalse(grid.TryGetPlacementAt(new Vector2Int(9, 9), out _));
            Assert.AreEqual(1, grid.Count);
        }

        [Test]
        public void Grid_IsAreaFree_IgnoresOnlyThePlacementBeingRechecked()
        {
            InventoryGrid grid = new InventoryGrid(4, 4);
            InventoryItemPlacement placement = AddToGrid(grid, m_kit.CreateItem(2, 2), new Vector2Int(0, 0));
            Vector2Int size = new Vector2Int(2, 2);

            // 自己的足迹压在自己的足迹上, 不忽略就判冲突, 忽略后放行
            Assert.IsFalse(grid.IsAreaFree(new Vector2Int(0, 0), size));
            Assert.IsTrue(grid.IsAreaFree(new Vector2Int(0, 0), size, placement));

            // 忽略自己不等于放行别人
            InventoryItemPlacement stranger = AddToGrid(grid, m_kit.CreateItem(1, 1), new Vector2Int(2, 1));
            Assert.IsTrue(grid.IsAreaFree(new Vector2Int(0, 2), size, placement));
            Assert.IsFalse(grid.IsAreaFree(new Vector2Int(1, 1), size, placement));
        }

        [Test]
        public void Grid_Remove_FreesEveryCellThePlacementCovered()
        {
            InventoryGrid grid = new InventoryGrid(4, 4);
            InventoryItemPlacement placement = AddToGrid(grid, m_kit.CreateItem(2, 2), new Vector2Int(1, 1));

            Assert.IsTrue(grid.Remove(placement));
            Assert.IsFalse(grid.Remove(placement));
            Assert.AreEqual(0, grid.Count);
            Assert.IsFalse(grid.TryGetPlacementAt(new Vector2Int(2, 2), out _));
            Assert.IsTrue(grid.IsAreaFree(new Vector2Int(1, 1), new Vector2Int(2, 2)));
        }

        [Test]
        public void Config_GetSizeAt_SwapsWidthAndHeightOnOddRotation()
        {
            InventoryItemConfigSO config = m_kit.CreateItem(2, 3);

            Assert.AreEqual(new Vector2Int(2, 3), config.GetSizeAt(InventoryRotation.None));
            Assert.AreEqual(new Vector2Int(3, 2), config.GetSizeAt(InventoryRotation.Rotate90));
            Assert.AreEqual(new Vector2Int(2, 3), config.GetSizeAt(InventoryRotation.Rotate180));
            Assert.AreEqual(new Vector2Int(3, 2), config.GetSizeAt(InventoryRotation.Rotate270));
        }

        #endregion
        #region 规则判定
        [Test]
        public void Rules_CanAdd_ReturnsOutOfBoundsWhenItemLeavesGrid()
        {
            InventoryContainer container = NewContainer(4, 4, float.MaxValue);
            InventoryItemInstance instance = m_kit.CreateInstance(m_kit.CreateItem(3, 3));

            Assert.AreEqual(InventoryMoveResult.OutOfBounds,
                InventoryRules.Instance.CanAdd(container, instance, new Vector2Int(2, 2), InventoryRotation.None));
        }

        [Test]
        public void Rules_CanAdd_ReturnsOccupiedWhenAreaAlreadyTaken()
        {
            InventoryContainer container = NewContainer(4, 4, float.MaxValue);
            Assert.IsNotNull(container.TryAdd(m_kit.CreateInstance(m_kit.CreateItem(2, 2)), new Vector2Int(0, 0)));

            InventoryItemInstance blockerProbe = m_kit.CreateInstance(m_kit.CreateItem(2, 1));

            Assert.AreEqual(InventoryMoveResult.Occupied,
                InventoryRules.Instance.CanAdd(container, blockerProbe, new Vector2Int(1, 1), InventoryRotation.None));
            Assert.AreEqual(InventoryMoveResult.Success,
                InventoryRules.Instance.CanAdd(container, blockerProbe, new Vector2Int(2, 3), InventoryRotation.None));
        }

        [Test]
        public void Rules_CanAdd_ReturnsWeightExceededBeforeAnyGeometryCheck()
        {
            InventoryContainer container = NewContainer(4, 4, 3f);
            InventoryItemInstance heavy = m_kit.CreateInstance(m_kit.CreateItem(4, 4, unitWeight: 10f));

            Assert.AreEqual(InventoryMoveResult.WeightExceeded,
                InventoryRules.Instance.CanAdd(container, heavy, new Vector2Int(9, 9), InventoryRotation.None));
        }

        [Test]
        public void Rules_CanMove_ReturnsSameSpotForAnIdenticalRequest()
        {
            InventoryContainer container = NewContainer(4, 4, float.MaxValue);
            InventoryItemPlacement placement = container.TryAdd(
                m_kit.CreateInstance(m_kit.CreateItem(2, 1)), new Vector2Int(1, 1));

            Assert.AreEqual(InventoryMoveResult.SameSpot,
                InventoryRules.Instance.CanMove(container, container, placement, new Vector2Int(1, 1),
                    InventoryRotation.None));
        }

        [Test]
        public void Rules_CanMove_ReturnsSourceNotFoundWhenPlacementIsNotInSource()
        {
            InventoryContainer owner = NewContainer(4, 4, float.MaxValue);
            InventoryContainer stranger = NewContainer(4, 4, float.MaxValue);
            InventoryItemPlacement placement = owner.TryAdd(
                m_kit.CreateInstance(m_kit.CreateItem(1, 1)), new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.SourceNotFound,
                InventoryRules.Instance.CanMove(stranger, stranger, placement, new Vector2Int(1, 1),
                    InventoryRotation.None));
        }

        [Test]
        public void Rules_CanMove_ReturnsNotRotatableWhenConfigForbidsFacingChange()
        {
            InventoryContainer container = NewContainer(4, 4, float.MaxValue);
            InventoryItemPlacement placement = container.TryAdd(
                m_kit.CreateInstance(m_kit.CreateItem(1, 1, canRotate: false)), new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.NotRotatable,
                InventoryRules.Instance.CanMove(container, container, placement, new Vector2Int(2, 2),
                    InventoryRotation.Rotate90));
            Assert.AreEqual(InventoryMoveResult.Success,
                InventoryRules.Instance.CanMove(container, container, placement, new Vector2Int(2, 2),
                    InventoryRotation.None));
        }

        [Test]
        public void Rules_CanMove_AllowsOverlapWithItsOwnFootprintInsideSameContainer()
        {
            InventoryContainer container = NewContainer(4, 4, float.MaxValue);
            InventoryItemPlacement placement = container.TryAdd(
                m_kit.CreateInstance(m_kit.CreateItem(2, 2)), new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.Success,
                InventoryRules.Instance.CanMove(container, container, placement, new Vector2Int(1, 0),
                    InventoryRotation.None));
        }

        [Test]
        public void Rules_CanMove_ReturnsWeightExceededWhenTargetContainerIsTooSmall()
        {
            InventoryContainer source = NewContainer(4, 4, float.MaxValue);
            InventoryContainer target = NewContainer(4, 4, 2f);
            InventoryItemPlacement placement = source.TryAdd(
                m_kit.CreateInstance(m_kit.CreateItem(1, 1, unitWeight: 5f)), new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.WeightExceeded,
                InventoryRules.Instance.CanMove(source, target, placement, new Vector2Int(0, 0),
                    InventoryRotation.None));
        }

        [Test]
        public void Rules_CanRotate_CyclesThroughFourFacingsAndLandsBackOnNone()
        {
            InventoryContainer container = NewContainer(3, 3, float.MaxValue);
            InventoryItemPlacement placement = container.TryAdd(
                m_kit.CreateInstance(m_kit.CreateItem(1, 2)), new Vector2Int(0, 0));

            InventoryRotation expected = InventoryRotation.Rotate90;
            for (int round = 0; round < 4; round++)
            {
                Assert.AreEqual(InventoryMoveResult.Success,
                    InventoryRules.Instance.CanRotate(container, placement, out InventoryRotation next));
                Assert.AreEqual(expected, next);

                Assert.AreEqual(InventoryMoveResult.Success, container.TryRotate(placement));
                expected = (InventoryRotation)(((int)expected + 1) % 4);
            }

            Assert.AreEqual(InventoryRotation.None, placement.Rotation);
        }

        [Test]
        public void Rules_CanRotate_ReturnsOccupiedWhenNextFacingIsBlocked()
        {
            InventoryContainer container = NewContainer(3, 3, float.MaxValue);
            Assert.IsNotNull(container.TryAdd(m_kit.CreateInstance(m_kit.CreateItem(1, 1)), new Vector2Int(1, 0)));
            InventoryItemPlacement placement = container.TryAdd(
                m_kit.CreateInstance(m_kit.CreateItem(1, 2)), new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.Occupied,
                InventoryRules.Instance.CanRotate(container, placement, out InventoryRotation next));
            Assert.AreEqual(InventoryRotation.Rotate90, next);
            Assert.AreEqual(InventoryRotation.None, placement.Rotation);
        }

        [Test]
        public void Rules_CanRotate_ReturnsNotRotatableForConfiguredFixedItems()
        {
            InventoryContainer container = NewContainer(3, 3, float.MaxValue);
            InventoryItemPlacement placement = container.TryAdd(
                m_kit.CreateInstance(m_kit.CreateItem(1, 2, canRotate: false)), new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.NotRotatable,
                InventoryRules.Instance.CanRotate(container, placement, out InventoryRotation next));
            Assert.AreEqual(InventoryRotation.None, next);
        }

        [Test]
        public void MoveResultExtensions_ClassifyEveryOperationCode()
        {
            Assert.IsFalse(InventoryMoveResult.None.IsValid());

            Assert.IsTrue(InventoryMoveResult.Success.IsSuccessOrSame());
            Assert.IsTrue(InventoryMoveResult.SameSpot.IsSuccessOrSame());
            Assert.IsFalse(InventoryMoveResult.None.IsSuccessOrSame());

            Assert.IsTrue(InventoryMoveResult.OutOfBounds.IsSpatialFailure());
            Assert.IsTrue(InventoryMoveResult.Occupied.IsSpatialFailure());
            Assert.IsTrue(InventoryMoveResult.NotRotatable.IsRuleFailure());
            Assert.IsTrue(InventoryMoveResult.WeightExceeded.IsRuleFailure());
            Assert.IsTrue(InventoryMoveResult.SourceNotFound.IsDataDesync());
            Assert.IsTrue(InventoryMoveResult.OwnershipConflict.IsDataDesync());
            Assert.IsTrue(InventoryMoveResult.ConfigMissing.IsDataDesync());
        }

        #endregion
        #region 容器事务
        [Test]
        public void Container_TryAdd_CommitsVersionAndRaisesOnChanged()
        {
            InventoryContainer container = NewContainer(4, 4, float.MaxValue);
            int changedCount = 0;
            container.OnChanged += target => changedCount++;

            Assert.AreEqual(0, container.Version);
            Assert.IsNotNull(container.TryAdd(m_kit.CreateInstance(m_kit.CreateItem(2, 1)), new Vector2Int(0, 0)));

            Assert.AreEqual(1, container.Version);
            Assert.AreEqual(1, changedCount);
            Assert.AreEqual(1, container.Count);
            Assert.IsTrue(container.Contains(FirstPlacement(container)));
        }

        [Test]
        public void Container_FailedOperation_KeepsVersionAndStaysSilent()
        {
            InventoryContainer container = NewContainer(4, 4, float.MaxValue);
            int changedCount = 0;
            container.OnChanged += target => changedCount++;
            Assert.IsNotNull(container.TryAdd(m_kit.CreateInstance(m_kit.CreateItem(2, 2)), new Vector2Int(0, 0)));

            InventoryItemInstance overflow = m_kit.CreateInstance(m_kit.CreateItem(2, 2));
            Assert.IsNull(container.TryAdd(overflow, new Vector2Int(3, 3)));
            Assert.AreEqual(InventoryMoveResult.OutOfBounds,
                container.TryMoveTo(container, FirstPlacement(container), new Vector2Int(3, 3),
                    InventoryRotation.None));

            Assert.AreEqual(1, container.Version);
            Assert.AreEqual(1, changedCount);
        }

        [Test]
        public void Container_TryMoveTo_AcrossContainers_TransfersOwnershipAndCommitsBoth()
        {
            InventoryContainer source = NewContainer(4, 4, float.MaxValue);
            InventoryContainer target = NewContainer(2, 2, float.MaxValue);
            InventoryItemPlacement placement = source.TryAdd(
                m_kit.CreateInstance(m_kit.CreateItem(2, 1)), new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.Success,
                source.TryMoveTo(target, placement, new Vector2Int(0, 1), InventoryRotation.None));

            // 移出方在放入时已提交过一次事务, 移出事务让双方版本号各自 +1
            Assert.AreEqual(2, source.Version);
            Assert.AreEqual(1, target.Version);
            Assert.AreEqual(0, source.Count);
            Assert.AreEqual(1, target.Count);
            Assert.IsFalse(source.Contains(placement));
            Assert.IsTrue(target.Contains(placement));
            Assert.AreEqual(new Vector2Int(0, 1), placement.Position);
        }

        [Test]
        public void Container_TryRemove_RejectsForeignPlacementAndCommitsOwn()
        {
            InventoryContainer container = NewContainer(4, 4, float.MaxValue);
            InventoryContainer other = NewContainer(4, 4, float.MaxValue);
            InventoryItemPlacement foreign = other.TryAdd(m_kit.CreateInstance(m_kit.CreateItem(1, 1)), new Vector2Int(0, 0));
            InventoryItemPlacement mine = container.TryAdd(m_kit.CreateInstance(m_kit.CreateItem(1, 1)), new Vector2Int(0, 0));

            int versionBefore = container.Version;

            Assert.IsFalse(container.TryRemove(foreign));
            Assert.AreEqual(versionBefore, container.Version, "别人的放置记录不该让本容器提交事务");

            Assert.IsTrue(container.TryRemove(mine));
            Assert.IsFalse(container.TryRemove(mine));
            Assert.AreEqual(versionBefore + 1, container.Version);
            Assert.AreEqual(0, container.Count);
        }

        [Test]
        public void Container_TotalWeight_CountsStackCountOfEveryInstance()
        {
            InventoryContainer container = NewContainer(4, 4, 100f);
            Assert.IsNotNull(container.TryAdd(m_kit.CreateInstance(m_kit.CreateItem(1, 1, maxStackCount: 5, unitWeight: 2.5f), 3),
                new Vector2Int(0, 0)));
            Assert.IsNotNull(container.TryAdd(m_kit.CreateInstance(m_kit.CreateItem(1, 1, unitWeight: 1f)), new Vector2Int(1, 0)));

            Assert.AreEqual(8.5f, container.TotalWeight, 0.001f);
        }

        [Test]
        public void Container_TryGetPlacement_ResolvesByRuntimeInstance()
        {
            InventoryContainer container = NewContainer(4, 4, float.MaxValue);
            InventoryItemInstance instance = m_kit.CreateInstance(m_kit.CreateItem(2, 2));
            InventoryItemPlacement placement = container.TryAdd(instance, new Vector2Int(1, 1));
            InventoryItemInstance stranger = m_kit.CreateInstance(m_kit.CreateItem(1, 1));

            Assert.IsTrue(container.TryGetPlacement(instance, out InventoryItemPlacement found));
            Assert.AreSame(placement, found);
            Assert.IsFalse(container.TryGetPlacement(stranger, out _));
            Assert.IsTrue(container.ToString().Contains("[Grid 4x4, 16 cells, 1 placements]"));
        }

        #endregion

        #region 实例堆叠
        [Test]
        public void Instance_StackCount_ClampsToMaxStackOnConstruction()
        {
            InventoryItemInstance over = m_kit.CreateInstance(m_kit.CreateItem(1, 1, maxStackCount: 5, unitWeight: 3f), 99);
            InventoryItemInstance under = m_kit.CreateInstance(m_kit.CreateItem(1, 1, maxStackCount: 5), 0);

            Assert.AreEqual(5, over.StackCount);
            Assert.AreEqual(1, under.StackCount);
            Assert.AreEqual(15f, over.Weight, 0.001f);
        }

        [Test]
        public void Instance_TryAddStack_AcceptsOnlyTheRemainingSpace()
        {
            InventoryItemInstance stackable = m_kit.CreateInstance(m_kit.CreateItem(1, 1, maxStackCount: 5), 2);

            Assert.IsTrue(stackable.TryAddStack(10, out int accepted));
            Assert.AreEqual(3, accepted);
            Assert.AreEqual(5, stackable.StackCount);

            Assert.IsFalse(stackable.TryAddStack(1, out int full));
            Assert.AreEqual(0, full);

            InventoryItemInstance single = m_kit.CreateInstance(m_kit.CreateItem(1, 1), 1);
            Assert.IsFalse(single.TryAddStack(1, out _));
        }

        [Test]
        public void Instance_TryReduceStack_RejectsRemovingTheWholeStack()
        {
            InventoryItemInstance stackable = m_kit.CreateInstance(m_kit.CreateItem(1, 1, maxStackCount: 5), 3);

            Assert.IsTrue(stackable.TryReduceStack(2));
            Assert.AreEqual(1, stackable.StackCount);
            Assert.IsFalse(stackable.TryReduceStack(1));
            Assert.IsFalse(stackable.TryReduceStack(0));
        }

        #endregion

        #region 容器规格
        // 规格是纯数据载荷: 宽高与限重原样保存, 格子总数由宽高相乘派生
        [Test]
        public void ContainerSpec_KeepsGeometryAndDerivesCellCount()
        {
            InventoryContainerSpec spec = new InventoryContainerSpec(8, 5, 12f);

            Assert.AreEqual(8, spec.Width);
            Assert.AreEqual(5, spec.Height);
            Assert.AreEqual(12f, spec.WeightLimit, 0.001f);
            Assert.AreEqual(40, spec.CellCount);
        }

        // 容器完全由规格构造: 网格尺寸与限重都取自 spec, 散参入口已彻底移除
        [Test]
        public void Container_BuiltFromSpecAdoptsGridSizeAndWeightLimit()
        {
            InventoryContainer container = NewContainer(3, 7, 9f);

            Assert.AreEqual(3, container.Grid.Width);
            Assert.AreEqual(7, container.Grid.Height);
            Assert.AreEqual(9f, container.WeightLimit, 0.001f);
        }
        #endregion

        #region 背包总配置
        // 物品表是运行时唯一的登记入口: 表里的每件物品都要能被 ItemRegistry 查到
        [Test]
        public void InventoryConfig_RegisterItems_RegistersTheWholeTable()
        {
            InventoryItemConfigSO[] items =
            {
                m_kit.CreateItem(1, 1, register: false),
                m_kit.CreateItem(2, 1, register: false),
                m_kit.CreateItem(1, 3, register: false)
            };

            m_kit.CreateInventoryConfig(null, items).RegisterItems();

            foreach (InventoryItemConfigSO item in items)
            {
                Assert.IsTrue(ItemRegistry.TryGet(item.ItemId, out InventoryItemConfigSO found));
                Assert.AreSame(item, found);
            }
        }

        // 空表会让背包解析不出任何物品, 必须报错而不是静默返回
        [Test]
        public void InventoryConfig_RegisterItems_EmptyTableLogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("物品表为空"));

            m_kit.CreateInventoryConfig().RegisterItems();
        }

        // 表里夹一个空槽只报那一条, 后面的物品照常登记
        [Test]
        public void InventoryConfig_RegisterItems_NullSlotLogsErrorAndKeepsGoing()
        {
            InventoryItemConfigSO item = m_kit.CreateItem(1, 1, register: false);
            LogAssert.Expect(LogType.Error, new Regex("空槽"));

            m_kit.CreateInventoryConfig(null, new[] { null, item }).RegisterItems();

            Assert.IsTrue(ItemRegistry.TryGet(item.ItemId, out _));
        }

        // 底色只认已定义的档位, 冒出没档位的枚举要当场炸出来
        [Test]
        public void InventoryConfig_GetRarityColor_UndefinedRarityThrows()
        {
            InventoryConfigSO config = m_kit.CreateInventoryConfig();

            Assert.Throws<System.ArgumentOutOfRangeException>(() => config.GetRarityColor((InventoryItemRarity)99));
        }
        #endregion

        // 取容器里的第一个占位, 用例基本只放一件物品
        private static InventoryItemPlacement FirstPlacement(InventoryContainer container)
        {
            foreach (InventoryItemPlacement placement in container.Placements) return placement;

            return null;
        }

        // 造一个独立编号的容器, 避免用例之间串数据
        private InventoryContainer NewContainer(int width, int height, float weightLimit)
            => new InventoryContainer(m_kit.NewContainerId(), new InventoryContainerSpec(width, height, weightLimit));

        // 直接往网格里塞一个占位, 用于剥离规则单测几何逻辑
        private InventoryItemPlacement AddToGrid(InventoryGrid grid, InventoryItemConfigSO config, Vector2Int cell,
            InventoryRotation rotation = InventoryRotation.None)
        {
            InventoryItemPlacement placement = new InventoryItemPlacement(m_kit.CreateInstance(config), cell, rotation);
            grid.Add(placement);
            return placement;
        }
    }
}
