/*
 * ┌──────────────────────────────────┐
 * │  描    述: 敌人固有数值配置资产
 * │  类    名: EnemyStatsConfigSO.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Enemy.Config
{
    [CreateAssetMenu(fileName = "EnemyStatsConfig", menuName = "配置/敌人/数值/敌人数值配置")]
    public sealed class EnemyStatsConfigSO : ScriptableObject
    {
        [Tooltip("敌人的最大生命值")]
        [SerializeField, Min(1f)] private float MaxHealth;

        public float MaxHealthValue => MaxHealth;
    }
}
