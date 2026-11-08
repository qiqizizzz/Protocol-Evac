/*
* ┌──────────────────────────────────┐
* │  描    述: 物品浮层测试, 覆盖左键详情与右键选中的信号编排和两个浮层视图模型的数据填充
* │  类    名: QInventoryPopupTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Framework.QTower.MVVM.ViewModel;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;

namespace Tests.Inventory
{
    /// <summary>
    /// 左键详情与右键菜单测试：不实例化 View, 直接验证 InventoryViewModel 的浮层信号与浮层视图模型行为
    /// 左键仅打开详情不建立选中态, 右键仅建立选中态不打开详情, 两条链路相互独立
    /// </summary>
    public class QInventoryPopupTests
    {
        private const int NO_SELECTION = -1;
        private const string CONTAINER_NAME = "测试容器";

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private InventoryViewModel m_viewModel;
        private int m_containerId;

        private int m_detailCount;
        private int m_menuCount;
        private int m_closeCount;
        private IDisposable m_detailSubscription;
        private IDisposable m_menuSubscription;
        private IDisposable m_closeSubscription;

        // 记录用例内创建的浮层视图模型, 拆除时统一回收
        private readonly List<ViewModelBase> m_popups = new List<ViewModelBase>();

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();

            m_containerId = m_kit.NewContainerId();
            m_model.RegisterContainer(m_containerId,
                new InventoryContainerSpec(4, 4, float.MaxValue, null, CONTAINER_NAME));

            m_detailCount = 0;
            m_menuCount = 0;
            m_closeCount = 0;
            m_popups.Clear();

            m_viewModel = new InventoryViewModel(m_model, m_containerId);
            m_detailSubscription = m_viewModel.DetailRequested.Subscribe(() => m_detailCount++);
            m_menuSubscription = m_viewModel.MenuRequested.Subscribe(() => m_menuCount++);
            m_closeSubscription = m_viewModel.PopupClosed.Subscribe(() => m_closeCount++);
            m_viewModel.OnBind();
        }

        [TearDown]
        public void TearDown()
        {
            // 与 ViewBase 拆除顺序一致：先解绑停止广播, 再释放, 最后回收响应式成员
            m_detailSubscription.Dispose();
            m_menuSubscription.Dispose();
            m_closeSubscription.Dispose();

            for (int i = m_popups.Count - 1; i >= 0; i--)
                DisposeViewModel(m_popups[i]);
            m_popups.Clear();

            m_viewModel.OnUnbind();
            m_viewModel.OnDispose();
            m_viewModel.Dispose();
            m_model.OnUnregister();
            m_kit.Dispose();
        }

        #region 左键详情编排
        [Test]
        public void InteractAt_OnItem_FiresDetailRequestWithoutSelecting()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(1, 2));

            m_viewModel.InteractAt(new Vector2Int(1, 2));

            Assert.AreEqual(1, m_detailCount);
            Assert.AreEqual(0, m_menuCount);
            Assert.AreEqual(0, m_closeCount, "左键开详情不该顺带收起浮层");
            Assert.AreEqual(instanceId, m_viewModel.DetailInstanceId.Value);
            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value, "左键不该建立选中态");
        }

        [Test]
        public void InteractAt_OnTheSameItemAgain_ReopensDetailAtTheNewCursor()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(1, 2));
            m_viewModel.InteractAt(new Vector2Int(1, 2));

            m_viewModel.InteractAt(new Vector2Int(2, 2));

            Assert.AreEqual(2, m_detailCount, "再点一次就按新光标位置重开详情");
            Assert.AreEqual(0, m_closeCount);
            Assert.AreEqual(instanceId, m_viewModel.DetailInstanceId.Value);
        }

        [Test]
        public void InteractAt_OnAnotherItem_ReopensDetailWithoutClosingFirst()
        {
            int first = AddItem(1, 1, new Vector2Int(0, 0));
            int second = AddItem(1, 1, new Vector2Int(3, 3));

            m_viewModel.InteractAt(new Vector2Int(0, 0));
            m_viewModel.InteractAt(new Vector2Int(3, 3));

            Assert.AreEqual(2, m_detailCount);
            Assert.AreEqual(0, m_closeCount);
            Assert.AreEqual(second, m_viewModel.DetailInstanceId.Value);
            Assert.AreNotEqual(first, m_viewModel.DetailInstanceId.Value);
            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value);
        }

        [Test]
        public void InteractAt_OnEmptyCellWithoutSelection_FiresPopupClosedOnly()
        {
            m_viewModel.InteractAt(new Vector2Int(2, 2));

            Assert.AreEqual(0, m_detailCount);
            Assert.AreEqual(1, m_closeCount);
            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value);
        }

        [Test]
        public void InteractAt_OnEmptyCell_ClosesPopupAndDropsContextMenuSelection()
        {
            int instanceId = AddItem(1, 1, new Vector2Int(0, 0));
            m_viewModel.OpenMenuAt(new Vector2Int(0, 0));
            m_detailCount = 0;
            m_menuCount = 0;
            m_closeCount = 0;

            m_viewModel.InteractAt(new Vector2Int(3, 3));

            Assert.AreEqual(0, m_detailCount, "左键点空格子不该开详情");
            Assert.AreEqual(1, m_closeCount, "点空白处要收起残留浮层");
            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value, "右键留下的选中高亮要一起擦掉");
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, new Vector2Int(0, 0), out int stillThereId));
            Assert.AreEqual(instanceId, stillThereId, "左键空格子不再负责放置");
        }

        [Test]
        public void BeginDragAt_OnItem_FiresPopupClosed()
        {
            AddItem(2, 1, new Vector2Int(1, 2));

            m_viewModel.BeginDragAt(new Vector2Int(1, 2));

            Assert.IsTrue(m_viewModel.IsDragging);
            Assert.AreEqual(0, m_detailCount, "拖起来不该开详情");
            Assert.AreEqual(1, m_closeCount);
        }
        #endregion

        #region 右键菜单编排
        [Test]
        public void OpenMenuAt_OnItem_FiresMenuRequestWithoutOpeningDetail()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(1, 2));

            m_viewModel.OpenMenuAt(new Vector2Int(1, 2));

            Assert.AreEqual(1, m_menuCount);
            Assert.AreEqual(0, m_detailCount, "右键不该顺手开详情");
            Assert.AreEqual(0, m_closeCount);
            Assert.AreEqual(instanceId, m_viewModel.SelectedInstanceId.Value);
        }

        [Test]
        public void OpenMenuAt_OnTheSelectedItem_KeepsSelectionInsteadOfTogglingOff()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(1, 2));
            m_viewModel.OpenMenuAt(new Vector2Int(1, 2));
            m_detailCount = 0;
            m_menuCount = 0;
            m_closeCount = 0;

            m_viewModel.OpenMenuAt(new Vector2Int(2, 2));

            Assert.AreEqual(1, m_menuCount);
            Assert.AreEqual(instanceId, m_viewModel.SelectedInstanceId.Value, "右键已选中的物品不能把它反选掉");
            Assert.AreEqual(0, m_closeCount);
            Assert.AreEqual(0, m_detailCount, "右键不该顺手开详情");
        }

        [Test]
        public void OpenMenuAt_OnEmptyCell_IsFullySilent()
        {
            AddItem(1, 1, new Vector2Int(0, 0));
            m_viewModel.OpenMenuAt(new Vector2Int(0, 0));
            m_detailCount = 0;
            m_menuCount = 0;
            m_closeCount = 0;

            m_viewModel.OpenMenuAt(new Vector2Int(3, 3));

            Assert.AreEqual(0, m_menuCount);
            Assert.AreEqual(0, m_detailCount);
            Assert.AreEqual(0, m_closeCount);
            Assert.AreNotEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value, "右键空白处不该动选中态");
        }

        [Test]
        public void OpenMenuAt_OnAnotherItem_SwitchesSelectionToTheRightClickedOne()
        {
            AddItem(1, 1, new Vector2Int(0, 0));
            int rightClicked = AddItem(1, 1, new Vector2Int(3, 3));
            m_viewModel.OpenMenuAt(new Vector2Int(0, 0));
            m_menuCount = 0;

            m_viewModel.OpenMenuAt(new Vector2Int(3, 3));

            Assert.AreEqual(rightClicked, m_viewModel.SelectedInstanceId.Value);
            Assert.AreEqual(1, m_menuCount);
            Assert.AreEqual(0, m_closeCount, "换选中走的是直接置选中, 不是反选");
        }
        #endregion

        #region 详情视图模型
        [Test]
        public void Detail_Open_FillsTitleTagsAndSpecsFromTheModel()
        {
            int itemId = m_kit.CreateItem(2, 1, true, unitValue: 250).ItemId;
            int instanceId = AddItem(itemId, new Vector2Int(0, 0));
            ItemDetailViewModel detail = CreateDetail();

            detail.Open(m_containerId, instanceId);

            Assert.IsNotEmpty(detail.Title.Value);
            Assert.AreEqual("其他 · 未分级 · " + CONTAINER_NAME, detail.Tags.Value);
            Assert.That(detail.Specs.Value, Does.Contain("2x1"));
            Assert.IsNull(detail.Icon.Value);
        }

        [Test]
        public void Detail_Close_FiresHideRequest()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(0, 0));
            ItemDetailViewModel detail = CreateDetail();
            int hideCount = 0;
            using IDisposable subscription = detail.HideRequested.Subscribe(() => hideCount++);

            detail.Open(m_containerId, instanceId);
            detail.Close();

            Assert.AreEqual(1, hideCount, "关闭按钮只请求收起, 不自己 Hide");
        }

        [Test]
        public void Detail_WhenTheItemRotates_RefreshesSpecs()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(0, 0));
            ItemDetailViewModel detail = CreateDetail();
            detail.Open(m_containerId, instanceId);
            Assert.That(detail.Specs.Value, Does.Contain("2x1"));

            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryRotateItem(m_containerId, instanceId));

            Assert.That(detail.Specs.Value, Does.Contain("1x2"));
        }

        [Test]
        public void Detail_WhenTheItemVanishes_FiresHideRequest()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(0, 0));
            ItemDetailViewModel detail = CreateDetail();
            int hideCount = 0;
            using IDisposable subscription = detail.HideRequested.Subscribe(() => hideCount++);
            detail.Open(m_containerId, instanceId);
            Assert.AreEqual(0, hideCount);

            m_model.UnregisterContainer(m_containerId);

            Assert.AreEqual(1, hideCount, "物品没了要自动收起详情");
        }
        #endregion

        #region 菜单视图模型
        [Test]
        public void Menu_RotateOnRotatableItem_TurnsItAndDismisses()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(0, 0));
            ItemMenuViewModel menu = CreateMenu();
            int dismissCount = 0;
            using IDisposable subscription = menu.Dismissed.Subscribe(() => dismissCount++);

            menu.Open(m_containerId, instanceId);
            Assert.IsTrue(menu.CanRotate.Value);

            menu.Rotate();

            Assert.AreEqual(1, dismissCount);
            Assert.AreEqual(new Vector2Int(1, 2), ItemOf(instanceId).Size);
        }

        [Test]
        public void Menu_RotateOnNonRotatableItem_LeavesFacingAloneButDismisses()
        {
            int itemId = m_kit.CreateItem(2, 1, false).ItemId;
            int instanceId = AddItem(itemId, new Vector2Int(0, 0));
            ItemMenuViewModel menu = CreateMenu();
            int dismissCount = 0;
            using IDisposable subscription = menu.Dismissed.Subscribe(() => dismissCount++);

            menu.Open(m_containerId, instanceId);
            Assert.IsFalse(menu.CanRotate.Value, "不可旋转的物品要把按钮置灰");

            menu.Rotate();

            Assert.AreEqual(1, dismissCount);
            Assert.AreEqual(new Vector2Int(2, 1), ItemOf(instanceId).Size);
        }

        [Test]
        public void Menu_RotateBeforeAnyOpen_DoesNothing()
        {
            ItemMenuViewModel menu = CreateMenu();
            int dismissCount = 0;
            using IDisposable subscription = menu.Dismissed.Subscribe(() => dismissCount++);

            menu.Rotate();

            Assert.AreEqual(0, dismissCount, "没打开过菜单就没有操作对象, 不该收起");
        }

        [Test]
        public void Menu_Dismiss_FiresDismissed()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(0, 0));
            ItemMenuViewModel menu = CreateMenu();
            int dismissCount = 0;
            using IDisposable subscription = menu.Dismissed.Subscribe(() => dismissCount++);
            menu.Open(m_containerId, instanceId);

            menu.Dismiss();

            Assert.AreEqual(1, dismissCount);
            Assert.AreEqual(new Vector2Int(2, 1), ItemOf(instanceId).Size, "点空白处收起不该转物品");
        }
        #endregion

        #region Helpers
        // 向模型添加一件测试物品, 返回运行时实例号
        private int AddItem(int width, int height, Vector2Int cell, bool canRotate = true)
        {
            return AddItem(m_kit.CreateItem(width, height, canRotate).ItemId, cell);
        }

        // 用指定物品号添加一件测试物品
        private int AddItem(int itemId, Vector2Int cell)
        {
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_containerId, itemId, 1, cell));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, cell, out int instanceId));
            return instanceId;
        }

        // 从模型取指定实例
        private InventoryItemVO ItemOf(int instanceId)
        {
            Assert.IsTrue(m_model.GetContainerVO(m_containerId).TryGetItem(instanceId, out InventoryItemVO item));
            return item;
        }

        // 创建已绑定的详情视图模型并登记到拆除清单
        private ItemDetailViewModel CreateDetail()
        {
            ItemDetailViewModel detail = new ItemDetailViewModel(m_model);
            m_popups.Add(detail);
            detail.OnBind();
            return detail;
        }

        // 创建已绑定的菜单视图模型并登记到拆除清单
        private ItemMenuViewModel CreateMenu()
        {
            ItemMenuViewModel menu = new ItemMenuViewModel(m_model);
            m_popups.Add(menu);
            menu.OnBind();
            return menu;
        }

        // 按 ViewBase 拆除顺序回收浮层视图模型
        private static void DisposeViewModel(ViewModelBase viewModel)
        {
            viewModel.OnUnbind();
            viewModel.OnDispose();
            viewModel.Dispose();
        }
        #endregion
    }
}
