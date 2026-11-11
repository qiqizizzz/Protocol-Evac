/*
* ┌──────────────────────────────────┐
* │  描    述: 丢弃交互测试, 覆盖左键双击判定、菜单丢弃请求与地面登记链路
* │  类    名: QItemDropInteractionTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Framework.QTower.MVVM.ViewModel;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UI.Inventory;
using UI.Inventory.View.Tools;
using UnityEngine;

namespace Tests.Inventory
{
    /// <summary>
    /// 丢弃交互测试：菜单与双击共用一条链路, 判定器只认同一实例上的两次近距离左键, 登记只在模型真丢成功时发生
    /// </summary>
    public class QItemDropInteractionTests
    {
        private const int NO_ITEM = -1;
        private const float INTERVAL = 0.3f;

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private GroundItemModel m_groundModel;
        private int m_sourceId;
        private readonly List<ViewModelBase> m_popups = new List<ViewModelBase>();

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();
            m_groundModel = new GroundItemModel();
            m_groundModel.OnRegister();

            m_sourceId = m_kit.NewContainerId();
            m_model.RegisterContainer(m_sourceId,
                new InventoryContainerSpec(4, 4, float.MaxValue, null, "测试源箱"));
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = m_popups.Count - 1; i >= 0; i--)
                DisposeViewModel(m_popups[i]);
            m_popups.Clear();

            m_groundModel.OnUnregister();
            m_model.OnUnregister();
            m_kit.Dispose();
        }

        #region 双击判定
        [Test]
        public void DoubleClickDetector_SameItemWithinThreshold_ReturnsDoubleClick()
        {
            DoubleClickDetector detector = new DoubleClickDetector();

            Assert.IsTrue(detector.TryClick(7, 10f, out bool firstIsDouble));
            Assert.IsFalse(firstIsDouble, "首次点击没有可配对的上一击");

            Assert.IsTrue(detector.TryClick(7, 10f + INTERVAL * 0.5f, out bool secondIsDouble));
            Assert.IsTrue(secondIsDouble, "同一件物品间隔足够短就是双击");
        }

        [Test]
        public void DoubleClickDetector_DifferentItem_DoesNotDoubleClick()
        {
            DoubleClickDetector detector = new DoubleClickDetector();

            detector.TryClick(7, 10f, out _);
            Assert.IsTrue(detector.TryClick(8, 10.05f, out bool isDouble));
            Assert.IsFalse(isDouble, "换了物品就要重新按首次点击算");

            // 上一次落在 8 号上, 立刻点 7 号仍然不算双击
            Assert.IsTrue(detector.TryClick(7, 10.1f, out isDouble));
            Assert.IsFalse(isDouble);
        }

        [Test]
        public void DoubleClickDetector_OutsideThreshold_DoesNotDoubleClick()
        {
            DoubleClickDetector detector = new DoubleClickDetector();

            detector.TryClick(7, 10f, out _);
            Assert.IsTrue(detector.TryClick(7, 10f + INTERVAL + 0.1f, out bool isDouble));
            Assert.IsFalse(isDouble, "间隔超出阈值只算新的首次点击");
        }

        [Test]
        public void DoubleClickDetector_ThirdClickStartsOver()
        {
            DoubleClickDetector detector = new DoubleClickDetector();

            detector.TryClick(7, 10f, out _);
            detector.TryClick(7, 10.05f, out bool secondIsDouble);
            Assert.IsTrue(secondIsDouble);

            // 双击已经消费, 紧跟着的第三击不该又算一次双击
            Assert.IsTrue(detector.TryClick(7, 10.1f, out bool thirdIsDouble));
            Assert.IsFalse(thirdIsDouble);
        }

        [Test]
        public void DoubleClickDetector_NoItem_RejectsClick()
        {
            DoubleClickDetector detector = new DoubleClickDetector();

            Assert.IsFalse(detector.TryClick(NO_ITEM, 10f, out bool isDouble), "空格子的点击不受理");
            Assert.IsFalse(isDouble);
        }
        #endregion

        #region 菜单丢弃请求
        [Test]
        public void ItemMenuViewModel_Drop_EmitsDropRequested()
        {
            int instanceId = PutItemIntoSource(2, 3, 1);
            ItemMenuViewModel menu = CreateMenu();
            menu.Open(m_sourceId, instanceId);

            int dropCount = 0;
            int dismissCount = 0;
            using (menu.DropRequested.Subscribe(() => dropCount++))
            using (menu.Dismissed.Subscribe(() => dismissCount++))
            {
                menu.Drop();
            }

            Assert.AreEqual(1, dropCount, "点丢弃要先广播请求, 落点由运行期宿主决定");
            Assert.AreEqual(1, dismissCount, "广播之后菜单自己收起");
            Assert.AreEqual(1, m_model.GetContainerVO(m_sourceId).Items.Count, "视图模型不碰模型, 物品仍在源箱");
        }

        [Test]
        public void ItemMenuViewModel_OpenAndDrop_TargetsCurrentContainerAndInstance()
        {
            int firstInstanceId = PutItemIntoSource(2, 2, 1);
            int secondInstanceId = PutItemIntoSource(1, 1, 1);
            ItemMenuViewModel menu = CreateMenu();

            menu.Open(m_sourceId, firstInstanceId);
            Assert.IsTrue(menu.CanDrop.Value, "容器里查得到的物品就允许丢弃");

            int droppedInstance = NO_ITEM;
            using (menu.DropRequested.Subscribe(() => droppedInstance = menu.TargetInstanceId))
                menu.Drop();
            Assert.AreEqual(firstInstanceId, droppedInstance, "丢弃请求要指向当前锁定的实例");

            // 换目标后请求对象要跟着换, 不能继续用上一次的物品
            menu.Open(m_sourceId, secondInstanceId);
            droppedInstance = NO_ITEM;
            using (menu.DropRequested.Subscribe(() => droppedInstance = menu.TargetInstanceId))
                menu.Drop();
            Assert.AreEqual(secondInstanceId, droppedInstance);
            Assert.AreEqual(m_sourceId, menu.TargetContainerId);
        }

        [Test]
        public void ItemMenuViewModel_Drop_MissingItem_GraysOutAndIgnoresRequest()
        {
            ItemMenuViewModel menu = CreateMenu();
            menu.Open(m_sourceId, NO_ITEM);

            Assert.IsFalse(menu.CanDrop.Value, "查不到物品时丢弃按钮置灰");

            int dropCount = 0;
            using (menu.DropRequested.Subscribe(() => dropCount++))
                menu.Drop();
            Assert.AreEqual(0, dropCount, "没有目标时不该发出丢弃请求");
        }
        #endregion

        #region 落地登记链路
        [Test]
        public void GroundDropInteraction_Success_RegistersGroundItemAtOrigin()
        {
            int instanceId = PutItemIntoSource(2, 3, 4, 10);
            Vector3 origin = new Vector3(1.5f, 0f, -2f);
            GroundDropInteraction interaction = new GroundDropInteraction(m_model, m_groundModel);

            Assert.IsTrue(interaction.TryDrop(m_sourceId, instanceId, origin));
            Assert.AreEqual(0, m_model.GetContainerVO(m_sourceId).Items.Count, "源箱腾空");

            List<GroundItemVO> nearby = new List<GroundItemVO>(m_groundModel.QueryNearby(origin, 0.5f));
            Assert.AreEqual(1, nearby.Count, "丢出去的物品要能在落点查到");
            Assert.AreEqual(instanceId, nearby[0].Item.InstanceId, "登记的是同一堆物品");
            Assert.AreEqual(4, nearby[0].Item.StackCount, "整堆数量带进地面快照");
            Assert.IsTrue(m_model.IsContainerRegistered(nearby[0].ContainerId), "地面容器号要真实可用, 捡回来靠它做转移");
        }

        [Test]
        public void GroundDropInteraction_FailedDrop_RegistersNothing()
        {
            PutItemIntoSource(2, 2, 1);
            GroundDropInteraction interaction = new GroundDropInteraction(m_model, m_groundModel);

            Assert.IsFalse(interaction.TryDrop(m_sourceId, NO_ITEM, Vector3.zero), "查不到物品时丢弃失败");

            Assert.AreEqual(1, m_model.GetContainerVO(m_sourceId).Items.Count, "失败不能把源箱物品弄丢");
            Assert.AreEqual(0, m_groundModel.QueryNearby(Vector3.zero, float.MaxValue).Count, "失败的丢弃不留地面登记");
        }
        #endregion

        #region Private Function
        // 往测试源箱塞一件物品并返回实例号
        private int PutItemIntoSource(int width, int height, int stackCount, int maxStackCount = 1)
        {
            InventoryItemConfigSO config = m_kit.CreateItem(width, height, true, maxStackCount);
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItemAuto(m_sourceId, config.ItemId, stackCount));

            InventoryContainerVO source = m_model.GetContainerVO(m_sourceId);
            return source.Items[source.Items.Count - 1].InstanceId;
        }

        // 创建已绑定的菜单视图模型并登记到拆除清单
        private ItemMenuViewModel CreateMenu()
        {
            ItemMenuViewModel menu = new ItemMenuViewModel(m_model);
            m_popups.Add(menu);
            menu.OnBind();
            return menu;
        }

        // 按 ViewBase 拆除顺序回收视图模型
        private static void DisposeViewModel(ViewModelBase viewModel)
        {
            viewModel.OnUnbind();
            viewModel.OnDispose();
            viewModel.Dispose();
        }
        #endregion
    }
}
