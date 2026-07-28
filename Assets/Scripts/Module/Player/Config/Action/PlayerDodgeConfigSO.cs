/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家闪避配置，保存闪避位移与输入缓存参数
 * │  类    名: PlayerDodgeConfigSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using UnityEngine;
using UnityEngine.Serialization;

namespace Module.Player.Config.Action
{
    [CreateAssetMenu(fileName = "PlayerDodgeConfig", menuName = "配置/玩家/动作/玩家闪避配置")]
    public sealed class PlayerDodgeConfigSO : ScriptableObject
    {
        [Header("闪避位移")]
        [Tooltip("闪避期间的水平速度")]
        [SerializeField, Min(0f)] private float DodgeSpeedValue = 9f;
        [Tooltip("闪避持续时间")]
        [SerializeField, Min(0f)] private float DodgeDurationValue = 0.32f;

        [Header("输入容错")]
        [Tooltip("闪避动作的输入力度平方阈值")]
        [SerializeField, Min(0f)] private float DodgeInputThresholdSqrValue = 0.01f;
        [Tooltip("闪避输入缓存时间")]
        [SerializeField, Min(0f)] private float DodgeBufferTimeValue = 0.18f;

        public float DodgeSpeed => DodgeSpeedValue;

        public float DodgeDuration => DodgeDurationValue;

        public float DodgeInputThresholdSqr => DodgeInputThresholdSqrValue;

        public float DodgeBufferTime => DodgeBufferTimeValue;
    }
}
