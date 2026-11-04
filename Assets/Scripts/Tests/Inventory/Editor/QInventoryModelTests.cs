/*
* ┌──────────────────────────────────┐
* │  描    述: 背包模型层测试, 覆盖容器注册注销、快照装配与对外操作契约
* │  类    名: QInventoryModelTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// InventoryModel 测试：容器注册幂等、快照字段、操作码回传与变更广播
    /// </summary>
    public class QInventoryModelTests
    {
        private const int UNKNOWN_ITEM_ID = 999999;

        private readonly List<int> m_changedIds = new List<int>();

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private int m_containerId;

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel();
            m_model.OnRegister();

            m_containerId = m_kit.NewContainerId();
            m_model.RegisterContainer(m_containerId, new InventoryContainerSpec(4, 4, float.MaxValue));
            // NUnit 复用同一个夹具实例, 广播记录必须在每个用例开头清零
            m_changedIds.Clear();
            m_model.OnChanged += HandleChanged;
        }

        [TearDown]
        public void TearDown()
        {
            m_model.OnChanged -= HandleChanged;
            m_model.OnUnregister();
            m_kit.Dispose();
        }

        // 记录模型广播过的容器号
        private void HandleChanged(int containerId) => m_changedIds.Add(containerId);

        #region 容器注册
        [Test]
        public void RegisterContainer_DuplicateRequest_KeepsTheFirstTopology()
        {
            m_model.RegisterContainer(m_containerId, new InventoryContainerSpec(8, 8, 1f));

            InventoryContainerVO vo = m_model.GetContainerVO(m_containerId);
            Assert.AreEqual(4, vo.Width);
            Assert.AreEqual(4, vo.Height);
            Assert.AreEqual(16, vo.CellCount);
            Assert.AreEqual(0, vo.Version);
            Assert.IsEmpty(vo.Items);
        }

        [Test]
        public void UnregisterContainer_RaisesOnChangedAndDropsTheContainer()
        {
            m_model.UnregisterContainer(m_containerId);

            Assert.AreEqual(1, m_changedIds.Count);
            Assert.AreEqual(m_containerId, m_changedIds[0]);

            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));
            Assert.AreSame(InventoryContainerVO.Empty, m_model.GetContainerVO(m_containerId));
        }

        [Test]
        public void OnUnregister_ClosesEveryContainer()
        {
            int otherContainerId = m_kit.NewContainerId();
            m_model.RegisterContainer(otherContainerId, new InventoryContainerSpec(2, 2, float.MaxValue));

            m_model.OnChanged -= HandleChanged;
            m_model.OnUnregister();

            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));
            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));
            Assert.AreEqual(0, m_model.GetContainerVO(m_containerId).CellCount);
            Assert.AreEqual(0, m_model.GetContainerVO(otherContainerId).CellCount);
        }
        #endregion

        #region 放入物品
        [Test]
        public void TryAddItem_UnknownItemId_ReturnsConfigMissing()
        {
            LogAssert.Expect(LogType.Error, new Regex("物品配置未登记"));

            Assert.AreEqual(InventoryMoveResult.ConfigMissing,
                m_model.TryAddItem(m_containerId, UNKNOWN_ITEM_ID, 1, Vector2Int.zero));
            Assert.AreEqual(0, m_changedIds.Count);
        }

        [Test]
        public void TryAddItem_UnknownContainer_ReturnsNone()
        {
            int missingContainerId = m_kit.NewContainerId();
            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));

            Assert.AreEqual(InventoryMoveResult.None,
                m_model.TryAddItem(missingContainerId, m_kit.CreateItem(1, 1).ItemId, 1, Vector2Int.zero));
        }

        [Test]
        public void TryAddItem_Success_AssemblesSnapshotGeometryAndValue()
        {
            int itemId = m_kit.CreateItem(2, 1, maxStackCount: 5, unitValue: 250).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItem(m_containerId, itemId, 3, new Vector2Int(1, 2)));

            InventoryContainerVO vo = m_model.GetContainerVO(m_containerId);
            Assert.AreEqual(1, vo.Items.Count);
            Assert.AreEqual(1, m_changedIds.Count);
            Assert.AreEqual(1, vo.Version);
            Assert.AreEqual(16, vo.CellCount);
            Assert.AreEqual(2, vo.UsedCellCount);

            InventoryItemVO item = vo.Items[0];
            Assert.AreEqual(itemId, item.ItemId);
            Assert.AreEqual(3, item.StackCount);
            Assert.AreEqual(new Vector2Int(1, 2), item.Origin);
            Assert.AreEqual(new Vector2Int(2, 1), item.Size);
            Assert.AreEqual(2, item.CellCount);
            Assert.AreEqual(InventoryRotation.None, item.Rotation);
            Assert.AreEqual(250, item.UnitValue);
            Assert.AreEqual(750, item.TotalValue);
            Assert.AreEqual("TEST_" + itemId, item.DisplayName);
            Assert.IsTrue(vo.Contains(item.InstanceId));
            Assert.IsTrue(vo.TryGetItem(item.InstanceId, out InventoryItemVO indexed));
            Assert.AreEqual(itemId, indexed.ItemId);
        }

        [Test]
        public void TryAddItem_OutOfBounds_LeavesContainerAndVersionUntouched()
        {
            int itemId = m_kit.CreateItem(3, 3).ItemId;

            Assert.AreEqual(InventoryMoveResult.OutOfBounds,
                m_model.TryAddItem(m_containerId, itemId, 1, new Vector2Int(2, 2)));
            Assert.AreEqual(0, m_model.GetContainerVO(m_containerId).UsedCellCount);
            Assert.AreEqual(0, m_changedIds.Count);
        }

        [Test]
        public void TryAddItem_OverWeightLimit_ReturnsWeightExceeded()
        {
            int heavyContainerId = m_kit.NewContainerId();
            m_model.RegisterContainer(heavyContainerId, new InventoryContainerSpec(4, 4, 3f));
            int itemId = m_kit.CreateItem(1, 1, unitWeight: 5f).ItemId;

            Assert.AreEqual(InventoryMoveResult.WeightExceeded,
                m_model.TryAddItem(heavyContainerId, itemId, 1, Vector2Int.zero));
            int lightItemId = m_kit.CreateItem(1, 1, unitWeight: 2.5f).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItem(heavyContainerId, lightItemId, 1, Vector2Int.zero));
            Assert.AreEqual(InventoryMoveResult.WeightExceeded,
                m_model.TryAddItem(heavyContainerId, lightItemId, 1, Vector2Int.one));
        }
        #endregion
        #region 移动与旋转
        [Test]
        public void TryMoveItem_UnknownInstance_ReturnsSourceNotFound()
        {
            Assert.AreEqual(InventoryMoveResult.SourceNotFound,
                m_model.TryMoveItem(m_containerId, -777, Vector2Int.zero, InventoryRotation.None));
            Assert.AreEqual(0, m_changedIds.Count);
        }

        [Test]
        public void TryMoveItem_AppliesRequestedRotationTogetherWithPosition()
        {
            int itemId = m_kit.CreateItem(1, 3).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_containerId, itemId, 1, Vector2Int.zero));

            int instanceId = ResolveInstanceId(Vector2Int.zero);
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryMoveItem(m_containerId, instanceId, new Vector2Int(0, 1), InventoryRotation.Rotate90));

            InventoryItemVO item = OnlyItem(m_model.GetContainerVO(m_containerId));
            Assert.AreEqual(new Vector2Int(0, 1), item.Origin);
            Assert.AreEqual(InventoryRotation.Rotate90, item.Rotation);
            Assert.AreEqual(new Vector2Int(3, 1), item.Size);
            Assert.AreEqual(2, m_changedIds.Count);
        }

        [Test]
        public void TryMoveItem_SameSpot_IsAcceptedSilentlyWithoutNewVersion()
        {
            int itemId = m_kit.CreateItem(2, 2).ItemId;
            m_model.TryAddItem(m_containerId, itemId, 1, new Vector2Int(1, 1));
            int instanceId = ResolveInstanceId(new Vector2Int(1, 1));
            int versionBefore = m_model.GetContainerVO(m_containerId).Version;

            Assert.AreEqual(InventoryMoveResult.SameSpot,
                m_model.TryMoveItem(m_containerId, instanceId, new Vector2Int(1, 1), InventoryRotation.None));
            Assert.AreEqual(versionBefore, m_model.GetContainerVO(m_containerId).Version);
            Assert.AreEqual(1, m_changedIds.Count);
        }

        [Test]
        public void TryRotateItem_CyclesFacingAndCommitsSnapshot()
        {
            int itemId = m_kit.CreateItem(2, 1).ItemId;
            m_model.TryAddItem(m_containerId, itemId, 1, Vector2Int.zero);
            int instanceId = ResolveInstanceId(Vector2Int.zero);

            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryRotateItem(m_containerId, instanceId));
            Assert.AreEqual(InventoryRotation.Rotate90, OnlyItem(m_model.GetContainerVO(m_containerId)).Rotation);

            for (int round = 0; round < 3; round++)
                Assert.AreEqual(InventoryMoveResult.Success, m_model.TryRotateItem(m_containerId, instanceId));

            Assert.AreEqual(InventoryRotation.None, OnlyItem(m_model.GetContainerVO(m_containerId)).Rotation);
            Assert.AreEqual(5, m_model.GetContainerVO(m_containerId).Version);
        }

        [Test]
        public void TryRotateItem_UnknownInstance_ReturnsSourceNotFound()
        {
            Assert.AreEqual(InventoryMoveResult.SourceNotFound, m_model.TryRotateItem(m_containerId, -777));
        }

        [Test]
        public void TryRotateItem_NextFacingBlocked_KeepsOriginalFacing()
        {
            int blockerId = m_kit.CreateItem(1, 1).ItemId;
            int armId = m_kit.CreateItem(1, 2).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_containerId, blockerId, 1, new Vector2Int(1, 0)));
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_containerId, armId, 1, Vector2Int.zero));

            int armInstanceId = ResolveInstanceId(Vector2Int.zero);
            Assert.AreEqual(InventoryMoveResult.Occupied, m_model.TryRotateItem(m_containerId, armInstanceId));

            InventoryContainerVO vo = m_model.GetContainerVO(m_containerId);
            Assert.IsTrue(vo.TryGetItem(armInstanceId, out InventoryItemVO arm));
            Assert.AreEqual(InventoryRotation.None, arm.Rotation);
        }
        #endregion

        #region 查询与广播
        [Test]
        public void TryGetInstanceIdAt_ResolvesEveryCellOfTheFootprint()
        {
            int itemId = m_kit.CreateItem(2, 2).ItemId;
            m_model.TryAddItem(m_containerId, itemId, 1, new Vector2Int(1, 1));

            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, new Vector2Int(1, 1), out int corner));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, new Vector2Int(2, 2), out int opposite));
            Assert.AreEqual(corner, opposite);

            Assert.IsFalse(m_model.TryGetInstanceIdAt(m_containerId, Vector2Int.zero, out _));
            Assert.IsFalse(m_model.TryGetInstanceIdAt(m_containerId, new Vector2Int(9, 9), out _));
        }

        [Test]
        public void TryGetInstanceIdAt_UnknownContainer_ReportsError()
        {
            int missingContainerId = m_kit.NewContainerId();
            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));

            Assert.IsFalse(m_model.TryGetInstanceIdAt(missingContainerId, Vector2Int.zero, out int instanceId));
            Assert.AreEqual(0, instanceId);
        }

        [Test]
        public void OnChanged_CarriesOnlyTheTouchedContainer()
        {
            int otherContainerId = m_kit.NewContainerId();
            m_model.RegisterContainer(otherContainerId, new InventoryContainerSpec(2, 2, float.MaxValue));
            int itemId = m_kit.CreateItem(1, 1).ItemId;

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItem(otherContainerId, itemId, 1, Vector2Int.zero));

            Assert.AreEqual(1, m_changedIds.Count);
            Assert.AreEqual(otherContainerId, m_changedIds[0]);
        }
        #endregion

        // 按格子反查实例号, 模型不吐 InstanceId 之外的引用
        private int ResolveInstanceId(Vector2Int cell)
        {
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, cell, out int instanceId));
            return instanceId;
        }

        // 取快照里唯一的那件物品
        private static InventoryItemVO OnlyItem(InventoryContainerVO vo)
        {
            Assert.AreEqual(1, vo.Items.Count);
            return vo.Items[0];
        }
    }
}
