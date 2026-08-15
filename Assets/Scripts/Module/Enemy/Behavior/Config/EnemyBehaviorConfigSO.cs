/*
 * ┌──────────────────────────────────┐
 * │  描    述: 敌人行为调度与意图切换配置资产
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

        [Tooltip("意图选择器的刷新间隔")]
        [SerializeField, Min(0f)] private float UtilityRefreshInterval;

        [Tooltip("路径请求的刷新间隔")]
        [SerializeField, Min(0f)] private float PathRefreshInterval;

        [Header("意图切换")]
        [Tooltip("新意图分数需要超过当前意图的最小差值")]
        [SerializeField, Min(0f)] private float IntentChangeThreshold;

        [Tooltip("进入意图后的最短保持时间")]
        [SerializeField, Min(0f)] private float MinIntentHoldTime;

        [Header("搜索")]
        [Tooltip("抵达最后目击位置后的搜索持续时间")]
        [SerializeField, Min(0f)] private float SearchDuration;

        public float SensorRefreshIntervalValue => SensorRefreshInterval;
        public float UtilityRefreshIntervalValue => UtilityRefreshInterval;
        public float PathRefreshIntervalValue => PathRefreshInterval;
        public float IntentChangeThresholdValue => IntentChangeThreshold;
        public float MinIntentHoldTimeValue => MinIntentHoldTime;
        public float SearchDurationValue => SearchDuration;
    }
}
