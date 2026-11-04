/*
* ┌──────────────────────────────────┐
* │  描    述: 容器数据驱动测试, 覆盖容器定义、物品容器引用、穿戴表解析与装配处注册链路
* │  类    名: QInventoryLoadoutTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Defines;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// 容器数据驱动测试：容器定义是网格身份, 物品通过引用它获得容器能力, 穿戴表按槽位解析出口, 装配处负责注册进模型
    /// </summary>
    public class QInventoryLoadoutTests
    {
        private QInventoryTestKit m_kit;
        private InventoryModel m_model;

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();
        }

        [TearDown]
        public void TearDown()
        {
            m_model.OnUnregister();
            m_kit.Dispose();
        }

        #region 容器定义与容器装备
        [Test]
        public void ContainerConfig_ExposesDisplayNameAndGeometry()
        {
            InventoryContainerConfigSO container = m_kit.CreateContainer(7, 3, "弹挂");

            Assert.AreEqual("弹挂", container.DisplayName);
            Assert.AreEqual(7, container.Width);
            Assert.AreEqual(3, container.Height);
        }

        [Test]
        public void ItemConfig_IsContainer_DerivesFromContainerReference()
        {
            InventoryItemConfigSO plainItem = m_kit.CreateItem(1, 1);
            InventoryContainerConfigSO grid = m_kit.CreateContainer(8, 5);
            InventoryItemConfigSO containerItem = m_kit.CreateContainerItem(grid);

            Assert.IsFalse(plainItem.IsContainer, "普通物品不该被认成容器");
            Assert.IsNull(plainItem.Container);
            Assert.IsTrue(containerItem.IsContainer, "挂了容器定义的物品就是容器");
            Assert.AreSame(grid, containerItem.Container);
        }

        [Test]
        public void ItemConfig_OccupancyAndCapacityAreIndependent()
        {
            InventoryContainerConfigSO grid = m_kit.CreateContainer(8, 5);
            InventoryItemConfigSO backpack = m_kit.CreateContainerItem(grid, 2, 2);

            // 占格是它躺在别人格子里的大小, 容量是它穿戴后撑起的网格, 两个数字互不污染
            Assert.AreEqual(new Vector2Int(2, 2), backpack.Size);
            Assert.AreEqual(8, backpack.Container.Width);
            Assert.AreEqual(5, backpack.Container.Height);
        }
        #endregion

        #region 穿戴表
        [Test]
        public void Loadout_TryGetContainer_MapsAllFourRoleSlots()
        {
            InventoryContainerConfigSO backpackGrid = m_kit.CreateContainer(8, 5);
            InventoryContainerConfigSO rigGrid = m_kit.CreateContainer(5, 3);
            InventoryContainerConfigSO pocketsGrid = m_kit.CreateContainer(4, 2);
            InventoryContainerConfigSO safeBoxGrid = m_kit.CreateContainer(3, 3);

            InventoryLoadoutConfigSO loadout = m_kit.CreateLoadout(
                m_kit.CreateContainerItem(backpackGrid),
                m_kit.CreateContainerItem(rigGrid),
                pocketsGrid,
                m_kit.CreateContainerItem(safeBoxGrid));

            int[] slotIds = InventoryContainerIds.ROLE_SLOT_IDS;
            InventoryContainerConfigSO[] expected = { backpackGrid, rigGrid, pocketsGrid, safeBoxGrid };

            for (int i = 0; i < slotIds.Length; i++)
            {
                Assert.IsTrue(loadout.TryGetContainer(slotIds[i], out InventoryContainerConfigSO container),
                    "角色槽位应能取到容器定义：" + slotIds[i]);
                Assert.AreSame(expected[i], container);
            }
        }

        [Test]
        public void Loadout_TryGetContainer_ResolvesThroughWornItem()
        {
            InventoryContainerConfigSO grid = m_kit.CreateContainer(10, 6);
            InventoryItemConfigSO backpack = m_kit.CreateContainerItem(grid);
            InventoryLoadoutConfigSO loadout = m_kit.CreateLoadout(backpack);

            Assert.IsTrue(loadout.TryGetContainer(InventoryContainerIds.BACKPACK, out InventoryContainerConfigSO resolved));
            Assert.AreSame(grid, resolved, "穿戴表应取到该物品挂着的容器定义");
        }

        [Test]
        public void Loadout_TryGetContainer_ReturnsFalseWhenWornItemIsNotAContainer()
        {
            InventoryItemConfigSO medkit = m_kit.CreateItem(1, 2);
            InventoryLoadoutConfigSO loadout = m_kit.CreateLoadout(medkit);

            Assert.IsFalse(loadout.TryGetContainer(InventoryContainerIds.BACKPACK, out InventoryContainerConfigSO container));
            Assert.IsNull(container, "往背包槽塞一件非容器装备不该解析出网格");
        }

        [Test]
        public void Loadout_TryGetContainer_ReturnsFalseForNonRoleSlot()
        {
            InventoryLoadoutConfigSO loadout = m_kit.CreateLoadout(
                m_kit.CreateContainerItem(m_kit.CreateContainer(8, 5)),
                m_kit.CreateContainerItem(m_kit.CreateContainer(5, 3)),
                m_kit.CreateContainer(4, 2),
                m_kit.CreateContainerItem(m_kit.CreateContainer(3, 3)));

            Assert.IsFalse(loadout.TryGetContainer(InventoryContainerIds.LOOT, out InventoryContainerConfigSO container));
            Assert.IsNull(container);
        }

        [Test]
        public void Loadout_TryGetContainer_ReturnsFalseWhenSlotIsEmpty()
        {
            InventoryLoadoutConfigSO loadout = m_kit.CreateLoadout(
                m_kit.CreateContainerItem(m_kit.CreateContainer(8, 5)));

            Assert.IsTrue(loadout.TryGetContainer(InventoryContainerIds.BACKPACK, out _));
            Assert.IsFalse(loadout.TryGetContainer(InventoryContainerIds.RIG, out _));
            Assert.IsFalse(loadout.TryGetContainer(InventoryContainerIds.POCKETS, out _));
            Assert.IsFalse(loadout.TryGetContainer(InventoryContainerIds.SAFEBOX, out _));
        }

        [Test]
        public void RoleSlotIds_ContainsFourDistinctCharacterSlots()
        {
            int[] slotIds = InventoryContainerIds.ROLE_SLOT_IDS;

            Assert.AreEqual(4, slotIds.Length);
            Assert.AreEqual(slotIds.Length, new HashSet<int>(slotIds).Count);
            Assert.IsTrue(Array.IndexOf(slotIds, InventoryContainerIds.BACKPACK) >= 0);
            Assert.IsTrue(Array.IndexOf(slotIds, InventoryContainerIds.RIG) >= 0);
            Assert.IsTrue(Array.IndexOf(slotIds, InventoryContainerIds.POCKETS) >= 0);
            Assert.IsTrue(Array.IndexOf(slotIds, InventoryContainerIds.SAFEBOX) >= 0);
            Assert.AreEqual(-1, Array.IndexOf(slotIds, InventoryContainerIds.LOOT));
        }
        #endregion

        #region 装配处注册
        [Test]
        public void ApplyLoadout_RegistersEachSlotWithItsOwnGeometry()
        {
            InventoryLoadoutConfigSO loadout = m_kit.CreateLoadout(
                m_kit.CreateContainerItem(m_kit.CreateContainer(8, 5)),
                m_kit.CreateContainerItem(m_kit.CreateContainer(5, 3)),
                m_kit.CreateContainer(4, 2),
                m_kit.CreateContainerItem(m_kit.CreateContainer(3, 3)));

            InventoryModule.ApplyLoadout(m_model, loadout);

            AssertContainerGeometry(InventoryContainerIds.BACKPACK, 8, 5, 40);
            AssertContainerGeometry(InventoryContainerIds.RIG, 5, 3, 15);
            AssertContainerGeometry(InventoryContainerIds.POCKETS, 4, 2, 8);
            AssertContainerGeometry(InventoryContainerIds.SAFEBOX, 3, 3, 9);
        }

        [Test]
        public void ApplyLoadout_EmptySlotLogsErrorButOthersStillRegister()
        {
            InventoryLoadoutConfigSO loadout = m_kit.CreateLoadout(
                m_kit.CreateContainerItem(m_kit.CreateContainer(8, 5)), null,
                m_kit.CreateContainer(4, 2),
                m_kit.CreateContainerItem(m_kit.CreateContainer(3, 3)));

            LogAssert.Expect(LogType.Error, new Regex("槽位未配置容器定义"));
            InventoryModule.ApplyLoadout(m_model, loadout);

            AssertContainerGeometry(InventoryContainerIds.BACKPACK, 8, 5, 40);
            AssertContainerGeometry(InventoryContainerIds.POCKETS, 4, 2, 8);
            AssertContainerGeometry(InventoryContainerIds.SAFEBOX, 3, 3, 9);

            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));
            Assert.AreEqual(0, m_model.GetContainerVO(InventoryContainerIds.RIG).CellCount);
        }

        [Test]
        public void ApplyLoadout_NonContainerEquipmentLogsError()
        {
            InventoryLoadoutConfigSO loadout = m_kit.CreateLoadout(
                m_kit.CreateItem(2, 3),
                m_kit.CreateContainerItem(m_kit.CreateContainer(5, 3)),
                m_kit.CreateContainer(4, 2),
                m_kit.CreateContainerItem(m_kit.CreateContainer(3, 3)));

            LogAssert.Expect(LogType.Error, new Regex("槽位未配置容器定义"));
            InventoryModule.ApplyLoadout(m_model, loadout);

            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));
            Assert.AreEqual(0, m_model.GetContainerVO(InventoryContainerIds.BACKPACK).CellCount);
            AssertContainerGeometry(InventoryContainerIds.RIG, 5, 3, 15);
        }

        [Test]
        public void ApplyLoadout_NullLoadoutLogsErrorAndRegistersNothing()
        {
            LogAssert.Expect(LogType.Error, new Regex("穿戴表为空"));
            InventoryModule.ApplyLoadout(m_model, null);

            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));
            Assert.AreEqual(0, m_model.GetContainerVO(InventoryContainerIds.BACKPACK).CellCount);
        }

        [Test]
        public void ApplyContainer_UsesConfigSizeAsContainerSpec()
        {
            InventoryModule.ApplyContainer(m_model, InventoryContainerIds.RIG, m_kit.CreateContainer(6, 4, "六级弹挂"));

            AssertContainerGeometry(InventoryContainerIds.RIG, 6, 4, 24);
        }

        [Test]
        public void ApplyContainer_NullContainerLogsErrorAndRegistersNothing()
        {
            LogAssert.Expect(LogType.Error, new Regex("容器定义为空"));
            InventoryModule.ApplyContainer(m_model, InventoryContainerIds.RIG, null);

            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));
            Assert.AreEqual(0, m_model.GetContainerVO(InventoryContainerIds.RIG).CellCount);
        }

        [Test]
        public void ViewModel_RebuildsGridWhenSlotIsResized()
        {
            int backpackSlot = InventoryContainerIds.BACKPACK;
            InventoryModule.ApplyContainer(m_model, backpackSlot, m_kit.CreateContainer(8, 5));

            InventoryViewModel viewModel = new InventoryViewModel(m_model, backpackSlot);
            viewModel.OnBind();

            Assert.AreEqual(40, viewModel.SlotCount);
            Assert.AreEqual(40, viewModel.Snapshot.Value.CellCount);

            // 换型号走先注销再注册, 注销瞬间快照为空, 注册新规格后网格按新拓扑重建
            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));
            m_model.UnregisterContainer(backpackSlot);
            InventoryModule.ApplyContainer(m_model, backpackSlot, m_kit.CreateContainer(10, 6));

            Assert.AreEqual(60, viewModel.SlotCount);
            Assert.AreEqual(60, viewModel.Snapshot.Value.CellCount);
            Assert.AreEqual(new Vector2Int(9, 5), viewModel.GetSlot(59).Cell);

            viewModel.OnUnbind();
            viewModel.OnDispose();
            viewModel.Dispose();
        }
        #endregion

        #region 断言辅助
        // 校验指定槽位在模型里注册出的网格尺寸与总格数
        private void AssertContainerGeometry(int slotId, int width, int height, int cellCount)
        {
            InventoryContainerVO vo = m_model.GetContainerVO(slotId);

            Assert.AreEqual(slotId, vo.ContainerId);
            Assert.AreEqual(width, vo.Width);
            Assert.AreEqual(height, vo.Height);
            Assert.AreEqual(cellCount, vo.CellCount);
        }
        #endregion
    }
}
