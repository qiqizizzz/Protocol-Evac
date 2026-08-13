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
        private const int JUMP_BEGIN_CLIP_INDEX = 0;
        private const int FALL_LOOP_CLIP_INDEX = 1;
        private const int FALL_END_CLIP_INDEX = 2;

        public const int REQUIRED_STATE_CLIP_COUNT = 3;

        [Header("跳跃")]
        [Tooltip("玩家起跳时写入的竖直速度")]
        [SerializeField, Min(0f)] private float JumpForceValue = 6f;

        [Tooltip("玩家下落阶段的重力倍率")]
        [SerializeField, Min(1f)] private float FallGravityMultiplierValue = 2f;

        [Header("空中移动")]
        [Tooltip("玩家空中水平移动速度")]
        [SerializeField, Min(0f)] private float AirMoveSpeedValue = 4f;

        [Header("输入容错")]
        [Tooltip("跳跃输入缓存时间")]
        [SerializeField, Min(0f)] private float JumpBufferTimeValue = 0.12f;
        [Tooltip("离开地面后仍允许跳跃的宽容时间")]
        [SerializeField, Min(0f)] private float CoyoteTimeValue = 0.1f;

        public float JumpForce => JumpForceValue;

        public float FallGravityMultiplier => FallGravityMultiplierValue;

        public float AirMoveSpeed => AirMoveSpeedValue;

        public float JumpBufferTime => JumpBufferTimeValue;

        public float CoyoteTime => CoyoteTimeValue;

        public PlayerStateClipData JumpBeginClipData => GetStateClip(JUMP_BEGIN_CLIP_INDEX);

        public PlayerStateClipData FallLoopClipData => GetStateClip(FALL_LOOP_CLIP_INDEX);

        public PlayerStateClipData FallEndClipData => GetStateClip(FALL_END_CLIP_INDEX);
    }
}
