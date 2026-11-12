/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱高亮配置, 集中管理可开启与聚焦描边的颜色和脉冲表现
* │  类    名: LootBoxHighlightConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;

namespace Module.Inventory.Presentation
{
    [CreateAssetMenu(fileName = "LootBoxHighlightConfig", menuName = "配置/背包/物资箱高亮")]
    public sealed class LootBoxHighlightConfigSO : ScriptableObject
    {
        [Header("可开启描边")]
        [SerializeField] private Color AvailableColorValue = new Color(1f, 0.68f, 0.18f, 0.52f);
        [SerializeField, Min(0f)] private float AvailableWidthValue = 0.012f;
        [SerializeField, Min(0f)] private float AvailablePulseSpeedValue = 2.2f;
        [SerializeField, Range(0f, 0.5f)] private float AvailablePulseAmountValue = 0.06f;

        [Header("聚焦描边")]
        [SerializeField] private Color FocusedColorValue = new Color(1f, 0.82f, 0.28f, 0.95f);
        [SerializeField, Min(0f)] private float FocusedWidthValue = 0.022f;
        [SerializeField, Min(0f)] private float FocusedPulseSpeedValue = 5f;
        [SerializeField, Range(0f, 0.5f)] private float FocusedPulseAmountValue = 0.16f;

        public Color AvailableColor => AvailableColorValue;
        public float AvailableWidth => AvailableWidthValue;
        public float AvailablePulseSpeed => AvailablePulseSpeedValue;
        public float AvailablePulseAmount => AvailablePulseAmountValue;

        public Color FocusedColor => FocusedColorValue;
        public float FocusedWidth => FocusedWidthValue;
        public float FocusedPulseSpeed => FocusedPulseSpeedValue;
        public float FocusedPulseAmount => FocusedPulseAmountValue;
    }
}
