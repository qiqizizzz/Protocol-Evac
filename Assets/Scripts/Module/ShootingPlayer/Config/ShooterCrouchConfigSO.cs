/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色蹲伏配置，保存蹲伏动画段落与蹲伏移速
* │  类    名: ShooterCrouchConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ChaisakiPlayer.HFSM.Config.Common;
using TriInspector;
using UnityEngine;

namespace Module.ShootingPlayer.Config
{
    [CreateAssetMenu(fileName = "ShooterCrouchConfig", menuName = "配置/射击玩家/蹲伏配置")]
    public sealed class ShooterCrouchConfigSO : PlayerStateCommonConfigSO
    {
        private const int CROUCH_IDLE_CLIP_INDEX = 0;
        private const int CROUCH_AIM_IDLE_CLIP_INDEX = 1;
        private const int CROUCH_AIM_MOVE_CLIP_INDEX = 2;

        public const int REQUIRED_STATE_CLIP_COUNT = 3;

        [Header("蹲伏移动")]
        [Tooltip("蹲伏移动速度")]
        [SerializeField, Min(0f)] private float CrouchMoveSpeedValue = 1.6f;
        [Tooltip("蹲伏瞄准移动速度")]
        [SerializeField, Min(0f)] private float CrouchAimMoveSpeedValue = 1.2f;

        public float CrouchMoveSpeed => CrouchMoveSpeedValue;

        public float CrouchAimMoveSpeed => CrouchAimMoveSpeedValue;

        public PlayerStateClipData CrouchIdleClipData => GetStateClip(CROUCH_IDLE_CLIP_INDEX);

        public PlayerStateClipData CrouchAimIdleClipData => GetStateClip(CROUCH_AIM_IDLE_CLIP_INDEX);

        public PlayerStateClipData CrouchAimMoveClipData => GetStateClip(CROUCH_AIM_MOVE_CLIP_INDEX);
    }
}
