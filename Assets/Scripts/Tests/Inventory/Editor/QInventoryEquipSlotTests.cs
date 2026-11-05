/*
* ┌──────────────────────────────────┐
* │  描    述: 装备槽测试, 覆盖槽位注册、部位校验、单格收纳、穿戴锁与容量联动
* │  类    名: QInventoryEquipSlotTests.cs
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
    /// 装备槽测试：六个槽位随模型注册、只收对应部位的装备、大件压成单格、穿上容器装备才撑起容量网格
    /// </summary>
    public class QInventoryEquipSlotTests
    {
        private const int UNKNOWN_ITEM_ID = 999999;
        private const int SLOT_OCCUPY_WIDTH = 3;
        private const int SLOT_OCCUPY_HEIGHT = 3;

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private int m_bagId;

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();

            // 另开一张普通网格当"地上的临时容器", 与角色身上的固定号互不串味
            m_bagId = m_kit.NewContainerId();
            m_model.RegisterContainer(m_bagId,
                new InventoryContainerSpec(8, 5, float.MaxValue, null, "临时容器"));
        }

        [TearDown]
        public void TearDown()
        {
            m_model.OnUnregister();
            m_kit.Dispose();
        }

        #region 槽位注册
        [Test]
        public void OnRegister_BuildsSixOneCellEquipSlots()
        {
            int[] slotIds = InventoryContainerIds.EQUIP_SLOT_IDS;

            for (int i = 0; i < slotIds.Length; i++)
            {
                Assert.IsTrue(m_model.IsContainerRegistered(slotIds[i]), "装备槽应随模型注册：" + slotIds[i]);

                InventoryContainerVO vo = m_model.GetContainerVO(slotIds[i]);
                Assert.AreEqual(1, vo.Width);
                Assert.AreEqual(1, vo.Height);
                Assert.AreEqual(1, vo.CellCount);
                Assert.IsNotEmpty(vo.DisplayName, "槽位表头要有部位名：" + slotIds[i]);
                Assert.AreEqual(0, vo.Items.Count);
            }
        }

        [Test]
        public void IsContainerRegistered_KnowsWhichCapacitiesAreAbsent()
        {
            Assert.IsTrue(m_model.IsContainerRegistered(InventoryContainerIds.CHESTRIG_SLOT));
            Assert.IsFalse(m_model.IsContainerRegistered(InventoryContainerIds.RIG));
            Assert.IsFalse(m_model.IsContainerRegistered(m_kit.NewContainerId()));
        }
        #endregion

        #region 部位校验
        [Test]
        public void TryAddItem_IntoWrongEquipSlot_ReturnsSlotMismatch()
        {
            int rifleId = m_kit.CreateItem(5, 2, equipSlot: InventoryEquipSlot.PrimaryWeapon).ItemId;

            Assert.AreEqual(InventoryMoveResult.SlotMismatch,
                m_model.TryAddItem(InventoryContainerIds.CHESTRIG_SLOT, rifleId, 1, Vector2Int.zero));
            Assert.AreEqual(0, m_model.GetContainerVO(InventoryContainerIds.CHESTRIG_SLOT).UsedCellCount);
        }

        [Test]
        public void TryAddItem_PlainItemIsNotWearable_ReturnsSlotMismatch()
        {
            int medkitId = m_kit.CreateItem(1, 1).ItemId;

            Assert.AreEqual(InventoryMoveResult.SlotMismatch,
                m_model.TryAddItem(InventoryContainerIds.HELMET_SLOT, medkitId, 1, Vector2Int.zero));
        }

        [Test]
        public void TryAddItemAuto_MatchingRoleWearsIntoSlot()
        {
            int helmetId = m_kit.CreateItem(1, 1, equipSlot: InventoryEquipSlot.Helmet).ItemId;

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItemAuto(InventoryContainerIds.HELMET_SLOT, helmetId, 1));

            InventoryItemVO worn = m_model.GetContainerVO(InventoryContainerIds.HELMET_SLOT).Items[0];
            Assert.AreEqual(Vector2Int.zero, worn.Origin);
            Assert.AreEqual(1, worn.CellCount);
        }

        [Test]
        public void TryTransferItem_IntoWrongEquipSlot_ReturnsSlotMismatch()
        {
            int knifeId = m_kit.CreateItem(1, 2, equipSlot: InventoryEquipSlot.Melee).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_bagId, knifeId, 1, Vector2Int.zero));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_bagId, Vector2Int.zero, out int knifeInstanceId));

            Assert.AreEqual(InventoryMoveResult.SlotMismatch,
                m_model.TryTransferItem(m_bagId, InventoryContainerIds.BACKPACK_SLOT, knifeInstanceId, Vector2Int.zero));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_bagId, Vector2Int.zero, out _), "被拒的拖拽不该把物品弄丢");
        }
        #endregion

        #region 单格收纳
        [Test]
        public void EquipSlot_SnapsOversizedWeaponIntoOneCell()
        {
            int rifleId = m_kit.CreateItem(5, 2, equipSlot: InventoryEquipSlot.PrimaryWeapon).ItemId;

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItemAuto(InventoryContainerIds.PRIMARY_WEAPON_SLOT, rifleId, 1));

            InventoryItemVO worn = m_model.GetContainerVO(InventoryContainerIds.PRIMARY_WEAPON_SLOT).Items[0];
            Assert.AreEqual(Vector2Int.one, worn.Size, "大件穿进装备槽只占一格");
            Assert.AreEqual(1, m_model.GetContainerVO(InventoryContainerIds.PRIMARY_WEAPON_SLOT).UsedCellCount);
        }

        [Test]
        public void TryRotateItem_InsideEquipSlot_ReturnsNotRotatable()
        {
            int rigId = m_kit.CreateContainerItem(m_kit.CreateContainer(5, 3), SLOT_OCCUPY_WIDTH, SLOT_OCCUPY_HEIGHT,
                equipSlot: InventoryEquipSlot.ChestRig).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItemAuto(InventoryContainerIds.CHESTRIG_SLOT, rigId, 1));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(InventoryContainerIds.CHESTRIG_SLOT, Vector2Int.zero,
                out int rigInstanceId));

            Assert.AreEqual(InventoryMoveResult.NotRotatable,
                m_model.TryRotateItem(InventoryContainerIds.CHESTRIG_SLOT, rigInstanceId));
        }
        #endregion

        #region 穿戴锁
        [Test]
        public void TryTransferItem_OutOfEquipSlot_AllowsUnequip()
        {
            int backpackId = m_kit.CreateContainerItem(m_kit.CreateContainer(8, 5), SLOT_OCCUPY_WIDTH,
                SLOT_OCCUPY_HEIGHT, equipSlot: InventoryEquipSlot.Backpack).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItemAuto(InventoryContainerIds.BACKPACK_SLOT, backpackId, 1));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(InventoryContainerIds.BACKPACK_SLOT, Vector2Int.zero,
                out int wornInstanceId));

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(InventoryContainerIds.BACKPACK_SLOT, m_bagId, wornInstanceId, Vector2Int.zero));
            Assert.IsTrue(m_model.GetContainerVO(m_bagId).Contains(wornInstanceId));
        }

        [Test]
        public void TryTransferItem_UnequippedHelmet_RestoresConfiguredFootprint()
        {
            int helmetId = m_kit.CreateItem(2, 2, equipSlot: InventoryEquipSlot.Helmet).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItemAuto(InventoryContainerIds.HELMET_SLOT, helmetId, 1));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(InventoryContainerIds.HELMET_SLOT, Vector2Int.zero,
                out int helmetInstanceId));

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(InventoryContainerIds.HELMET_SLOT, m_bagId, helmetInstanceId,
                    Vector2Int.zero));

            InventoryContainerVO bag = m_model.GetContainerVO(m_bagId);
            Assert.IsTrue(bag.TryGetItem(helmetInstanceId, out InventoryItemVO item));
            Assert.AreEqual(new Vector2Int(2, 2), item.Size);
        }

        [Test]
        public void TryTransferItem_SwappingOutASlottedWeapon_AllowsWeaponSwap()
        {
            int wornId = m_kit.CreateItem(1, 1, equipSlot: InventoryEquipSlot.PrimaryWeapon).ItemId;
            int spareId = m_kit.CreateItem(1, 1, equipSlot: InventoryEquipSlot.PrimaryWeapon).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItemAuto(InventoryContainerIds.PRIMARY_WEAPON_SLOT, wornId, 1));
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_bagId, spareId, 1, Vector2Int.zero));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_bagId, Vector2Int.zero, out int spareInstanceId));

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(m_bagId, InventoryContainerIds.PRIMARY_WEAPON_SLOT, spareInstanceId,
                    Vector2Int.zero));
        }

        [Test]
        public void TryMoveItem_InsideEquipSlot_StillLocksTheWornItem()
        {
            int helmetId = m_kit.CreateItem(1, 1, equipSlot: InventoryEquipSlot.Helmet).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItemAuto(InventoryContainerIds.HELMET_SLOT, helmetId, 1));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(InventoryContainerIds.HELMET_SLOT, Vector2Int.zero,
                out int helmetInstanceId));

            // 同容器原地重复放置属于幂等放手, 不算卸下
            Assert.AreEqual(InventoryMoveResult.SameSpot,
                m_model.TryMoveItem(InventoryContainerIds.HELMET_SLOT, helmetInstanceId, Vector2Int.zero,
                    InventoryRotation.None));
        }
        #endregion

        #region 容量联动
        [Test]
        public void CapacityContainer_AppearsOnceTheRigIsWorn()
        {
            InventoryContainerConfigSO grid = m_kit.CreateContainer(5, 3);
            int rigId = m_kit.CreateContainerItem(grid, SLOT_OCCUPY_WIDTH, SLOT_OCCUPY_HEIGHT,
                equipSlot: InventoryEquipSlot.ChestRig).ItemId;

            Assert.IsFalse(m_model.IsContainerRegistered(InventoryContainerIds.RIG));

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItemAuto(InventoryContainerIds.CHESTRIG_SLOT, rigId, 1));

            InventoryContainerVO capacity = m_model.GetContainerVO(InventoryContainerIds.RIG);
            Assert.IsTrue(m_model.IsContainerRegistered(InventoryContainerIds.RIG));
            Assert.AreEqual(5, capacity.Width);
            Assert.AreEqual(3, capacity.Height);
            Assert.AreEqual(15, capacity.CellCount);
            Assert.AreEqual(grid.DisplayName, capacity.DisplayName, "容量表头取容器定义名而不是装备名");
        }

        [Test]
        public void CapacityContainer_DisappearsWhenSlottedEquipmentCarriesNoGrid()
        {
            InventoryModule.ApplyContainer(m_model, InventoryContainerIds.BACKPACK, m_kit.CreateContainer(8, 5));
            int bareId = m_kit.CreateItem(SLOT_OCCUPY_WIDTH, SLOT_OCCUPY_HEIGHT,
                equipSlot: InventoryEquipSlot.Backpack).ItemId;

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItemAuto(InventoryContainerIds.BACKPACK_SLOT, bareId, 1));

            Assert.IsFalse(m_model.IsContainerRegistered(InventoryContainerIds.BACKPACK), "槽里穿的装备不带容量, 网格不该留着");
        }

        [Test]
        public void Weapons_IntoTheirSlots_LeaveEveryCapacityClosed()
        {
            int rifleId = m_kit.CreateItem(5, 2, equipSlot: InventoryEquipSlot.PrimaryWeapon).ItemId;
            int pistolId = m_kit.CreateItem(1, 2, equipSlot: InventoryEquipSlot.SecondaryWeapon).ItemId;

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItemAuto(InventoryContainerIds.PRIMARY_WEAPON_SLOT, rifleId, 1));
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItemAuto(InventoryContainerIds.SECONDARY_WEAPON_SLOT, pistolId, 1));

            Assert.IsFalse(m_model.IsContainerRegistered(InventoryContainerIds.BACKPACK));
            Assert.IsFalse(m_model.IsContainerRegistered(InventoryContainerIds.RIG));
        }
        #endregion

        #region 视图联动
        [Test]
        public void ViewModel_UnregisteredCapacityStaysHiddenUntilTheRigIsWorn()
        {
            InventoryViewModel viewModel = new InventoryViewModel(m_model, InventoryContainerIds.RIG);
            viewModel.OnBind();

            try
            {
                Assert.IsFalse(viewModel.Visible.Value, "没穿胸挂时容量载体不该亮着");
                Assert.AreEqual(0, viewModel.SlotCount);

                int rigId = m_kit.CreateContainerItem(m_kit.CreateContainer(5, 3), SLOT_OCCUPY_WIDTH,
                    SLOT_OCCUPY_HEIGHT, equipSlot: InventoryEquipSlot.ChestRig).ItemId;
                Assert.AreEqual(InventoryMoveResult.Success,
                    m_model.TryAddItemAuto(InventoryContainerIds.CHESTRIG_SLOT, rigId, 1));

                Assert.IsTrue(viewModel.Visible.Value);
                Assert.AreEqual(15, viewModel.SlotCount);
            }
            finally
            {
                viewModel.OnUnbind();
                viewModel.OnDispose();
                viewModel.Dispose();
            }
        }

        [Test]
        public void ViewModel_EquipSlotExposesOneSelectableCell()
        {
            InventoryViewModel viewModel = new InventoryViewModel(m_model, InventoryContainerIds.BACKPACK_SLOT);
            viewModel.OnBind();

            try
            {
                Assert.AreEqual(1, viewModel.SlotCount);
                Assert.IsTrue(viewModel.Visible.Value);
                Assert.AreEqual(Vector2Int.zero, viewModel.GetSlot(0).Cell);

                int backpackId = m_kit.CreateContainerItem(m_kit.CreateContainer(8, 5), SLOT_OCCUPY_WIDTH,
                    SLOT_OCCUPY_HEIGHT, equipSlot: InventoryEquipSlot.Backpack).ItemId;
                Assert.AreEqual(InventoryMoveResult.Success,
                    m_model.TryAddItemAuto(InventoryContainerIds.BACKPACK_SLOT, backpackId, 1));

                viewModel.InteractAt(Vector2Int.zero);
                Assert.AreNotEqual(0, viewModel.SelectedInstanceId.Value);

                viewModel.RotateSelectedItem();
                Assert.IsFalse(viewModel.CanRotate.Value, "装备槽里的东西不转朝向");
            }
            finally
            {
                viewModel.OnUnbind();
                viewModel.OnDispose();
                viewModel.Dispose();
            }
        }
        #endregion

        #region 自动找位
        [Test]
        public void TryAddItemAuto_UnknownContainer_LogsErrorAndReturnsNone()
        {
            int missingContainerId = m_kit.NewContainerId();
            int itemId = m_kit.CreateItem(1, 1).ItemId;
            LogAssert.Expect(LogType.Error, new Regex("容器未注册"));

            Assert.AreEqual(InventoryMoveResult.None, m_model.TryAddItemAuto(missingContainerId, itemId, 1));
        }

        [Test]
        public void TryAddItemAuto_UnknownItem_LogsErrorAndReturnsConfigMissing()
        {
            LogAssert.Expect(LogType.Error, new Regex("物品配置未登记"));

            Assert.AreEqual(InventoryMoveResult.ConfigMissing, m_model.TryAddItemAuto(m_bagId, UNKNOWN_ITEM_ID, 1));
        }

        [Test]
        public void TryAddItemAuto_FullContainer_ReturnsOccupied()
        {
            int boxId = m_kit.NewContainerId();
            int tinyId = m_kit.CreateItem(1, 1).ItemId;
            m_model.RegisterContainer(boxId, new InventoryContainerSpec(2, 2, float.MaxValue));

            for (int i = 0; i < 4; i++)
                Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItemAuto(boxId, tinyId, 1));

            Assert.AreEqual(InventoryMoveResult.Occupied, m_model.TryAddItemAuto(boxId, tinyId, 1));
            Assert.AreEqual(4, m_model.GetContainerVO(boxId).UsedCellCount);
        }

        [Test]
        public void TryAddItemAuto_ScansFromTopLeftInReadingOrder()
        {
            int wideId = m_kit.CreateItem(3, 1).ItemId;
            int tinyId = m_kit.CreateItem(1, 1).ItemId;
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItemAuto(m_bagId, wideId, 1));

            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItemAuto(m_bagId, tinyId, 1));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_bagId, new Vector2Int(3, 0), out int tinyInstanceId));
            Assert.IsTrue(m_model.GetContainerVO(m_bagId).Contains(tinyInstanceId), "第二件应落在第一行剩下的空位");
        }
        #endregion
    }
}
