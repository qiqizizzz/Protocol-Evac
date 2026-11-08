/*
 * ┌──────────────────────────────────┐
 * │  描    述: 物品右键菜单视图, 贴着鼠标摆一张按钮卡片, 点卡片外任意处收起
 * │  类    名: UIItemMenuPanel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.View;
using UI.Inventory.View.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Inventory.View
{
    public sealed class UIItemMenuPanel : ViewBase<ItemMenuViewModel>
    {
        private const string DISMISS_PATH = "Btn_Dismiss";
        private const string CARD_PATH = "Panel_Menu";
        private const string ROTATE_PATH = CARD_PATH + "/Btn_Rotate";

        // 卡片左上角相对鼠标的偏移量, 与详情面板一致
        private static readonly Vector2 m_cursorOffset = new Vector2(12f, -12f);

        private RectTransform m_rect;
        private RectTransform m_cardRect;
        private Button m_dismissButton;
        private Button m_rotateButton;

        #region 生命周期
        protected override ItemMenuViewModel CreateViewModel() => new ItemMenuViewModel();

        protected override void OnAwake()
        {
            m_rect = (RectTransform)transform;
            m_cardRect = FindComponent<RectTransform>(CARD_PATH);
            m_dismissButton = FindComponent<Button>(DISMISS_PATH);
            m_rotateButton = FindComponent<Button>(ROTATE_PATH);
        }

        protected override void BindViewModel(ItemMenuViewModel viewModel)
        {
            m_dismissButton.onClick.AddListener(viewModel.Dismiss);
            m_rotateButton.onClick.AddListener(viewModel.Rotate);

            // 不可旋转时按钮保留但置为不可用, 用于提示该物品不支持旋转
            Scope.Bind(viewModel.CanRotate, interactable => m_rotateButton.interactable = interactable);
            Scope.Bind(viewModel.Dismissed, Hide);
        }

        // 每次右键对应不同物品, 可用性与位置均在 OnShow 重新计算
        protected override void OnShow(params object[] args)
        {
            if (!ItemPopupPlacement.TryReadCursorArgs(args, out int containerId, out int instanceId,
                    out Vector3 screenPos))
                return;

            // 菜单卡片位于网格之上, 排序低于面板时旋转按钮无法接收点击
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
