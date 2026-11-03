/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品视图, 渲染单个物品块
 * │  类    名: UIInventoryItem.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.Binding;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Find;

namespace UI.Inventory.View
{
    public sealed class UIInventoryItem : MonoBehaviour
    {
        //单格边长与格间距, 与 InventoryPrefabBuilder 保持一致
        public const int CELL_SIZE = 80;
        public const int CELL_SPACING = 4;

        private const string ICON_PATH = "Img_Icon";
        private const string COUNT_PATH = "Txt_Count";
        private const string SELECTED_PATH = "Img_Selected";

        private RectTransform m_rect;
        private Image m_background;
        private Image m_icon;
        private TMP_Text m_countText;
        private GameObject m_selectedFrame;

        public int InstanceId { get; private set; }

        private void Awake()
        {
            m_rect = this.GetOwnerComponent<RectTransform>();
            m_background = this.GetOwnerComponent<Image>();
            m_icon = this.FindChildComponent<Image>(ICON_PATH);
            m_countText = this.FindChildComponent<TMP_Text>(COUNT_PATH);
            m_selectedFrame = this.FindChildGo(SELECTED_PATH);
        }

        //按渲染载体刷新物品表现
        public void Setup(InventoryItemVO item)
        {
            InstanceId = item.InstanceId;

            //格子坐标原点在左上角
            m_rect.sizeDelta = new Vector2(
                item.Size.x * CELL_SIZE + (item.Size.x - 1) * CELL_SPACING,
                item.Size.y * CELL_SIZE + (item.Size.y - 1) * CELL_SPACING);
            m_rect.anchoredPosition = new Vector2(
                item.Origin.x * (CELL_SIZE + CELL_SPACING),
                -item.Origin.y * (CELL_SIZE + CELL_SPACING));

            m_background.color = item.BaseColor;
            m_icon.sprite = item.Icon;
            m_icon.enabled = item.Icon != null;

            m_countText.text = item.StackCount > 1 ? item.StackCount.ToString() : string.Empty;
        }

        //绑定选中实例号, 由所在作用域统一收敛
        public void BindSelection(BindingScope scope, IReadOnlyReactiveProperty<int> selectedInstanceId)
        {
            scope.Bind(selectedInstanceId, RenderSelected);
        }

        //选中态走描边叠层, 不改物品底色
        private void RenderSelected(int selectedInstanceId)
        {
            m_selectedFrame.SetActive(selectedInstanceId == InstanceId);
        }
    }
}
