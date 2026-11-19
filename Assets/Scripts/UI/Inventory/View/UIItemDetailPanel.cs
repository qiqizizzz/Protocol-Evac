/*
 * ┌──────────────────────────────────┐
 * │  描    述: 物品详情弹窗视图, 展示分类、价值、重量、占格图与描述
 * │  类    名: UIItemDetailPanel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Cysharp.Threading.Tasks;
using Framework.QTower.MVVM.View;
using UI.Inventory.View.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.log;

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
        private const string FOOTPRINT_PATH = SHOWCASE_PATH + "/Layout_Footprint";
        private const string WEIGHT_PATH = FOOTPRINT_PATH + "/Txt_Weight";
        private const string FOOTPRINT_GRID_PATH = FOOTPRINT_PATH + "/Layout_FootprintGrid";
        private const string CONTAINER_LAYOUT_BUTTON_PATH =
            FOOTPRINT_PATH + "/Btn_ContainerLayout";
        private const string DIVIDER_PATH = CARD_PATH + "/Img_Divider";
        private const string DESCRIPTION_PATH = CARD_PATH + "/Txt_Description";
        private const string CLOSE_PATH = HEADER_PATH + "/Btn_Close";

        private const float ITEM_GAP = 12f;
        private const float CONTAINER_LAYOUT_BUTTON_SIZE = 20f;
        private const float CONTAINER_LAYOUT_BUTTON_GAP = 6f;
        private const int FOOTPRINT_CELL_COUNT = 24;
        private static readonly Color m_footprintFilledColor = new Color(0.62f, 0.67f, 0.73f, 0.95f);
        private static readonly Color m_footprintEmptyColor = new Color(0.2f, 0.23f, 0.27f, 0.7f);

        private RectTransform m_rect;
        private RectTransform m_cardRect;
        private Image m_accentImage;
        private Image m_iconImage;
        private Image m_dividerImage;
        private TMP_Text m_nameText;
        private TMP_Text m_valueText;
        private TMP_Text m_tagsText;
        private TMP_Text m_weightText;
        private TMP_Text m_descriptionText;
        private Image[] m_footprintCells;
        private Button m_closeButton;
        private Button m_containerLayoutButton;
        private Rect m_anchorItemScreenRect;
        private bool m_preferLeft;
        private bool m_hasAnchor;
        private bool m_hasFootprintGrid;
        private bool m_containerLayoutOpening;
        private int m_containerLayoutGeneration;

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
            m_weightText = FindComponent<TMP_Text>(WEIGHT_PATH);
            m_descriptionText = FindComponent<TMP_Text>(DESCRIPTION_PATH);
            m_closeButton = FindComponent<Button>(CLOSE_PATH);
            m_containerLayoutButton = FindComponent<Button>(CONTAINER_LAYOUT_BUTTON_PATH);
            EnsureContainerLayoutButton();
            CacheFootprintCells();
        }

        protected override void BindViewModel(ItemDetailViewModel viewModel)
        {
            m_closeButton.onClick.AddListener(viewModel.Close);
            if (m_containerLayoutButton != null)
                m_containerLayoutButton.onClick.AddListener(viewModel.RequestContainerLayout);

            Scope.Bind(viewModel.Title, text => m_nameText.text = text);
            Scope.Bind(viewModel.Tags, text => m_tagsText.text = text);
            Scope.Bind(viewModel.Value, text => m_valueText.text = text);
            Scope.Bind(viewModel.WeightLabel, text => m_weightText.text = text);
            Scope.Bind(viewModel.FootprintPreview, UpdateFootprintPreview);
            Scope.Bind(viewModel.CanShowContainerLayout,
                canShow =>
                {
                    if (m_containerLayoutButton != null)
                        m_containerLayoutButton.gameObject.SetActive(canShow);
                });
            Scope.Bind(viewModel.ContainerLayoutRequested, () => OpenContainerLayoutAsync().Forget());
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

            m_containerLayoutGeneration++;
            HideContainerLayoutPopup();

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

        // 详情收起时同步撤掉独立容器布局, 并让仍在加载的打开请求失效
        protected override void OnHide()
        {
            m_containerLayoutGeneration++;
            HideContainerLayoutPopup();
        }
        #endregion

        // 缓存固定 6x4 占格单元, 层级不符合预期时明确暴露配置错误
        private void CacheFootprintCells()
        {
            m_footprintCells = new Image[FOOTPRINT_CELL_COUNT];
            Transform grid = transform.Find(FOOTPRINT_GRID_PATH);
            if (grid == null)
            {
                QLog.Error("Layout_FootprintGrid 未找到");
                return;
            }

            if (grid.childCount != FOOTPRINT_CELL_COUNT)
            {
                QLog.Error($"Layout_FootprintGrid 单元数错误, 期望 {FOOTPRINT_CELL_COUNT}, 实际 {grid.childCount}");
                return;
            }

            for (int i = 0; i < FOOTPRINT_CELL_COUNT; i++)
            {
                Image cell = grid.GetChild(i).GetComponent<Image>();
                if (cell == null)
                {
                    QLog.Error($"Layout_FootprintGrid 第 {i} 个单元缺少 Image");
                    return;
                }

                m_footprintCells[i] = cell;
            }

            m_hasFootprintGrid = true;
        }

        // 详情占格只绘制物品自身物理体积
        private void UpdateFootprintPreview(ItemFootprintPreviewVO preview)
        {
            if (!m_hasFootprintGrid) return;

            for (int i = 0; i < m_footprintCells.Length; i++)
            {
                bool filled = preview != null && preview.SolidCells != null &&
                    i < preview.SolidCells.Count && preview.SolidCells[i];
                m_footprintCells[i].color = filled ? m_footprintFilledColor : m_footprintEmptyColor;
            }
        }

        // 兼容尚未重生成的详情预制体, 在占格右上角补建问号按钮
        private void EnsureContainerLayoutButton()
        {
            Transform footprint = transform.Find(FOOTPRINT_PATH);
            if (footprint == null)
            {
                QLog.Error($"容器布局入口缺少父节点：{FOOTPRINT_PATH}");
                return;
            }

            if (m_containerLayoutButton == null)
            {
                GameObject buttonNode = new GameObject("Btn_ContainerLayout", typeof(RectTransform));
                buttonNode.transform.SetParent(footprint, false);

                Image image = buttonNode.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.12f);
                image.raycastTarget = true;

                Button button = buttonNode.AddComponent<Button>();
                button.targetGraphic = image;
                m_containerLayoutButton = button;

                GameObject labelNode = new GameObject("Txt_ContainerLayout", typeof(RectTransform));
                labelNode.transform.SetParent(buttonNode.transform, false);
                RectTransform labelRect = labelNode.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                TextMeshProUGUI label = labelNode.AddComponent<TextMeshProUGUI>();
                label.text = "?";
                label.fontSize = 16f;
                label.alignment = TextAlignmentOptions.Midline;
                label.color = new Color(0.9f, 0.93f, 0.98f, 1f);
                label.raycastTarget = false;

                buttonNode.SetActive(false);
                QLog.Warning($"详情预制体缺少 {CONTAINER_LAYOUT_BUTTON_PATH}, 已运行时补建");
            }

            RectTransform buttonRect = (RectTransform)m_containerLayoutButton.transform;
            buttonRect.SetParent(footprint, false);
            buttonRect.anchorMin = Vector2.one;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.pivot = new Vector2(1f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, CONTAINER_LAYOUT_BUTTON_GAP);
            buttonRect.sizeDelta = new Vector2(CONTAINER_LAYOUT_BUTTON_SIZE, CONTAINER_LAYOUT_BUTTON_SIZE);

            LayoutElement layout = buttonRect.GetComponent<LayoutElement>();
            if (layout == null)
                layout = buttonRect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = CONTAINER_LAYOUT_BUTTON_SIZE;
            layout.preferredHeight = CONTAINER_LAYOUT_BUTTON_SIZE;
            // 问号固定在占格右上角, 不参与重量与占格网格的水平排布
            layout.ignoreLayout = true;
        }

        // 打开独立窗口展示容器完整内部布局
        private async UniTaskVoid OpenContainerLayoutAsync()
        {
            if (m_containerLayoutOpening || !ViewModel.CanShowContainerLayout.Value) return;

            m_containerLayoutOpening = true;
            int generation = m_containerLayoutGeneration;
            int containerId = ViewModel.ContainerId;
            int instanceId = ViewModel.InstanceId;
            if (!ItemPopupPlacement.TryGetScreenRect(m_cardRect, out Rect detailScreenRect))
            {
                m_containerLayoutOpening = false;
                QLog.Error("无法读取详情卡片屏幕矩形, 容器布局窗口未打开");
                return;
            }

            await MvvmRoot.Instance.Windows.ShowAsync<UIItemContainerLayoutPanel>(
                containerId, instanceId, detailScreenRect);

            m_containerLayoutOpening = false;

            // 等待期间详情已收起或换了物品, 迟到的窗口必须立刻藏掉
            if (generation != m_containerLayoutGeneration || containerId != ViewModel.ContainerId ||
                instanceId != ViewModel.InstanceId || !ViewModel.CanShowContainerLayout.Value)
                HideContainerLayoutPopup();
        }

        // 只有实例已创建时才隐藏, 未打开状态不触发窗口服务报错
        private static void HideContainerLayoutPopup()
        {
            MvvmRoot root = MvvmRoot.Instance;
            if (root != null && root.Windows.TryGet(out UIItemContainerLayoutPanel popup))
                popup.Hide();
        }

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
