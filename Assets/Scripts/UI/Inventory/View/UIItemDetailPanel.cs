/*
 * ┌──────────────────────────────────┐
 * │  描    述: 物品详情弹窗视图, 按物品配置展示价值、规格、专属属性与描述
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
        private const string HEADER_PATH = CARD_PATH + "/Layout_Header";
        private const string META_PATH = CARD_PATH + "/Layout_Meta";
        private const string SHOWCASE_PATH = CARD_PATH + "/Layout_Showcase";
        private const string ACCENT_PATH = HEADER_PATH + "/Img_Accent";
        private const string ICON_PATH = SHOWCASE_PATH + "/Layout_ShowcaseIcon/Img_Icon";
        private const string NAME_PATH = HEADER_PATH + "/Txt_Name";
        private const string VALUE_PATH = META_PATH + "/Txt_Value";
        private const string TAGS_PATH = META_PATH + "/Txt_Tags";
        private const string SPEC_PATH = SHOWCASE_PATH + "/Txt_Spec";
        private const string DIVIDER_PATH = CARD_PATH + "/Img_Divider";
        private const string DETAIL_ENTRIES_PATH = CARD_PATH + "/Txt_DetailEntries";
        private const string DESCRIPTION_PATH = CARD_PATH + "/Txt_Description";
        private const string CLOSE_PATH = HEADER_PATH + "/Btn_Close";

        private const float ITEM_GAP = 12f;

        private RectTransform m_rect;
        private RectTransform m_cardRect;
        private Image m_accentImage;
        private Image m_iconImage;
        private Image m_dividerImage;
        private TMP_Text m_nameText;
        private TMP_Text m_valueText;
        private TMP_Text m_tagsText;
        private TMP_Text m_specText;
        private TMP_Text m_detailEntriesText;
        private TMP_Text m_descriptionText;
        private Button m_closeButton;
        private Rect m_anchorItemScreenRect;
        private bool m_preferLeft;
        private bool m_hasAnchor;

        #region 生命周期
        protected override ItemDetailViewModel CreateViewModel() => new ItemDetailViewModel();

        protected override void OnAwake()
        {
            m_rect = (RectTransform)transform;
            m_cardRect = FindComponent<RectTransform>(CARD_PATH);
            m_accentImage = FindComponent<Image>(ACCENT_PATH);
            m_iconImage = FindComponent<Image>(ICON_PATH);
            m_dividerImage = FindComponent<Image>(DIVIDER_PATH);
            m_nameText = FindComponent<TMP_Text>(NAME_PATH);
            m_valueText = FindComponent<TMP_Text>(VALUE_PATH);
            m_tagsText = FindComponent<TMP_Text>(TAGS_PATH);
            m_specText = FindComponent<TMP_Text>(SPEC_PATH);
            m_detailEntriesText = FindComponent<TMP_Text>(DETAIL_ENTRIES_PATH);
            m_descriptionText = FindComponent<TMP_Text>(DESCRIPTION_PATH);
            m_closeButton = FindComponent<Button>(CLOSE_PATH);
        }

        protected override void BindViewModel(ItemDetailViewModel viewModel)
        {
            m_closeButton.onClick.AddListener(viewModel.Close);

            Scope.Bind(viewModel.Title, text => m_nameText.text = text);
            Scope.Bind(viewModel.Tags, text => m_tagsText.text = text);
            Scope.Bind(viewModel.Value, text => m_valueText.text = text);
            Scope.Bind(viewModel.Specs, text => m_specText.text = text);
            Scope.Bind(viewModel.DetailEntries,
                text =>
                {
                    SetOptionalSectionText(m_detailEntriesText, text);
                    RefreshLayout();
                });
            Scope.Bind(viewModel.Description,
                text =>
                {
                    SetOptionalSectionText(m_descriptionText, text);
                    RefreshLayout();
                });
            Scope.Bind(viewModel.HasContent, hasContent => m_dividerImage.gameObject.SetActive(hasContent));
            Scope.Bind(viewModel.AccentColor, color => m_accentImage.color = color);
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
                    out Rect itemScreenRect, out bool preferLeft))
                return;

            // 排序需高于面板才能接收关闭按钮点击, 同级排序仅决定显示先后
            ItemPopupPlacement.RaiseAbovePanels(gameObject);
            transform.SetAsLastSibling();
            m_anchorItemScreenRect = itemScreenRect;
            m_preferLeft = preferLeft;
            m_hasAnchor = true;
            ViewModel.Open(containerId, instanceId);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_cardRect);
            ItemPopupPlacement.AnchorToScreenRect(m_rect, m_cardRect, itemScreenRect, ITEM_GAP, preferLeft);
        }
        #endregion

        // 空属性或空描述直接收起整段, 让详情卡片按剩余内容自动缩短
        private static void SetOptionalSectionText(TMP_Text text, string content)
        {
            bool hasContent = !string.IsNullOrWhiteSpace(content);
            text.text = content;
            text.gameObject.SetActive(hasContent);
        }

        // 文本高度变化后立即重排卡片, 避免长描述或空区块留下旧高度
        private void RefreshLayout()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_cardRect);

            // 显示期间内容高度变化后重新贴靠物品, 保持卡片不越出屏幕
            if (m_hasAnchor)
                ItemPopupPlacement.AnchorToScreenRect(
                    m_rect, m_cardRect, m_anchorItemScreenRect, ITEM_GAP, m_preferLeft);
        }
    }
}
