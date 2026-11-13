/*
 * ┌──────────────────────────────────┐
 * │  描    述: 物资箱逐件搜索时长配置, 按物品稀有度决定揭晓节奏
 * │  类    名: LootSearchConfigSO.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Inventory.Core.Config.Type;
using UnityEngine;

namespace Module.Inventory.Core.Config.Loot
{
    [CreateAssetMenu(fileName = "LootSearchConfig", menuName = "配置/背包/物资箱搜索时长")]
    public sealed class LootSearchConfigSO : ScriptableObject
    {
        [Header("逐件搜索时长")]
        [Tooltip("未配置稀有度时的搜索秒数")]
        [SerializeField, Min(0f)] private float NoneSecondsValue = 0.35f;

        [Tooltip("普通品质的搜索秒数")]
        [SerializeField, Min(0f)] private float CommonSecondsValue = 0.65f;

        [Tooltip("优秀品质的搜索秒数")]
        [SerializeField, Min(0f)] private float UncommonSecondsValue = 1.05f;

        [Tooltip("稀有品质的搜索秒数")]
        [SerializeField, Min(0f)] private float RareSecondsValue = 1.5f;

        [Tooltip("史诗品质的搜索秒数")]
        [SerializeField, Min(0f)] private float EpicSecondsValue = 2.2f;

        [Tooltip("传说品质的搜索秒数")]
        [SerializeField, Min(0f)] private float LegendarySecondsValue = 3f;

        /// <summary>
        /// 查询指定稀有度物品的逐件搜索时长
        /// </summary>
        /// <param name="rarity">物品稀有度</param>
        public float GetSearchSeconds(InventoryItemRarity rarity)
        {
            float seconds = rarity switch
            {
                InventoryItemRarity.Common => CommonSecondsValue,
                InventoryItemRarity.Uncommon => UncommonSecondsValue,
                InventoryItemRarity.Rare => RareSecondsValue,
                InventoryItemRarity.Epic => EpicSecondsValue,
                InventoryItemRarity.Legendary => LegendarySecondsValue,
                _ => NoneSecondsValue
            };

            return Mathf.Max(0f, seconds);
        }
    }
}
