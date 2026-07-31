/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家空中配置，保存跳跃与空中移动参数
 * │  类    名: PlayerAirConfigSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.HFSM.Config.Common;
using UnityEngine;

namespace Module.Player.HFSM.Config.Air
{
    [CreateAssetMenu(fileName = "PlayerAirConfig", menuName = "配置/玩家/空中/玩家空中配置")]
    public sealed class PlayerAirConfigSO : PlayerStateCommonConfigSO
    {
        [Header("跳跃")]
        [Tooltip("玩家起跳时写入的竖直速度")]
        [SerializeField, Min(0f)] private float JumpForceValue = 6f;

        [Header("空中移动")]
        [Tooltip("玩家空中水平移动速度")]
        [SerializeField, Min(0f)] private float AirMoveSpeedValue = 4f;

        [Header("输入容错")]
        [Tooltip("跳跃输入缓存时间")]
        [SerializeField, Min(0f)] private float JumpBufferTimeValue = 0.12f;
        [Tooltip("离开地面后仍允许跳跃的宽容时间")]
        [SerializeField, Min(0f)] private float CoyoteTimeValue = 0.1f;

        public float JumpForce => JumpForceValue;

        public float AirMoveSpeed => AirMoveSpeedValue;

        public float JumpBufferTime => JumpBufferTimeValue;

        public float CoyoteTime => CoyoteTimeValue;
    }
}
