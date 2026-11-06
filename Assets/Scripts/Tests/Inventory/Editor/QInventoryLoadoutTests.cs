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

        // 常驻底格只在没穿型号时顶上, 穿了型号仍以型号网格为准
        [Test]
        public void Loadout_SafeBoxResolvesBaseGridOnlyWhenNoModelWorn()
        {
            InventoryContainerConfigSO baseGrid = m_kit.CreateContainer(3, 3, "安全箱");
            InventoryContainerConfigSO modelGrid = m_kit.CreateContainer(2, 2, "安全箱Lv2");
            InventoryItemConfigSO model = m_kit.CreateContainerItem(modelGrid, equipSlot: InventoryEquipSlot.SafeBox);

            InventoryLoadoutConfigSO bare = m_kit.CreateLoadout(null, null, null, null, baseGrid);
            Assert.IsTrue(bare.TryGetContainer(InventoryContainerIds.SAFEBOX, out InventoryContainerConfigSO fallback));
            Assert.AreSame(baseGrid, fallback, "没穿型号时安全箱该解析到常驻底格");
            Assert.IsTrue(bare.TryGetPersistentContainer(InventoryContainerIds.SAFEBOX, out _));

            InventoryLoadoutConfigSO worn = m_kit.CreateLoadout(null, null, null, model, baseGrid);
            Assert.IsTrue(worn.TryGetContainer(InventoryContainerIds.SAFEBOX, out InventoryContainerConfigSO fromModel));
            Assert.AreSame(modelGrid, fromModel, "穿了型号就以型号网格为准");

            Assert.IsFalse(m_kit.CreateLoadout(null).TryGetPersistentContainer(InventoryContainerIds.SAFEBOX, out _),
                "没配常驻底格时安全箱不常驻");
            Assert.IsFalse(bare.TryGetPersistentContainer(InventoryContainerIds.BACKPACK, out _), "背包不是常驻槽位");
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
                m_kit.CreateContainerItem(m_kit.CreateContainer(8, 5), equipSlot: InventoryEquipSlot.Backpack),
                m_kit.CreateContainerItem(m_kit.CreateContainer(5, 3), equipSlot: InventoryEquipSlot.ChestRig),
                m_kit.CreateContainer(4, 2),
                m_kit.CreateContainerItem(m_kit.CreateContainer(3, 3)));

            InventoryModule.ApplyLoadout(m_model, loadout);

            Assert.IsTrue(m_model.IsContainerRegistered(InventoryContainerIds.BACKPACK_SLOT));
            Assert.IsTrue(m_model.IsContainerRegistered(InventoryContainerIds.CHESTRIG_SLOT));
            AssertContainerGeometry(InventoryContainerIds.BACKPACK, 8, 5, 40);
            AssertContainerGeometry(InventoryContainerIds.RIG, 5, 3, 15);
            AssertContainerGeometry(InventoryContainerIds.POCKETS, 4, 2, 8);
            AssertContainerGeometry(InventoryContainerIds.SAFEBOX, 3, 3, 9);
        }

        // 没穿胸挂是常态而非配置事故, 装配处不该报错, 胸挂容量也不该被注册出来
        [Test]
        public void ApplyLoadout_EmptyRigSlotRegistersNothingAndKeepsTheRest()
        {
            InventoryLoadoutConfigSO loadout = m_kit.CreateLoadout(
                m_kit.CreateContainerItem(m_kit.CreateContainer(8, 5), equipSlot: InventoryEquipSlot.Backpack), null,
                m_kit.CreateContainer(4, 2),
                m_kit.CreateContainerItem(m_kit.CreateContainer(3, 3)));

            InventoryModule.ApplyLoadout(m_model, loadout);

            Assert.IsFalse(m_model.IsContainerRegistered(InventoryContainerIds.RIG));
            AssertContainerGeometry(InventoryContainerIds.BACKPACK, 8, 5, 40);
            AssertContainerGeometry(InventoryContainerIds.POCKETS, 4, 2, 8);
            AssertContainerGeometry(InventoryContainerIds.SAFEBOX, 3, 3, 9);
        }

        [Test]
        public void ApplyLoadout_NonContainerEquipmentLogsError()
        {
            InventoryLoadoutConfigSO loadout = m_kit.CreateLoadout(
                m_kit.CreateItem(2, 3),
                m_kit.CreateContainerItem(m_kit.CreateContainer(5, 3), equipSlot: InventoryEquipSlot.ChestRig),
                m_kit.CreateContainer(4, 2),
                m_kit.CreateContainerItem(m_kit.CreateContainer(3, 3)));

            LogAssert.Expect(LogType.Error, new Regex("背包穿戴的物品不是容器装备"));
            InventoryModule.ApplyLoadout(m_model, loadout);

            Assert.IsFalse(m_model.IsContainerRegistered(InventoryContainerIds.BACKPACK));
            AssertContainerGeometry(InventoryContainerIds.RIG, 5, 3, 15);
        }

        // 穿戴表把部位配错, 装备槽按部位拒收, 装配处把失败原因报出来
        [Test]
        public void ApplyLoadout_WrongEquipSlotLogsWearFailure()
        {
            InventoryItemConfigSO helmetBackpack = m_kit.CreateContainerItem(m_kit.CreateContainer(8, 5),
                equipSlot: InventoryEquipSlot.Helmet);
            InventoryLoadoutConfigSO loadout = m_kit.CreateLoadout(helmetBackpack, null,
                m_kit.CreateContainer(4, 2),
                m_kit.CreateContainerItem(m_kit.CreateContainer(3, 3)));

            LogAssert.Expect(LogType.Error, new Regex("背包穿戴失败"));
            InventoryModule.ApplyLoadout(m_model, loadout);

            Assert.IsFalse(m_model.IsContainerRegistered(InventoryContainerIds.BACKPACK));
            Assert.AreEqual(0, m_model.GetContainerVO(InventoryContainerIds.BACKPACK_SLOT).Items.Count);
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

        #region 安全箱常驻与换型
        // 常驻判定只认安全箱: 胸挂与背包卸下型号后仍该整块注销
        [Test]
        public void ContainerIds_SafeBoxIsTheOnlyPersistentCapacityContainer()
        {
            Assert.IsTrue(InventoryContainerIds.IsPersistentCapacityContainer(InventoryContainerIds.SAFEBOX));
            Assert.IsFalse(InventoryContainerIds.IsPersistentCapacityContainer(InventoryContainerIds.BACKPACK));
            Assert.IsFalse(InventoryContainerIds.IsPersistentCapacityContainer(InventoryContainerIds.RIG));
            Assert.IsFalse(InventoryContainerIds.IsPersistentCapacityContainer(InventoryContainerIds.POCKETS));

            Assert.IsTrue(InventoryContainerIds.TryGetCapacityContainerId(InventoryEquipSlot.SafeBox, out int safeBoxId));
            Assert.AreEqual(InventoryContainerIds.SAFEBOX, safeBoxId);
        }

        // 卸下型号不等于拆掉安全箱: 网格回落到常驻底格, 重新穿上型号又换回型号尺寸
        [Test]
        public void SafeBox_PersistentBaseGridSurvivesModelUnequipAndReEquip()
        {
            InventoryContainerConfigSO baseGrid = m_kit.CreateContainer(3, 3, "安全箱");
            InventoryContainerConfigSO modelGrid = m_kit.CreateContainer(2, 2, "安全箱Lv2");
            InventoryItemConfigSO model = m_kit.CreateContainerItem(modelGrid, 2, 2, equipSlot: InventoryEquipSlot.SafeBox);
            InventoryLoadoutConfigSO loadout = m_kit.CreateLoadout(null, null, m_kit.CreateContainer(4, 2, "口袋"),
                model, baseGrid);

            RebuildModelWithLoadout(loadout);
            InventoryModule.ApplyLoadout(m_model, loadout);
            AssertContainerGeometry(InventoryContainerIds.SAFEBOX, 2, 2, 4);

            int pockets = InventoryContainerIds.POCKETS;
            int wornId = m_model.GetContainerVO(InventoryContainerIds.SAFEBOX_SLOT).Items[0].InstanceId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(InventoryContainerIds.SAFEBOX_SLOT, pockets, wornId, Vector2Int.zero));

            Assert.IsTrue(m_model.IsContainerRegistered(InventoryContainerIds.SAFEBOX), "卸下型号后安全箱网格仍常驻");
            AssertContainerGeometry(InventoryContainerIds.SAFEBOX, 3, 3, 9);
            Assert.AreEqual("安全箱", m_model.GetContainerVO(InventoryContainerIds.SAFEBOX).DisplayName, "表头名跟着常驻底格走");

            // 再穿回型号, 常驻底格让位给型号网格
            int pocketId = m_model.GetContainerVO(pockets).Items[0].InstanceId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(pockets, InventoryContainerIds.SAFEBOX_SLOT, pocketId, Vector2Int.zero));
            AssertContainerGeometry(InventoryContainerIds.SAFEBOX, 2, 2, 4);
        }

        // 换大: 拓扑重建但实例号不变, 原位放得下的物品留在原位
        [Test]
        public void TryResizeContainer_BiggerGridKeepsItemsAndInstanceIds()
        {
            int safeBox = InventoryContainerIds.SAFEBOX;
            InventoryModule.ApplyContainer(m_model, safeBox, m_kit.CreateContainer(3, 3, "安全箱"));
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItem(safeBox, m_kit.CreateItem(2, 1).ItemId, 1, new Vector2Int(0, 2)));

            int instanceId = m_model.GetContainerVO(safeBox).Items[0].InstanceId;
            Assert.IsTrue(m_model.TryResizeContainer(safeBox,
                new InventoryContainerSpec(4, 4, float.MaxValue, null, "安全箱")));

            AssertContainerGeometry(safeBox, 4, 4, 16);
            Assert.AreEqual(1, m_model.GetContainerVO(safeBox).Items.Count);
            Assert.AreEqual(instanceId, m_model.GetContainerVO(safeBox).Items[0].InstanceId, "换尺寸不该重造实例");
            Assert.AreEqual(new Vector2Int(0, 2), m_model.GetContainerVO(safeBox).Items[0].Origin, "原位放得下就该留在原位");
        }

        // 换小且塞不下: 整次回滚, 拓扑与内容都保持原样并把问题报出来
        [Test]
        public void TryResizeContainer_TooSmallForContentsRollsBackToOriginalGeometry()
        {
            int safeBox = InventoryContainerIds.SAFEBOX;
            InventoryModule.ApplyContainer(m_model, safeBox, m_kit.CreateContainer(3, 3, "安全箱"));
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItem(safeBox, m_kit.CreateItem(2, 2).ItemId, 1, new Vector2Int(0, 1)));

            int instanceId = m_model.GetContainerVO(safeBox).Items[0].InstanceId;
            LogAssert.Expect(LogType.Error, new Regex("容器换尺寸放不下现有内容"));
            Assert.IsFalse(m_model.TryResizeContainer(safeBox,
                new InventoryContainerSpec(1, 1, float.MaxValue, null, "安全箱")));

            AssertContainerGeometry(safeBox, 3, 3, 9);
            Assert.IsTrue(m_model.IsContainerRegistered(safeBox), "回滚后网格必须还在");
            Assert.AreEqual(instanceId, m_model.GetContainerVO(safeBox).Items[0].InstanceId, "回滚不该重造实例");
            Assert.AreEqual(new Vector2Int(0, 1), m_model.GetContainerVO(safeBox).Items[0].Origin, "回滚后物品该回到原位");
        }

        // 规格与现有拓扑完全一致时不换尺寸, 免得白吃一次重建
        [Test]
        public void TryResizeContainer_SameShapeIsNoOp()
        {
            int safeBox = InventoryContainerIds.SAFEBOX;
            InventoryModule.ApplyContainer(m_model, safeBox, m_kit.CreateContainer(3, 3, "安全箱"));
            int version = m_model.GetContainerVO(safeBox).Version;

            Assert.IsTrue(m_model.TryResizeContainer(safeBox,
                new InventoryContainerSpec(3, 3, float.MaxValue, null, "安全箱")));
            Assert.AreEqual(version, m_model.GetContainerVO(safeBox).Version, "没换尺寸不该动版本");
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

        // 换成带穿戴表的总配置重建模型, SyncWornCapacities 才可能从配置里读到常驻底格
        private void RebuildModelWithLoadout(InventoryLoadoutConfigSO loadout)
        {
            m_model.OnUnregister();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig(loadout));
            m_model.OnRegister();
        }
        #endregion
    }
}
