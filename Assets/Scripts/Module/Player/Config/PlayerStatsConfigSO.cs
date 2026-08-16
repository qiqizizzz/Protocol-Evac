/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家固有数值配置资产
 * │  类    名: PlayerStatsConfigSO.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Player.Config
{
    [CreateAssetMenu(fileName = "PlayerStatsConfig", menuName = "配置/玩家/数值/玩家数值配置")]
    public sealed class PlayerStatsConfigSO : ScriptableObject
    {
        [Tooltip("玩家的最大生命值")]
        [SerializeField, Min(1f)] private float MaxHealth;

        public float MaxHealthValue => MaxHealth;
    }
}
