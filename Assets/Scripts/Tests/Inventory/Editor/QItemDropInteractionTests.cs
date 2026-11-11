/*
* ┌──────────────────────────────────┐
* │  描    述: 丢弃交互测试, 覆盖左键双击判定、菜单丢弃请求与地面登记链路
* │  类    名: QItemDropInteractionTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Framework.QTower.MVVM.ViewModel;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UI.Inventory;
using UI.Inventory.View.Tools;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// 丢弃交互测试：菜单与双击共用一条链路, 判定器只认同一实例上的两次近距离左键, 登记只在模型真丢成功时发生
    /// </summary>
    public class QItemDropInteractionTests
    {
        private const int NO_ITEM = -1;
        private const float INTERVAL = 0.3f;

        // 阶梯四档全撑不进一件超大物品时, 模型侧会留下一条回滚报错, 报错按条数逐条预期
        private const int RESIZE_FAILURE_LOG_COUNT = 4;

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private GroundPileModel m_pileModel;
        private int m_sourceId;
        private readonly List<ViewModelBase> m_popups = new List<ViewModelBase>();

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();
            m_pileModel = new GroundPileModel();
            m_pileModel.OnRegister();

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

            m_pileModel.OnUnregister();
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

        #region 地面堆收拢
        [Test]
        public void GroundDropInteraction_FirstDrop_CreatesPileAtTheDropOrigin()
        {
            int instanceId = PutItemIntoSource(1, 1, 3, 10);
            Vector3 origin = new Vector3(1.5f, 0f, -2f);
            GroundDropInteraction interaction = new GroundDropInteraction(m_model, m_pileModel);

            Assert.IsTrue(interaction.TryDrop(m_sourceId, instanceId, origin));
            Assert.AreEqual(0, m_model.GetContainerVO(m_sourceId).Items.Count, "源箱腾空");

            Assert.AreEqual(1, m_pileModel.Piles.Count, "脚边没堆就新起一座");
            GroundPileVO pile = m_pileModel.Piles[0];
            Assert.AreEqual(origin, pile.AnchorPosition, "堆锚点就是落点");
            Assert.IsTrue(m_model.IsContainerRegistered(pile.ContainerId), "堆容器号要真实可用, 捡回来靠它做转移");

            InventoryContainerVO pileContainer = m_model.GetContainerVO(pile.ContainerId);
            Assert.AreEqual(2, pileContainer.Width, "起堆按阶梯第一档开格");
            Assert.AreEqual(2, pileContainer.Height);
            Assert.AreEqual(1, pileContainer.Items.Count);
            Assert.AreEqual(3, pileContainer.Items[0].StackCount, "整堆数量跟着落地面");
        }

        [Test]
        public void GroundDropInteraction_NearbyDrop_MergesIntoTheSamePile()
        {
            Vector3 origin = new Vector3(0f, 0f, 1f);
            GroundDropInteraction interaction = new GroundDropInteraction(m_model, m_pileModel);

            Assert.IsTrue(interaction.TryDrop(m_sourceId, PutItemIntoSource(1, 1, 1), origin));
            string pileId = m_pileModel.Piles[0].PileId;
            int pileContainerId = m_pileModel.Piles[0].ContainerId;

            // 第二件只丢在 0.4 米外, 落在收拢半径内, 该并进同一座堆而不是又起一座
            Assert.IsTrue(interaction.TryDrop(m_sourceId, PutItemIntoSource(1, 1, 1),
                origin + new Vector3(0.4f, 0f, 0f)));

            Assert.AreEqual(1, m_pileModel.Piles.Count, "脚边的堆要合并");
            Assert.AreEqual(pileId, m_pileModel.Piles[0].PileId, "合并保留先起的那座堆号");
            Assert.AreEqual(pileContainerId, m_pileModel.Piles[0].ContainerId, "合并共用同一只容器");
            Assert.AreEqual(2, m_model.GetContainerVO(pileContainerId).Items.Count, "两件都在这座堆里");
        }

        [Test]
        public void GroundDropInteraction_DropBeyondMergeRadius_StartsANewPile()
        {
            GroundDropInteraction interaction = new GroundDropInteraction(m_model, m_pileModel);

            Assert.IsTrue(interaction.TryDrop(m_sourceId, PutItemIntoSource(1, 1, 1), Vector3.zero));
            // 隔到 8 米外, 远超收拢半径, 不该被吸进同一座堆
            Assert.IsTrue(interaction.TryDrop(m_sourceId, PutItemIntoSource(1, 1, 1), new Vector3(8f, 0f, 0f)));

            Assert.AreEqual(2, m_pileModel.Piles.Count, "远处要另起一座堆");
        }

        [Test]
        public void GroundDropInteraction_FullPile_ExpandsToTheNextLadderStep()
        {
            Vector3 origin = new Vector3(2f, 0f, 2f);
            GroundDropInteraction interaction = new GroundDropInteraction(m_model, m_pileModel);

            // 首件 2x2 把 2x2 的堆占满, 次件必须推着堆往上升一档才塞得下
            Assert.IsTrue(interaction.TryDrop(m_sourceId, PutItemIntoSource(2, 2, 1), origin));
            Assert.AreEqual(2, m_model.GetContainerVO(m_pileModel.Piles[0].ContainerId).Width);

            Assert.IsTrue(interaction.TryDrop(m_sourceId, PutItemIntoSource(1, 1, 1), origin));

            Assert.AreEqual(1, m_pileModel.Piles.Count, "升档不算新起一堆");
            InventoryContainerVO grown = m_model.GetContainerVO(m_pileModel.Piles[0].ContainerId);
            Assert.AreEqual(3, grown.Width, "2x2 塞满后升到 3x3");
            Assert.AreEqual(3, grown.Height);
            Assert.AreEqual(2, grown.Items.Count);
        }

        [Test]
        public void GroundDropInteraction_BigItemSkipsTheStepWithNoRoomForItsShape()
        {
            Vector3 origin = new Vector3(-3f, 0f, 4f);
            GroundDropInteraction interaction = new GroundDropInteraction(m_model, m_pileModel);

            // 2x2 占满 2x2 的堆后, 3x3 里剩的是边条, 再放一件 2x2 就凑不出整块 2x2 空位
            // 所以这一件要跳过 3x3 直接升到 4x3, 升档失败时不能把已经拿走的物品退回去
            Assert.IsTrue(interaction.TryDrop(m_sourceId, PutItemIntoSource(2, 2, 1), origin));
            Assert.IsTrue(interaction.TryDrop(m_sourceId, PutItemIntoSource(2, 2, 1), origin));

            InventoryContainerVO grown = m_model.GetContainerVO(m_pileModel.Piles[0].ContainerId);
            Assert.AreEqual(1, m_pileModel.Piles.Count);
            Assert.AreEqual(4, grown.Width, "3x3 塞不下整块 2x2, 要继续升档");
            Assert.AreEqual(3, grown.Height);
            Assert.AreEqual(2, grown.Items.Count);
            Assert.AreEqual(0, m_model.GetContainerVO(m_sourceId).Items.Count, "两件都已离开源箱");
        }

        [Test]
        public void GroundDropInteraction_ItemLargerThanTheTopLadder_KeepsItsOwnFootprint()
        {
            // 阶梯最大只到 4x4, 这件 5x1 一档都撑不进, 只能按物品自身轮廓起堆
            int oversizedSource = m_kit.NewContainerId();
            m_model.RegisterContainer(oversizedSource,
                new InventoryContainerSpec(6, 6, float.MaxValue, null, "超大件源箱"));

            InventoryItemConfigSO config = m_kit.CreateItem(5, 1, false);
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItemAuto(oversizedSource, config.ItemId, 1));

            InventoryContainerVO source = m_model.GetContainerVO(oversizedSource);
            int instanceId = source.Items[source.Items.Count - 1].InstanceId;

            for (int i = 0; i < RESIZE_FAILURE_LOG_COUNT; i++)
                LogAssert.Expect(LogType.Error, new Regex("容器换尺寸放不下现有内容"));

            GroundDropInteraction interaction = new GroundDropInteraction(m_model, m_pileModel);
            Assert.IsTrue(interaction.TryDrop(oversizedSource, instanceId, Vector3.zero),
                "撑到顶也要保住物品, 收回容器等于物品凭空消失");

            Assert.AreEqual(1, m_pileModel.Piles.Count);
            InventoryContainerVO pileContainer = m_model.GetContainerVO(m_pileModel.Piles[0].ContainerId);
            Assert.AreEqual(5, pileContainer.Width, "撑不进阶梯就按原尺寸保号");
            Assert.AreEqual(1, pileContainer.Height);
            Assert.AreEqual(1, pileContainer.Items.Count);
            Assert.AreEqual(0, m_model.GetContainerVO(oversizedSource).Items.Count, "源箱腾空");
        }

        [Test]
        public void GroundDropInteraction_FailedDrop_RegistersNoPile()
        {
            PutItemIntoSource(2, 2, 1);
            GroundDropInteraction interaction = new GroundDropInteraction(m_model, m_pileModel);

            Assert.IsFalse(interaction.TryDrop(m_sourceId, NO_ITEM, Vector3.zero), "查不到物品时丢弃失败");

            Assert.AreEqual(1, m_model.GetContainerVO(m_sourceId).Items.Count, "失败不能把源箱物品弄丢");
            Assert.AreEqual(0, m_pileModel.Piles.Count, "失败的丢弃不留地面堆");
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
