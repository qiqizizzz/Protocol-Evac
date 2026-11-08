/*
 * ┌──────────────────────────────────┐
 * │  描    述: 物品拆分弹窗视图, 贴鼠标摆一张数量卡片, 用滑条与加减按钮选定拆出数量
 * │  类    名: UIItemSplitPanel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.View;
using TMPro;
using UI.Inventory.View.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Inventory.View
{
    public sealed class UIItemSplitPanel : ViewBase<ItemSplitViewModel>
    {
        private const string DISMISS_PATH = "Btn_Dismiss";
        private const string CARD_PATH = "Panel_Split";
        private const string COUNT_PATH = CARD_PATH + "/Txt_Count";
        private const string SLIDER_PATH = CARD_PATH + "/Layout_Slider/Slider_Count";
        private const string DECREASE_PATH = CARD_PATH + "/Layout_Slider/Btn_Decrease";
        private const string INCREASE_PATH = CARD_PATH + "/Layout_Slider/Btn_Increase";
        private const string CANCEL_PATH = CARD_PATH + "/Layout_Actions/Btn_Cancel";
        private const string CONFIRM_PATH = CARD_PATH + "/Layout_Actions/Btn_Confirm";

        // 卡片左上角相对鼠标的偏移量, 与右键菜单一致
        private static readonly Vector2 m_cursorOffset = new Vector2(12f, -12f);

        private RectTransform m_rect;
        private RectTransform m_cardRect;
        private Button m_dismissButton;
        private Button m_decreaseButton;
        private Button m_increaseButton;
        private Button m_cancelButton;
        private Button m_confirmButton;
        private Slider m_slider;
        private TMP_Text m_countText;

        #region 生命周期
        protected override ItemSplitViewModel CreateViewModel() => new ItemSplitViewModel();

        protected override void OnAwake()
        {
            m_rect = (RectTransform)transform;
            m_cardRect = FindComponent<RectTransform>(CARD_PATH);
            m_dismissButton = FindComponent<Button>(DISMISS_PATH);
            m_countText = FindComponent<TMP_Text>(COUNT_PATH);
            m_slider = FindComponent<Slider>(SLIDER_PATH);
            m_decreaseButton = FindComponent<Button>(DECREASE_PATH);
            m_increaseButton = FindComponent<Button>(INCREASE_PATH);
            m_cancelButton = FindComponent<Button>(CANCEL_PATH);
            m_confirmButton = FindComponent<Button>(CONFIRM_PATH);
        }

        protected override void BindViewModel(ItemSplitViewModel viewModel)
        {
            m_dismissButton.onClick.AddListener(viewModel.Dismiss);
            m_cancelButton.onClick.AddListener(viewModel.Dismiss);
            m_confirmButton.onClick.AddListener(viewModel.Confirm);
            m_decreaseButton.onClick.AddListener(() => viewModel.Step(-1));
            m_increaseButton.onClick.AddListener(() => viewModel.Step(1));
            m_slider.onValueChanged.AddListener(viewModel.SetRatio);

            Scope.Bind(viewModel.CountText, text => m_countText.text = text);
            // 视图模型已把数量取整并回算比例, 回写滑条时不再触发回调, 避免与拖动互相顶
            Scope.Bind(viewModel.Ratio, ratio => m_slider.SetValueWithoutNotify(ratio));
            Scope.Bind(viewModel.Dismissed, Hide);
        }

        // 每次拆分对应不同物品, 数量与位置均在 OnShow 重新计算
        protected override void OnShow(params object[] args)
        {
            if (!ItemPopupPlacement.TryReadCursorArgs(args, out int containerId, out int instanceId,
                    out Vector3 screenPos))
                return;

            // 卡片需遮挡遮罩射线, 排序也要抬到面板之上才能点到按钮
            ItemPopupPlacement.RaiseAbovePanels(gameObject);
            transform.SetAsLastSibling();
            ViewModel.Open(containerId, instanceId);

            // 卡片高度由 VerticalLayoutGroup 计算, 先重建布局再取实际尺寸定位
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_cardRect);
            ItemPopupPlacement.AnchorToCursor(m_rect, m_cardRect, screenPos, m_cursorOffset);
        }
        #endregion
    }
}
