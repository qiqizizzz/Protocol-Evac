/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品视图                      
 * │  类    名: InventoryItemView.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Inventory.View
{
    public sealed class InventoryItemView : MonoBehaviour
    {
        public const int CELL_SIZE = 80;
        public const int CELL_SPACING = 4;

        private static readonly Color[] m_rarityColors =
        {
            new Color(1.00f, 1.00f, 1.00f, 0.90f), // None      
            new Color(0.34f, 0.36f, 0.40f, 0.92f), // Common
            new Color(0.20f, 0.44f, 0.28f, 0.92f), // Uncommon
            new Color(0.16f, 0.34f, 0.58f, 0.92f), // Rare
            new Color(0.36f, 0.20f, 0.54f, 0.92f), // Epic
            new Color(0.58f, 0.42f, 0.12f, 0.92f)  // Legendary
        };

        private RectTransform m_rect;
        private Image m_background;
        private Image m_icon;
        private TMP_Text m_countText;

        private void Awake()
        {
            m_rect = transform as RectTransform;
            m_background = GetComponent<Image>();
            m_icon = transform.Find("Icon").GetComponent<Image>();
        }
    }
}