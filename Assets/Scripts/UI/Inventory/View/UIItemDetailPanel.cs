/*
 * ┌──────────────────────────────────┐
 * │  描    述: 物品详情弹窗视图, 左键点中物品后把名称、标签与规格钉在完整占格右侧
 * │  类    名: UIItemDetailPanel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.View;
using UI.Inventory.View.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Inventory.View
{
    public sealed class UIItemDetailPanel : ViewBase<ItemDetailViewModel>
    {
        private const string CARD_PATH = "Panel_Detail";
        private const string ICON_PATH = CARD_PATH + "/Img_Icon";
        private const string NAME_PATH = CARD_PATH + "/Txt_Name";
        private const string TAGS_PATH = CARD_PATH + "/Txt_Tags";
        private const string SPEC_PATH = CARD_PATH + "/Txt_Spec";
        private const string CLOSE_PATH = CARD_PATH + "/Btn_Close";

        private const float ITEM_GAP = 12f;

        private RectTransform m_rect;
        private RectTransform m_cardRect;
        private Image m_iconImage;
        private TMP_Text m_nameText;
        private TMP_Text m_tagsText;
        private TMP_Text m_specText;
        private Button m_closeButton;

        #region 生命周期
        protected override ItemDetailViewModel CreateViewModel() => new ItemDetailViewModel();

        protected override void OnAwake()
        {
            m_rect = (RectTransform)transform;
            m_cardRect = FindComponent<RectTransform>(CARD_PATH);
            m_iconImage = FindComponent<Image>(ICON_PATH);
            m_nameText = FindComponent<TMP_Text>(NAME_PATH);
            m_tagsText = FindComponent<TMP_Text>(TAGS_PATH);
            m_specText = FindComponent<TMP_Text>(SPEC_PATH);
            m_closeButton = FindComponent<Button>(CLOSE_PATH);
        }

        protected override void BindViewModel(ItemDetailViewModel viewModel)
        {
            m_closeButton.onClick.AddListener(viewModel.Close);

            Scope.Bind(viewModel.Title, text => m_nameText.text = text);
            Scope.Bind(viewModel.Tags, text => m_tagsText.text = text);
            Scope.Bind(viewModel.Specs, text => m_specText.text = text);
            // 无图标物品保留空白底板, 不加载占位贴图
            Scope.Bind(viewModel.Icon, icon =>
            {
                m_iconImage.sprite = icon;
                m_iconImage.enabled = icon != null;
            });
            Scope.Bind(viewModel.HideRequested, Hide);
        }

        // 每次打开对应不同物品, 定位与刷新均在 OnShow 处理
        protected override void OnShow(params object[] args)
        {
            if (!ItemPopupPlacement.TryReadItemRectArgs(args, out int containerId, out int instanceId,
                    out Rect itemScreenRect))
                return;

            // 排序需高于面板才能接收关闭按钮点击, 同级排序仅决定显示先后
            ItemPopupPlacement.RaiseAbovePanels(gameObject);
            transform.SetAsLastSibling();
            ViewModel.Open(containerId, instanceId);
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_cardRect);
            ItemPopupPlacement.AnchorToScreenRect(m_rect, m_cardRect, itemScreenRect, ITEM_GAP);
        }
        #endregion
    }
}
