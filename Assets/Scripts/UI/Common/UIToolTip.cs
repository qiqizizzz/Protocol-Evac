/*
 * ┌──────────────────────────────────┐
 * │  描    述: 通用提示窗视图，负责状态图标、提示文字与自适应尺寸
 * │  类    名: UIToolTip.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.log;

namespace UI.Common
{
    public sealed class UIToolTip : UIItemBase
    {
        private const string FRAME_PATH = "Panel_Hint/Img_Frame";
        private const string ICON_PATH = "Panel_Hint/Img_Icon";
        private const string MESSAGE_PATH = "Panel_Hint/Txt_Message";

        public enum ToolTipType
        {
            Normal,
            Interaction,
            Warning,
            Success
        }

        [Header("状态图标")]
        [SerializeField] private Sprite NormalIcon;
        [SerializeField] private Sprite InteractionIcon;
        [SerializeField] private Sprite WarningIcon;
        [SerializeField] private Sprite SuccessIcon;

        [Header("状态强调色")]
        [SerializeField] private Color NormalAccentColor = new Color(0.72f, 0.82f, 0.88f, 1f);
        [SerializeField] private Color InteractionAccentColor = new Color(0.18f, 0.86f, 0.95f, 1f);
        [SerializeField] private Color WarningAccentColor = new Color(1f, 0.64f, 0.16f, 1f);
        [SerializeField] private Color SuccessAccentColor = new Color(0.36f, 0.94f, 0.58f, 1f);

        [Header("自适应尺寸")]
        [SerializeField] private float MinWidth = 280f;
        [SerializeField] private float MaxWidth = 720f;
        [SerializeField] private float MinHeight = 64f;
        [SerializeField] private float HorizontalPadding = 100f;
        [SerializeField] private float VerticalPadding = 16f;

        private RectTransform m_rootRect;
        private CanvasGroup m_canvasGroup;
        private Image m_frame;
        private Image m_icon;
        private TMP_Text m_message;

        private void Awake()
        {
            m_rootRect = GetComponent<RectTransform>();
            m_canvasGroup = GetComponent<CanvasGroup>();
            m_frame = FindComponent<Image>(FRAME_PATH);
            m_icon = FindComponent<Image>(ICON_PATH);
            m_message = FindComponent<TMP_Text>(MESSAGE_PATH);

            if (m_rootRect == null || m_canvasGroup == null || m_frame == null || m_icon == null || m_message == null)
            {
                QLog.Error("通用提示窗必要引用缺失，请检查 Prefab 层级");
                enabled = false;
                return;
            }

            MinWidth = Mathf.Max(MinWidth, 0f);
            MaxWidth = Mathf.Max(MaxWidth, MinWidth);
            MinHeight = Mathf.Max(MinHeight, 0f);
            HorizontalPadding = Mathf.Max(HorizontalPadding, 0f);
            VerticalPadding = Mathf.Max(VerticalPadding, 0f);
            Hide();
        }

        /// <summary>
        /// 显示交互类提示
        /// </summary>
        /// <param name="message">提示文字</param>
        public void Show(string message)
        {
            Show(message, ToolTipType.Interaction);
        }

        /// <summary>
        /// 按提示类型显示状态图标与自适应的提示文字
        /// </summary>
        /// <param name="message">提示文字</param>
        /// <param name="toolTipType">提示类型</param>
        public void Show(string message, ToolTipType toolTipType)
        {
            if (string.IsNullOrEmpty(message))
            {
                QLog.Warning("通用提示窗收到空文本");
                return;
            }

            m_message.text = message;
            ApplyAdaptiveSize(message);
            m_icon.sprite = GetIcon(toolTipType);
            m_frame.color = GetAccentColor(toolTipType);
            m_canvasGroup.alpha = 1f;
        }

        // 立即隐藏提示内容
        public void Hide()
        {
            m_canvasGroup.alpha = 0f;
            m_message.text = string.Empty;
        }

        // 根据文字实际排版尺寸调整窗口宽高
        private void ApplyAdaptiveSize(string message)
        {
            float availableWidth = Mathf.Max(MaxWidth - HorizontalPadding, 1f);
            Vector2 naturalSize = m_message.GetPreferredValues(message);
            float contentWidth = Mathf.Min(naturalSize.x, availableWidth);
            float contentHeight = naturalSize.y;

            if (naturalSize.x > availableWidth)
                contentHeight = m_message.GetPreferredValues(message, contentWidth, 0f).y;

            float width = Mathf.Clamp(contentWidth + HorizontalPadding, MinWidth, MaxWidth);
            float height = Mathf.Max(contentHeight + VerticalPadding, MinHeight);
            m_rootRect.sizeDelta = new Vector2(width, height);
        }

        // 获取提示类型对应的图标
        private Sprite GetIcon(ToolTipType toolTipType)
        {
            return toolTipType switch
            {
                ToolTipType.Normal => NormalIcon,
                ToolTipType.Warning => WarningIcon,
                ToolTipType.Success => SuccessIcon,
                _ => InteractionIcon
            };
        }

        // 获取提示类型对应的框架色
        private Color GetAccentColor(ToolTipType toolTipType)
        {
            return toolTipType switch
            {
                ToolTipType.Normal => NormalAccentColor,
                ToolTipType.Warning => WarningAccentColor,
                ToolTipType.Success => SuccessAccentColor,
                _ => InteractionAccentColor
            };
        }
    }
}
