/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色地面移动配置，保存站立动画段落与移动参数
* │  类    名: ShooterMoveConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ChaisakiPlayer.HFSM.Config.Common;
using TriInspector;
using UnityEngine;

namespace Module.ShootingPlayer.Config
{
    [CreateAssetMenu(fileName = "ShooterMoveConfig", menuName = "配置/射击玩家/移动配置")]
    public sealed class ShooterMoveConfigSO : PlayerStateCommonConfigSO
    {
        private const int IDLE_CLIP_INDEX = 0;
        private const int WALK_CLIP_INDEX = 1;
        private const int RUN_CLIP_INDEX = 2;

        public const int REQUIRED_STATE_CLIP_COUNT = 3;

        [Header("地面移动")]
        [Tooltip("步行移动速度")]
        [SerializeField, Min(0f)] private float WalkSpeedValue = 2.2f;
        [Tooltip("奔跑移动速度")]
        [SerializeField, Min(0f)] private float RunSpeedValue = 4.5f;
        [Tooltip("加速到目标速度的速率")]
        [SerializeField, Min(0f)] private float AccelerationValue = 24f;
        [Tooltip("减速到停止状态的速率")]
        [SerializeField, Min(0f)] private float DecelerationValue = 28f;

        [Header("地面检测")]
        [Tooltip("地面检测向下延伸的距离")]
        [SerializeField, Min(0f)] private float GroundCheckDistanceValue = 0.2f;
        [Tooltip("被识别为地面的 Layer")]
        [SerializeField] private LayerMask GroundLayerValue;

        public float WalkSpeed => WalkSpeedValue;

        public float RunSpeed => RunSpeedValue;

        public float Acceleration => AccelerationValue;

        public float Deceleration => DecelerationValue;

        public float GroundCheckDistance => GroundCheckDistanceValue;

        public LayerMask GroundLayer => GroundLayerValue;

        public PlayerStateClipData IdleClipData => GetStateClip(IDLE_CLIP_INDEX);

        public PlayerStateClipData WalkClipData => GetStateClip(WALK_CLIP_INDEX);

        public PlayerStateClipData RunClipData => GetStateClip(RUN_CLIP_INDEX);
    }
}
