/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色运行时状态，保存射击角色独有的运行时数据
* │  类    名: ShooterRuntimeState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ShootingPlayer.HFSM.Animation;

namespace Module.ShootingPlayer.HFSM.Runtime
{
    // 射击角色运行时状态，独立于 PlayerContext，避免污染千咲数据
    public sealed class ShooterRuntimeState
    {
        // 是否处于瞄准姿态
        public bool IsAiming { get; set; }

        // 是否处于蹲伏姿态
        public bool IsCrouching { get; set; }

        // 当前播放的受击动画标识
        public ShooterHurtAnimationId CurrentHurtAnimationId { get; set; }

        // 是否要求动画写入器下一帧从头重播目标动画
        public bool ForceAnimatorRestart { get; set; }

        // 下一次允许开火的最早时间，用于射击间隔
        public float NextShootAvailableTime { get; set; }

        // 重置运行时状态
        public void Reset()
        {
            IsAiming = false;
            IsCrouching = false;
            CurrentHurtAnimationId = ShooterHurtAnimationId.None;
            ForceAnimatorRestart = false;
            NextShootAvailableTime = float.NegativeInfinity;
        }
    }
}
