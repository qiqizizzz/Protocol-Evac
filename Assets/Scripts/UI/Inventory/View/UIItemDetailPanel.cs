/*
 * ┌──────────────────────────────────┐
 * │  描    述: 物品详情弹窗视图, 左键点中物品后把名称、标签与规格钉在鼠标旁
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

        //卡片左上角相对鼠标的避让量, 向右下错开, 免得盖住指针本身
        private static readonly Vector2 m_cursorOffset = new Vector2(12f, -12f);

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
            //没有图标的物品留一块空底板, 比糊一张白图干净
            Scope.Bind(viewModel.Icon, icon =>
            {
                m_iconImage.sprite = icon;
                m_iconImage.enabled = icon != null;
            });
            Scope.Bind(viewModel.HideRequested, Hide);
        }

        //每次打开都换一件物品重看, 所以定位与刷新全部走 OnShow
        protected override void OnShow(params object[] args)
        {
            if (!ItemPopupPlacement.TryReadCursorArgs(args, out int containerId, out int instanceId,
                    out Vector3 screenPos))
                return;

            //档位压过面板才接得住自己那颗关闭按钮的点击, 同级排序只决定后开的盖住前开的
            ItemPopupPlacement.RaiseAbovePanels(gameObject);
            transform.SetAsLastSibling();
            ViewModel.Open(containerId, instanceId);
            ItemPopupPlacement.AnchorToCursor(m_rect, m_cardRect, screenPos, m_cursorOffset);
        }
        #endregion
    }
}
