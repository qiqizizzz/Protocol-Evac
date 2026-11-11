/*
* ┌──────────────────────────────────┐
* │  描    述: 原子丢弃测试, 覆盖整堆搬运、按占格开箱与失败不留残箱
* │  类    名: QInventoryDropTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Text.RegularExpressions;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Defines;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// TryDropItem 测试：丢弃就是一次带临时容器的转移, 要么整堆搬走, 要么源容器和运行时容器号表一律不动
    /// </summary>
    public class QInventoryDropTests
    {
        //模型表示「没有容器号」的取值, 与 InventoryModel 内部常量对齐
        private const int INVALID_CONTAINER_ID = -1;

        //模型分配运行时容器号的起点, 用来验证失败的丢弃没留下残箱
        private const int RUNTIME_ID_START = InventoryContainerIds.LOOT;

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private int m_sourceId;

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();

            m_sourceId = m_kit.NewContainerId();
            m_model.RegisterContainer(m_sourceId,
                new InventoryContainerSpec(4, 4, float.MaxValue, null, "测试源箱"));
        }

        [TearDown]
        public void TearDown()
        {
            m_model.OnUnregister();
            m_kit.Dispose();
        }

        [Test]
        public void TryDropItem_StandingStack_MovesWholeStackToGroundContainer()
        {
            InventoryItemConfigSO ammo = m_kit.CreateItem(2, 3, true, 10, 1.5f);
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItemAuto(m_sourceId, ammo.ItemId, 4));

            int instanceId = m_model.GetContainerVO(m_sourceId).Items[0].InstanceId;
            InventoryMoveResult result = m_model.TryDropItem(m_sourceId, instanceId, out int groundContainerId);

            Assert.AreEqual(InventoryMoveResult.Success, result);
            Assert.IsTrue(m_model.IsContainerRegistered(groundContainerId), "丢弃要落到一只真实存在的地面容器");

            InventoryContainerVO ground = m_model.GetContainerVO(groundContainerId);
            Assert.AreEqual(1, ground.Items.Count);
            Assert.AreEqual(instanceId, ground.Items[0].InstanceId, "实例号要跟着走, 捡回来才知道是同一堆");
            Assert.AreEqual(4, ground.Items[0].StackCount, "整堆数量不许只带走一件");
            Assert.AreEqual(2, ground.Width);
            Assert.AreEqual(3, ground.Height);
            Assert.AreEqual(0, m_model.GetContainerVO(m_sourceId).Items.Count, "源箱要腾空");
        }

        [Test]
        public void TryDropItem_RotatedItem_KeepsRotationAndOpensRotatedFootprint()
        {
            InventoryItemConfigSO board = m_kit.CreateItem(1, 3, true);
            m_model.TryAddItemAuto(m_sourceId, board.ItemId, 1);

            int instanceId = m_model.GetContainerVO(m_sourceId).Items[0].InstanceId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryRotateItem(m_sourceId, instanceId));

            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryDropItem(m_sourceId, instanceId, out int groundContainerId));

            InventoryContainerVO ground = m_model.GetContainerVO(groundContainerId);
            Assert.AreEqual(InventoryRotation.Rotate90, ground.Items[0].Rotation, "丢出去不许把玩家的朝向转回来");
            Assert.AreEqual(3, ground.Width, "竖着的 1x3 转倒后要按 3x1 开地面容器");
            Assert.AreEqual(1, ground.Height);
        }

        [Test]
        public void TryDropItem_EquippedHelmet_GroundContainerUsesRealFootprint()
        {
            InventoryItemConfigSO helmet = m_kit.CreateItem(2, 1, false, 1, 1f, 0, InventoryItemRarity.None, null,
                InventoryEquipSlot.Helmet);
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItemAuto(InventoryContainerIds.HELMET_SLOT, helmet.ItemId, 1));

            int instanceId = m_model.GetContainerVO(InventoryContainerIds.HELMET_SLOT).Items[0].InstanceId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryDropItem(InventoryContainerIds.HELMET_SLOT, instanceId, out int groundContainerId));

            InventoryContainerVO ground = m_model.GetContainerVO(groundContainerId);
            Assert.AreEqual(2, ground.Width, "装备槽把头盔压成单格, 地面容器得按头盔真实占格开, 否则根本放不下");
            Assert.AreEqual(1, ground.Height);
            Assert.IsFalse(m_model.GetContainerVO(InventoryContainerIds.HELMET_SLOT).Contains(instanceId));
        }

        [Test]
        public void TryDropItem_MissingInstance_LeavesSourceUntouched()
        {
            InventoryItemConfigSO ration = m_kit.CreateItem(1, 1, true, 5);
            m_model.TryAddItemAuto(m_sourceId, ration.ItemId, 2);

            InventoryContainerVO before = m_model.GetContainerVO(m_sourceId);
            int survivor = before.Items[0].InstanceId;

            Assert.AreEqual(InventoryMoveResult.SourceNotFound,
                m_model.TryDropItem(m_sourceId, survivor + 5000, out int groundContainerId));

            Assert.AreEqual(INVALID_CONTAINER_ID, groundContainerId);
            InventoryContainerVO after = m_model.GetContainerVO(m_sourceId);
            Assert.AreEqual(1, after.Items.Count, "东西没丢成, 源箱一件都不许少");
            Assert.AreEqual(survivor, after.Items[0].InstanceId);
            Assert.AreEqual(2, after.Items[0].StackCount);
        }

        [Test]
        public void TryDropItem_FailedDrop_DoesNotLeakRuntimeContainerId()
        {
            Assert.AreEqual(InventoryMoveResult.SourceNotFound, m_model.TryDropItem(m_sourceId, 424242, out _));

            InventoryItemConfigSO tape = m_kit.CreateItem(1, 2);
            m_model.TryAddItemAuto(m_sourceId, tape.ItemId, 1);
            int instanceId = m_model.GetContainerVO(m_sourceId).Items[0].InstanceId;

            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryDropItem(m_sourceId, instanceId, out int groundContainerId));

            Assert.AreEqual(RUNTIME_ID_START, groundContainerId, "失败的丢弃要是漏了一只空箱, 容器号就会被白占走");
        }

        [Test]
        public void TryDropItem_UnregisteredSource_ReportsErrorAndDropsNothing()
        {
            int ghostContainerId = m_kit.NewContainerId();

            LogAssert.Expect(LogType.Error, new Regex("丢弃失败, 容器未注册"));

            Assert.AreEqual(InventoryMoveResult.None, m_model.TryDropItem(ghostContainerId, 1, out int groundContainerId));
            Assert.AreEqual(INVALID_CONTAINER_ID, groundContainerId);
        }

        [Test]
        public void TryDropItem_TwoDropsInARow_EachGetsItsOwnGroundContainer()
        {
            InventoryItemConfigSO knife = m_kit.CreateItem(1, 2, false);

            m_model.TryAddItemAuto(m_sourceId, knife.ItemId, 1);
            int firstInstanceId = m_model.GetContainerVO(m_sourceId).Items[0].InstanceId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryDropItem(m_sourceId, firstInstanceId, out int first));

            m_model.TryAddItemAuto(m_sourceId, knife.ItemId, 1);
            int secondInstanceId = m_model.GetContainerVO(m_sourceId).Items[0].InstanceId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryDropItem(m_sourceId, secondInstanceId, out int second));

            Assert.AreEqual(second, first + 1, "每丢一件就新起一只地面容器");
            Assert.AreEqual(firstInstanceId, m_model.GetContainerVO(first).Items[0].InstanceId,
                "前一件丢出去的东西不许被后一次丢弃顶掉");
            Assert.AreEqual(secondInstanceId, m_model.GetContainerVO(second).Items[0].InstanceId);
            Assert.AreEqual(0, m_model.GetContainerVO(m_sourceId).Items.Count);
        }
    }
}
