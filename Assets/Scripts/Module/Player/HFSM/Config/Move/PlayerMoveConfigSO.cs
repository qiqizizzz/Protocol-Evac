/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家移动配置，保存地面动画、移动与地面检测参数
 * │  类    名: PlayerMoveConfigSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.Context.Runtime;
using Module.Player.HFSM.Config.Common;
using TriInspector;
using UnityEngine;

namespace Module.Player.HFSM.Config.Move
{
    [CreateAssetMenu(fileName = "PlayerMoveConfig",menuName = "配置/玩家/移动/玩家移动配置")]
    [DeclareFoldoutGroup("StopAnimation", Title = "急停动画段落")]
    public sealed class PlayerMoveConfigSO : PlayerStateCommonConfigSO
    {
        private const int IDLE_CLIP_INDEX = 0;
        private const int WALK_CLIP_INDEX = 1;
        private const int RUN_CLIP_INDEX = 2;
        private const int SPRINT_RUN_CLIP_INDEX = 3;

        public const int REQUIRED_STATE_CLIP_COUNT = 4;

        [Group("StopAnimation")]
        [LabelText("步行急停")]
        [SerializeField] private PlayerStopClipPairData WalkStopClipPairValue;
        [Group("StopAnimation")]
        [LabelText("奔跑急停")]
        [SerializeField] private PlayerStopClipPairData RunStopClipPairValue;
        [Group("StopAnimation")]
        [LabelText("疾跑急停")]
        [SerializeField] private PlayerStopClipPairData SprintStopClipPairValue;

        [Header("地面移动")]
        [Tooltip("步行移动速度")]
        [SerializeField, Min(0f)] private float WalkSpeedValue = 2.5f;
        [Tooltip("默认奔跑移动速度")]
        [SerializeField, Min(0f)] private float RunSpeedValue = 4f;
        [Tooltip("玩家疾跑速度")]
        [SerializeField, Min(0f)] private float SprintSpeedValue = 6f;
        [Tooltip("玩家加速到目标速度的速率")]
        [SerializeField, Min(0f)] private float AccelerationValue = 20f;
        [Tooltip("玩家减速到停止状态的速率")]
        [SerializeField, Min(0f)] private float DecelerationValue = 25f;
        [Tooltip("玩家每秒最大旋转角度")]
        [SerializeField, Min(0f)] private float RotationSpeedValue = 720f;

        [Header("地面检测")]
        [Tooltip("地面检测向下延伸的距离")]
        [SerializeField, Min(0f)] private float GroundCheckDistanceValue = 0.2f;
        [Tooltip("被识别为地面的 Layer")]
        [SerializeField] private LayerMask GroundLayerValue;
        
        public float WalkSpeed => WalkSpeedValue;

        public float RunSpeed => RunSpeedValue;

        public float SprintSpeed => SprintSpeedValue;

        public float Acceleration => AccelerationValue;

        public float Deceleration => DecelerationValue;

        public float RotationSpeed => RotationSpeedValue;

        public float GroundCheckDistance => GroundCheckDistanceValue;

        public LayerMask GroundLayer => GroundLayerValue;

        public PlayerStateClipData IdleClipData => GetStateClip(IDLE_CLIP_INDEX);

        public PlayerStateClipData WalkClipData => GetStateClip(WALK_CLIP_INDEX);

        public PlayerStateClipData RunClipData => GetStateClip(RUN_CLIP_INDEX);

        public PlayerStateClipData SprintRunClipData => GetStateClip(SPRINT_RUN_CLIP_INDEX);

        // 同步移动配置中的急停动画时长
        protected override bool SyncAdditionalClipDurations()
        {
            bool hasSynced = false;
            hasSynced |= WalkStopClipPairValue != null && WalkStopClipPairValue.SyncDurationsFromClips();
            hasSynced |= RunStopClipPairValue != null && RunStopClipPairValue.SyncDurationsFromClips();
            hasSynced |= SprintStopClipPairValue != null && SprintStopClipPairValue.SyncDurationsFromClips();
            return hasSynced;
        }

        // 获取指定急停动作的左右动作对
        public PlayerStopClipPairData GetStopClipPairData(PlayerStopAnimationId animationId)
        {
            return animationId switch
            {
                PlayerStopAnimationId.WalkLeft => WalkStopClipPairValue,
                PlayerStopAnimationId.WalkRight => WalkStopClipPairValue,
                PlayerStopAnimationId.RunLeft => RunStopClipPairValue,
                PlayerStopAnimationId.RunRight => RunStopClipPairValue,
                PlayerStopAnimationId.SprintLeft => SprintStopClipPairValue,
                PlayerStopAnimationId.SprintRight => SprintStopClipPairValue,
                _ => null
            };
        }
    }
}
