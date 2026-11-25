/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色瞄准配置，保存瞄准动画段落与瞄准移速
* │  类    名: ShooterAimConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ChaisakiPlayer.HFSM.Config.Common;
using TriInspector;
using UnityEngine;

namespace Module.ShootingPlayer.Config
{
    [CreateAssetMenu(fileName = "ShooterAimConfig", menuName = "配置/射击玩家/瞄准配置")]
    public sealed class ShooterAimConfigSO : PlayerStateCommonConfigSO
    {
        private const int AIM_IDLE_CLIP_INDEX = 0;
        private const int AIM_WALK_CLIP_INDEX = 1;
        private const int AIM_JOG_CLIP_INDEX = 2;

        public const int REQUIRED_STATE_CLIP_COUNT = 3;

        [Header("瞄准移动")]
        [Tooltip("瞄准时允许移动")]
        [SerializeField] private bool CanMoveWhileAimingValue = true;
        [Tooltip("瞄准状态移动速度")]
        [SerializeField, Min(0f)] private float AimMoveSpeedValue = 2.2f;
        [Tooltip("进入瞄准动画的淡入时长")]
        [SerializeField, Min(0f)] private float EnterAimBlendDurationValue = 0.15f;
        [Tooltip("退出瞄准动画的淡入时长")]
        [SerializeField, Min(0f)] private float ExitAimBlendDurationValue = 0.15f;

        public bool CanMoveWhileAiming => CanMoveWhileAimingValue;

        public float AimMoveSpeed => AimMoveSpeedValue;

        public float EnterAimBlendDuration => EnterAimBlendDurationValue;

        public float ExitAimBlendDuration => ExitAimBlendDurationValue;

        public PlayerStateClipData AimIdleClipData => GetStateClip(AIM_IDLE_CLIP_INDEX);

        public PlayerStateClipData AimWalkClipData => GetStateClip(AIM_WALK_CLIP_INDEX);

        public PlayerStateClipData AimJogClipData => GetStateClip(AIM_JOG_CLIP_INDEX);
    }
}
