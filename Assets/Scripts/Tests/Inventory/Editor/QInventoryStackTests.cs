/*
* ┌──────────────────────────────────┐
* │  描    述: 堆叠与拆分测试, 覆盖同类并堆判定、拆堆落位以及拆分弹窗与菜单的视图模型
* │  类    名: QInventoryStackTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Framework.QTower.MVVM.ViewModel;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Defines;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;

namespace Tests.Inventory
{
    /// <summary>
    /// 堆叠与拆分测试：整堆吸收、部分吸收、满堆回退交换与跨容器并堆的重量口径,
    /// 再加 TrySplitItem 的拆堆落位, 以及拆分弹窗与右键菜单拆分入口的视图模型
    /// </summary>
    public class QInventoryStackTests
    {
        private const int GRID_WIDTH = 4;
        private const int MAX_STACK = 60;
        private const string CONTAINER_NAME = "测试容器";
        private const int PRIMARY_WEAPON_SLOT = InventoryContainerIds.PRIMARY_WEAPON_SLOT;
        private const float DELTA = 0.0001f;

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private int m_containerId;
        private int m_otherId;

        // 记录用例内创建的视图模型, 收尾统一回收
        private readonly List<InventoryViewModel> m_carriers = new List<InventoryViewModel>();
        private readonly List<ViewModelBase> m_popups = new List<ViewModelBase>();

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();

            m_containerId = m_kit.NewContainerId();
            m_otherId = m_kit.NewContainerId();
            Register(m_containerId, GRID_WIDTH, GRID_WIDTH, float.MaxValue);
            Register(m_otherId, GRID_WIDTH, GRID_WIDTH, float.MaxValue);

            m_carriers.Clear();
            m_popups.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = m_carriers.Count - 1; i >= 0; i--)
                DisposeViewModel(m_carriers[i]);
            m_carriers.Clear();

            for (int i = m_popups.Count - 1; i >= 0; i--)
                DisposeViewModel(m_popups[i]);
            m_popups.Clear();

            m_model.OnUnregister();
            m_kit.Dispose();
        }

        #region 同容器并堆
        [Test]
        public void Merge_WholeStackAbsorbed_RemovesSourcePlacement()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 20, new Vector2Int(0, 0));
            int destination = AddStack(m_containerId, itemId, 30, new Vector2Int(1, 0));

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(m_containerId, m_containerId, source, new Vector2Int(1, 0)));

            Assert.AreEqual(50, StackOf(m_containerId, destination));
            Assert.IsFalse(HasItem(m_containerId, source), "整堆并走之后源实例不该还在");
            Assert.IsFalse(m_model.TryGetInstanceIdAt(m_containerId, new Vector2Int(0, 0), out _), "腾出的格子要能再放");
        }

        [Test]
        public void Merge_PartialAbsorption_LeavesRemainderInPlace()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 20, new Vector2Int(0, 0));
            int destination = AddStack(m_containerId, itemId, MAX_STACK - 5, new Vector2Int(1, 0));

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(m_containerId, m_containerId, source, new Vector2Int(1, 0)));

            Assert.AreEqual(MAX_STACK, StackOf(m_containerId, destination), "目标堆只吃到上限为止");
            Assert.AreEqual(15, StackOf(m_containerId, source), "装不下的余量该留在原格");
        }

        [Test]
        public void Merge_FullDestination_FallsBackToSwap()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 20, new Vector2Int(0, 0));
            int destination = AddStack(m_containerId, itemId, MAX_STACK, new Vector2Int(1, 0));

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(m_containerId, m_containerId, source, new Vector2Int(1, 0)));

            Assert.AreEqual(new Vector2Int(1, 0), ItemOf(m_containerId, source).Origin);
            Assert.AreEqual(new Vector2Int(0, 0), ItemOf(m_containerId, destination).Origin);
            Assert.AreEqual(20, StackOf(m_containerId, source), "交换不改任何一堆的数量");
            Assert.AreEqual(MAX_STACK, StackOf(m_containerId, destination));
        }

        [Test]
        public void Merge_OccupiedBySameStack_KeepsThirdItemUntouched()
        {
            int itemId = NewStackItem(2, 1);
            int thirdItemId = NewStackItem(2, 1);
            int source = AddStack(m_containerId, itemId, 10, new Vector2Int(0, 3));
            int destination = AddStack(m_containerId, itemId, 10, new Vector2Int(1, 0));
            int third = AddStack(m_containerId, thirdItemId, 4, new Vector2Int(1, 2));

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(m_containerId, m_containerId, source, new Vector2Int(1, 0)));

            Assert.AreEqual(20, StackOf(m_containerId, destination));
            Assert.IsFalse(HasItem(m_containerId, source));
            Assert.AreEqual(4, StackOf(m_containerId, third), "并堆只动落点那一格, 旁处的物品不受牵连");
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, new Vector2Int(1, 2), out int stillThere));
            Assert.AreEqual(third, stillThere);
        }
        #endregion

        #region 跨容器并堆
        [Test]
        public void Merge_AcrossContainers_AbsorbsIntoTargetContainer()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 20, new Vector2Int(0, 0));
            int destination = AddStack(m_otherId, itemId, 30, new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(m_containerId, m_otherId, source, new Vector2Int(0, 0)));

            Assert.AreEqual(50, StackOf(m_otherId, destination));
            Assert.IsFalse(HasItem(m_containerId, source));
            Assert.IsFalse(m_model.TryGetInstanceIdAt(m_containerId, new Vector2Int(0, 0), out _), "源容器不该留痕");
        }

        [Test]
        public void Merge_TargetOverWeightLimit_ReportsWeightExceeded()
        {
            int tightId = m_kit.NewContainerId();
            Register(tightId, 2, 2, 50f);

            int itemId = m_kit.CreateItem(1, 1, true, MAX_STACK, 10f).ItemId;
            int source = AddStack(m_containerId, itemId, 5, new Vector2Int(0, 0));
            int destination = AddStack(tightId, itemId, 4, new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.WeightExceeded,
                m_model.TryTransferItem(m_containerId, tightId, source, new Vector2Int(0, 0)));

            Assert.AreEqual(5, StackOf(m_containerId, source));
            Assert.AreEqual(4, StackOf(tightId, destination), "超重被拒之后两边数量都要原封不动");
        }
        #endregion

        #region 并堆预判
        [Test]
        public void Peek_MergeableStack_ReportsMergeNotSwap()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 8, new Vector2Int(0, 0));
            AddStack(m_containerId, itemId, 8, new Vector2Int(1, 0));

            InventoryMoveResult verdict = m_model.PeekTransferItem(m_containerId, m_containerId, source,
                new Vector2Int(1, 0), InventoryRotation.None, out bool willSwap, out bool willMerge,
                out InventoryRotation rotation);

            Assert.AreEqual(InventoryMoveResult.Success, verdict);
            Assert.IsTrue(willMerge);
            Assert.IsFalse(willSwap);
            Assert.AreEqual(InventoryRotation.None, rotation);
        }

        [Test]
        public void Peek_DifferentItem_ReportsSwapNotMerge()
        {
            int source = AddStack(m_containerId, NewStackItem(), 8, new Vector2Int(0, 0));
            AddStack(m_containerId, NewStackItem(), 3, new Vector2Int(1, 0));

            InventoryMoveResult verdict = m_model.PeekTransferItem(m_containerId, m_containerId, source,
                new Vector2Int(1, 0), InventoryRotation.None, out bool willSwap, out bool willMerge, out _);

            Assert.AreEqual(InventoryMoveResult.Success, verdict);
            Assert.IsFalse(willMerge);
            Assert.IsTrue(willSwap);
        }

        [Test]
        public void Peek_UnstackableItem_ReportsSwapNotMerge()
        {
            // 上限为一就不算可堆物品, 同 ItemId 也只能交换
            int itemId = m_kit.CreateItem(1, 1, true, 1).ItemId;
            int source = AddStack(m_containerId, itemId, 1, new Vector2Int(0, 0));
            AddStack(m_containerId, itemId, 1, new Vector2Int(1, 0));

            InventoryMoveResult verdict = m_model.PeekTransferItem(m_containerId, m_containerId, source,
                new Vector2Int(1, 0), InventoryRotation.None, out bool willSwap, out bool willMerge, out _);

            Assert.AreEqual(InventoryMoveResult.Success, verdict);
            Assert.IsFalse(willMerge);
            Assert.IsTrue(willSwap);
        }

        [Test]
        public void Peek_IntoEquipSlot_ReportsSwapNotMerge()
        {
            int itemId = NewStackItem(1, 1, MAX_STACK, InventoryEquipSlot.PrimaryWeapon);
            AddStack(PRIMARY_WEAPON_SLOT, itemId, 10, Vector2Int.zero);
            int source = AddStack(m_containerId, itemId, 5, Vector2Int.zero);

            InventoryMoveResult verdict = m_model.PeekTransferItem(m_containerId, PRIMARY_WEAPON_SLOT, source,
                Vector2Int.zero, InventoryRotation.None, out bool willSwap, out bool willMerge, out _);

            Assert.AreEqual(InventoryMoveResult.Success, verdict);
            Assert.IsFalse(willMerge, "装备槽按单格收纳整件装备, 不参与并堆");
            Assert.IsTrue(willSwap);
        }

        [Test]
        public void PreviewDropAt_OntoSameStack_MarksMerge()
        {
            int itemId = NewStackItem();
            AddStack(m_containerId, itemId, 8, new Vector2Int(0, 0));
            AddStack(m_containerId, itemId, 8, new Vector2Int(1, 0));

            InventoryViewModel carrier = BindCarrier(m_containerId);
            carrier.BeginDragAt(new Vector2Int(0, 0));
            carrier.PreviewDropAt(new Vector2Int(1, 0));

            Assert.AreEqual(InventoryCellMark.DropMerge, MarkAt(carrier, 1, 0));
            Assert.AreEqual(InventoryCellMark.None, MarkAt(carrier, 0, 0), "拖起来的原位不该留色");
        }
        #endregion

        #region 拆分落位
        [Test]
        public void Split_MovesPartIntoFirstFreeCell()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 30, new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TrySplitItem(m_containerId, source, 10, out int splitId));

            Assert.AreEqual(20, StackOf(m_containerId, source));
            Assert.AreEqual(10, StackOf(m_containerId, splitId));
            Assert.AreEqual(new Vector2Int(1, 0), ItemOf(m_containerId, splitId).Origin, "新堆落在扫到的第一个空格");
        }

        [Test]
        public void Split_InheritsSourceRotation()
        {
            int itemId = NewStackItem(2, 1);
            int source = AddStack(m_containerId, itemId, 6, new Vector2Int(0, 0));
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryRotateItem(m_containerId, source));

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TrySplitItem(m_containerId, source, 2, out int splitId));

            InventoryItemVO split = ItemOf(m_containerId, splitId);
            Assert.AreEqual(InventoryRotation.Rotate90, split.Rotation, "拆出来的新堆要跟原堆同朝向");
            Assert.AreEqual(new Vector2Int(1, 2), split.Size);
        }

        [Test]
        public void Split_WholeStackCount_Rejected()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 10, new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.None,
                m_model.TrySplitItem(m_containerId, source, 10, out int splitId));

            Assert.AreEqual(0, splitId);
            Assert.AreEqual(10, StackOf(m_containerId, source), "整堆拆走会留不住原堆, 必须拒绝");
        }

        [Test]
        public void Split_ZeroCount_Rejected()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 10, new Vector2Int(0, 0));

            Assert.AreEqual(InventoryMoveResult.None,
                m_model.TrySplitItem(m_containerId, source, 0, out int splitId));

            Assert.AreEqual(0, splitId);
            Assert.AreEqual(10, StackOf(m_containerId, source));
        }

        [Test]
        public void Split_NoFreeCell_ReportsOutOfBounds()
        {
            int tightId = m_kit.NewContainerId();
            Register(tightId, 1, 1, float.MaxValue);

            int itemId = NewStackItem();
            int source = AddStack(tightId, itemId, 4, Vector2Int.zero);

            Assert.AreEqual(InventoryMoveResult.OutOfBounds,
                m_model.TrySplitItem(tightId, source, 1, out int splitId));

            Assert.AreEqual(0, splitId);
            Assert.AreEqual(4, StackOf(tightId, source), "没有落位时数量不该被改动");
        }

        [Test]
        public void Split_UnknownInstance_ReportsSourceNotFound()
        {
            Assert.AreEqual(InventoryMoveResult.SourceNotFound,
                m_model.TrySplitItem(m_containerId, int.MaxValue, 1, out int splitId));

            Assert.AreEqual(0, splitId);
        }
        #endregion

        #region 拆分弹窗视图模型
        [Test]
        public void SplitPanel_OpenOnStackOfThree_DefaultsToHalfRemainder()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 3, Vector2Int.zero);

            ItemSplitViewModel panel = CreateSplit();
            int dismissCount = 0;
            using IDisposable subscription = panel.Dismissed.Subscribe(() => dismissCount++);

            panel.Open(m_containerId, source);

            Assert.AreEqual("数量：1/3", panel.CountText.Value);
            Assert.AreEqual(0.5f, panel.Ratio.Value, DELTA);
            Assert.AreEqual(0, dismissCount);
        }

        [Test]
        public void SplitPanel_SetRatio_FullSpanSplitsMost()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 5, Vector2Int.zero);
            ItemSplitViewModel panel = CreateSplit();
            panel.Open(m_containerId, source);

            panel.SetRatio(1f);

            Assert.AreEqual("数量：4/5", panel.CountText.Value, "滑条拉满只拆到留一整份为止");
            Assert.AreEqual(1f, panel.Ratio.Value, DELTA);
        }

        [Test]
        public void SplitPanel_SetRatio_ZeroKeepsSinglePortion()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 5, Vector2Int.zero);
            ItemSplitViewModel panel = CreateSplit();
            panel.Open(m_containerId, source);

            panel.SetRatio(0f);

            Assert.AreEqual("数量：1/5", panel.CountText.Value);
            Assert.AreEqual(0.25f, panel.Ratio.Value, DELTA, "滑条位置要回写到真实档位");
        }

        [Test]
        public void SplitPanel_StepUpBeyondSpan_ClampsToMaximum()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 3, Vector2Int.zero);
            ItemSplitViewModel panel = CreateSplit();
            panel.Open(m_containerId, source);

            panel.Step(1);
            panel.Step(1);

            Assert.AreEqual("数量：2/3", panel.CountText.Value, "加号越过上限就停在最大档位");
        }

        [Test]
        public void SplitPanel_StepDownBelowOne_ClampsToSingle()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 5, Vector2Int.zero);
            ItemSplitViewModel panel = CreateSplit();
            panel.Open(m_containerId, source);
            Assert.AreEqual("数量：2/5", panel.CountText.Value);

            panel.Step(-1);
            panel.Step(-1);

            Assert.AreEqual("数量：1/5", panel.CountText.Value, "减号不能把拆出数量压到一整份以下");
        }

        [Test]
        public void SplitPanel_Confirm_SplitsStackAndDismissesOnce()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 4, Vector2Int.zero);
            ItemSplitViewModel panel = CreateSplit();
            int dismissCount = 0;
            using IDisposable subscription = panel.Dismissed.Subscribe(() => dismissCount++);
            panel.Open(m_containerId, source);
            Assert.AreEqual("数量：2/4", panel.CountText.Value);

            panel.Confirm();

            Assert.AreEqual(1, dismissCount);
            Assert.AreEqual(2, StackOf(m_containerId, source));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, new Vector2Int(1, 0), out int splitId));
            Assert.AreEqual(2, StackOf(m_containerId, splitId));
        }

        [Test]
        public void SplitPanel_ConfirmWithoutRoom_KeepsStackAndDismisses()
        {
            int tightId = m_kit.NewContainerId();
            Register(tightId, 1, 1, float.MaxValue);

            int itemId = NewStackItem();
            int source = AddStack(tightId, itemId, 4, Vector2Int.zero);
            ItemSplitViewModel panel = CreateSplit();
            int dismissCount = 0;
            using IDisposable subscription = panel.Dismissed.Subscribe(() => dismissCount++);
            panel.Open(tightId, source);

            panel.Confirm();

            Assert.AreEqual(1, dismissCount, "拆分被拒也要收起弹窗");
            Assert.AreEqual(4, StackOf(tightId, source));
        }

        [Test]
        public void SplitPanel_ConfirmBeforeOpen_DoesNothing()
        {
            ItemSplitViewModel panel = CreateSplit();
            int dismissCount = 0;
            using IDisposable subscription = panel.Dismissed.Subscribe(() => dismissCount++);

            panel.Confirm();

            Assert.AreEqual(0, dismissCount, "没打开过弹窗就没有操作对象");
        }

        [Test]
        public void SplitPanel_Dismiss_FiresDismissed()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 4, Vector2Int.zero);
            ItemSplitViewModel panel = CreateSplit();
            int dismissCount = 0;
            using IDisposable subscription = panel.Dismissed.Subscribe(() => dismissCount++);
            panel.Open(m_containerId, source);

            panel.Dismiss();

            Assert.AreEqual(1, dismissCount);
            Assert.AreEqual(4, StackOf(m_containerId, source), "点空白处收起不该动数量");
        }

        [Test]
        public void SplitPanel_OpenOnSinglePortion_DismissesImmediately()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 1, Vector2Int.zero);
            ItemSplitViewModel panel = CreateSplit();
            int dismissCount = 0;
            using IDisposable subscription = panel.Dismissed.Subscribe(() => dismissCount++);

            panel.Open(m_containerId, source);

            Assert.AreEqual(1, dismissCount, "只剩一份就没有可拆的余量");
        }

        [Test]
        public void SplitPanel_WhenItemVanishes_Dismisses()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 3, Vector2Int.zero);
            ItemSplitViewModel panel = CreateSplit();
            int dismissCount = 0;
            using IDisposable subscription = panel.Dismissed.Subscribe(() => dismissCount++);
            panel.Open(m_containerId, source);

            m_model.UnregisterContainer(m_containerId);

            Assert.AreEqual(1, dismissCount, "物品没了就要自动收起");
        }

        [Test]
        public void SplitPanel_ModelChangeInAnotherContainer_Ignored()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 4, Vector2Int.zero);
            ItemSplitViewModel panel = CreateSplit();
            int dismissCount = 0;
            using IDisposable subscription = panel.Dismissed.Subscribe(() => dismissCount++);
            panel.Open(m_containerId, source);

            AddStack(m_otherId, itemId, 2, Vector2Int.zero);

            Assert.AreEqual(0, dismissCount, "别的容器变化与本次拆分无关");
            Assert.AreEqual("数量：2/4", panel.CountText.Value);
        }

        [Test]
        public void SplitPanel_ModelChangeInSameContainer_RefreshesCount()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 3, Vector2Int.zero);
            int feeder = AddStack(m_containerId, itemId, 2, new Vector2Int(1, 0));
            ItemSplitViewModel panel = CreateSplit();
            int dismissCount = 0;
            using IDisposable subscription = panel.Dismissed.Subscribe(() => dismissCount++);
            panel.Open(m_containerId, source);
            Assert.AreEqual("数量：1/3", panel.CountText.Value);

            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryTransferItem(m_containerId, m_containerId, feeder, Vector2Int.zero));

            Assert.AreEqual(0, dismissCount);
            Assert.AreEqual("数量：1/5", panel.CountText.Value, "原堆被并大之后文案要跟着刷新");
        }
        #endregion

        #region 菜单拆分入口
        [Test]
        public void Menu_SplitOnSinglePortion_ReportsSplitUnavailable()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 1, Vector2Int.zero);
            ItemMenuViewModel menu = CreateMenu();

            menu.Open(m_containerId, source);

            Assert.IsFalse(menu.CanSplit.Value, "只剩一份时拆分按钮要置灰");
            Assert.IsTrue(menu.CanRotate.Value);
        }

        [Test]
        public void Menu_SplitOnStack_ReportsSplitAvailable()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 7, Vector2Int.zero);
            ItemMenuViewModel menu = CreateMenu();

            menu.Open(m_containerId, source);

            Assert.IsTrue(menu.CanSplit.Value);
            Assert.AreEqual(m_containerId, menu.TargetContainerId);
            Assert.AreEqual(source, menu.TargetInstanceId);
        }

        [Test]
        public void Menu_Split_FiresRequestBeforeDismiss()
        {
            int itemId = NewStackItem();
            int source = AddStack(m_containerId, itemId, 7, Vector2Int.zero);
            ItemMenuViewModel menu = CreateMenu();
            List<string> order = new List<string>();
            using IDisposable request = menu.SplitRequested.Subscribe(() => order.Add("request"));
            using IDisposable dismiss = menu.Dismissed.Subscribe(() => order.Add("dismiss"));
            menu.Open(m_containerId, source);

            menu.Split();

            Assert.AreEqual("request,dismiss", string.Join(",", order), "先请求拆分再收起菜单");
            Assert.AreEqual(7, StackOf(m_containerId, source), "菜单只发请求, 数量交给弹窗决定");
        }

        [Test]
        public void Menu_Split_WithoutOpen_DoesNothing()
        {
            ItemMenuViewModel menu = CreateMenu();
            List<string> order = new List<string>();
            using IDisposable request = menu.SplitRequested.Subscribe(() => order.Add("request"));
            using IDisposable dismiss = menu.Dismissed.Subscribe(() => order.Add("dismiss"));

            menu.Split();

            Assert.AreEqual(0, order.Count);
        }

        [Test]
        public void Menu_OnUnknownItem_ReportsBothActionsUnavailable()
        {
            ItemMenuViewModel menu = CreateMenu();

            menu.Open(m_containerId, int.MaxValue);

            Assert.IsFalse(menu.CanRotate.Value);
            Assert.IsFalse(menu.CanSplit.Value);
        }
        #endregion

        // ==================== Private Function ====================
        // 注册一张矩形容器, 限重由用例自己决定
        private void Register(int containerId, int width, int height, float weightLimit)
            => m_model.RegisterContainer(containerId,
                new InventoryContainerSpec(width, height, weightLimit, null, CONTAINER_NAME));

        // 造一份默认可堆物品, 按需指定外形、堆叠上限与可穿戴部位
        private int NewStackItem(int width = 1, int height = 1, int maxStackCount = MAX_STACK,
            InventoryEquipSlot equipSlot = InventoryEquipSlot.None)
            => m_kit.CreateItem(width, height, true, maxStackCount, 0f, 0, InventoryItemRarity.None, null,
                equipSlot).ItemId;

        // 往指定容器塞一堆同类物品并回这一堆的实例号
        private int AddStack(int containerId, int itemId, int amount, Vector2Int cell)
        {
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(containerId, itemId, amount, cell));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(containerId, cell, out int instanceId), "放完查不到实例");
            return instanceId;
        }

        // 从指定容器的最新快照里取实例
        private InventoryItemVO ItemOf(int containerId, int instanceId)
        {
            InventoryContainerVO container = m_model.GetContainerVO(containerId);
            Assert.IsTrue(container.TryGetItem(instanceId, out InventoryItemVO item),
                "实例 " + instanceId + " 不在容器 " + containerId);
            return item;
        }

        // 实例是否还留在容器里
        private bool HasItem(int containerId, int instanceId)
            => m_model.GetContainerVO(containerId).TryGetItem(instanceId, out _);

        // 读某一堆当前的数量
        private int StackOf(int containerId, int instanceId) => ItemOf(containerId, instanceId).StackCount;

        // 按真实流程绑定一个载体视图模型, 收尾交给 TearDown
        private InventoryViewModel BindCarrier(int containerId)
        {
            InventoryViewModel viewModel = new InventoryViewModel(m_model, containerId);
            viewModel.OnBind();
            m_carriers.Add(viewModel);
            return viewModel;
        }

        // 创建已绑定的菜单视图模型并登记到拆除清单
        private ItemMenuViewModel CreateMenu()
        {
            ItemMenuViewModel menu = new ItemMenuViewModel(m_model);
            m_popups.Add(menu);
            menu.OnBind();
            return menu;
        }

        // 创建已绑定的拆分弹窗视图模型并登记到拆除清单
        private ItemSplitViewModel CreateSplit()
        {
            ItemSplitViewModel panel = new ItemSplitViewModel(m_model);
            m_popups.Add(panel);
            panel.OnBind();
            return panel;
        }

        // 读某一格的着色语义, 网格统一按 4 列排
        private static InventoryCellMark MarkAt(InventoryViewModel viewModel, int x, int y)
            => viewModel.GetSlot(y * GRID_WIDTH + x).Mark.Value;

        // 按 ViewBase 拆除顺序回收视图模型
        private static void DisposeViewModel(ViewModelBase viewModel)
        {
            viewModel.OnUnbind();
            viewModel.OnDispose();
            viewModel.Dispose();
        }
    }
}
