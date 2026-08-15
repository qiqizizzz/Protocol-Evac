/*
 * ┌──────────────────────────────────┐
 * │  描    述: 敌人行为树的目标、刷新与行为参数配置资产
 * │  类    名: EnemyBehaviorConfigSO.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Enemy.Behavior.Config
{
    [CreateAssetMenu(fileName = "EnemyBehaviorConfig", menuName = "配置/敌人/行为/敌人行为配置")]
    public sealed class EnemyBehaviorConfigSO : ScriptableObject
    {
        [Header("刷新间隔")]
        [Tooltip("感知数据的刷新间隔")]
        [SerializeField, Min(0f)] private float SensorRefreshInterval;

        [Tooltip("路径请求的刷新间隔")]
        [SerializeField, Min(0f)] private float PathRefreshInterval;

        [Header("攻击")]
        [Tooltip("敌人允许执行近战攻击的最大距离")]
        [SerializeField, Min(0f)] private float AttackDistance;

        [Header("巡逻")]
        [Tooltip("敌人围绕出生位置随机巡逻的最大半径")]
        [SerializeField, Min(0f)] private float PatrolRadius;

        [Tooltip("敌人抵达巡逻目的地后的停留时间")]
        [SerializeField, Min(0f)] private float PatrolWaitDuration;

        [Header("搜索")]
        [Tooltip("抵达最后目击位置后的搜索持续时间")]
        [SerializeField, Min(0f)] private float SearchDuration;

        public float SensorRefreshIntervalValue => SensorRefreshInterval;
        public float PathRefreshIntervalValue => PathRefreshInterval;
        public float AttackDistanceValue => AttackDistance;
        public float PatrolRadiusValue => PatrolRadius;
        public float PatrolWaitDurationValue => PatrolWaitDuration;
        public float SearchDurationValue => SearchDuration;
    }
}
