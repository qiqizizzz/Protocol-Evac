/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色受击状态，负责受击硬直、移动锁定与击退位移
* │  类    名: ShooterHurtState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Combat.Damage;
using Module.ShootingPlayer.Context;
using Module.ShootingPlayer.Config;
using Module.ShootingPlayer.HFSM.Animation;
using Module.ShootingPlayer.HFSM.Runtime;
using UnityEngine;
using Utils.Timer;

namespace Module.ShootingPlayer.HFSM.States.Disabled
{
    // 受击状态，按受击动画的移动锁定窗口约束输入与位移
    public sealed class ShooterHurtState : BaseShooterState
    {
        private const float DEFAULT_HURT_DURATION = 0.3f;
        private const float MIN_FORCED_MOVE_SQR_MAGNITUDE = 0.0001f;

        private readonly ShooterContext m_context;
        private readonly ShooterRuntimeState m_runtimeState;
        private readonly ShooterDamageConfigSO m_damageConfig;

        private readonly DurationTimer m_hurtTimer;
        private readonly DurationTimer m_hurtAnimationTimer;
        private readonly DurationTimer m_forcedMoveTimer;

        private DamageReactionType m_reactionType;

        public override ShooterStateId Id => ShooterStateId.DisabledHurt;
        public override ShooterStateId ParentId => ShooterStateId.Disabled;

        public ShooterHurtState(ShooterContext context, ShooterRuntimeState runtimeState,
            ShooterDamageConfigSO damageConfig)
        {
            m_context = context;
            m_runtimeState = runtimeState;
            m_damageConfig = damageConfig;
            m_hurtTimer = new DurationTimer();
            m_hurtAnimationTimer = new DurationTimer();
            m_forcedMoveTimer = new DurationTimer();
        }

        // 进入受击状态并写入受击动画与击退速度
        public override void Enter()
        {
            m_reactionType = m_context.Damage.PendingReactionType;
            m_context.Damage.ConsumePendingHurt();
            m_context.Action.IsStateFinished = false;
            ClearHorizontalMove();
            m_forcedMoveTimer.Reset();

            ShooterHurtAnimationId animationId = m_reactionType switch
            {
                DamageReactionType.Light => ShooterHurtAnimationId.Hit1,
                DamageReactionType.Heavy => ShooterHurtAnimationId.Hit2,
                _ => ShooterHurtAnimationId.Stun
            };

            m_runtimeState.CurrentHurtAnimationId = animationId;
            m_runtimeState.ForceAnimatorRestart = true;
            m_context.Input.IsInputLocked = true;
            m_context.Movement.IsMovementLocked = true;

            m_hurtAnimationTimer.Start(GetHurtDuration(animationId));
            m_hurtTimer.Start(GetHurtDuration(animationId));
            SetKnockbackVelocity(animationId);
        }

        // 清理受击状态写入的控制与位移事实
        public override void Exit()
        {
            m_hurtTimer.Reset();
            m_hurtAnimationTimer.Reset();
            m_forcedMoveTimer.Reset();
            m_context.Action.IsStateFinished = false;
            m_context.Input.IsInputLocked = false;
            m_context.Movement.IsMovementLocked = false;
            ClearHorizontalMove();
            m_runtimeState.CurrentHurtAnimationId = ShooterHurtAnimationId.None;
        }

        public override void Tick(float deltaTime)
        {
            TickForcedMove(deltaTime);
            m_hurtAnimationTimer.Tick(deltaTime);
            m_hurtTimer.Tick(deltaTime);

            if (m_hurtTimer.IsFinished)
            {
                FinishHurt();
                return;
            }

            SyncMovementLock();
        }

        // 获取受击动画时长，配置缺失时使用兜底硬直
        private float GetHurtDuration(ShooterHurtAnimationId animationId)
        {
            float duration = m_damageConfig.GetHurtAnimationDuration(animationId);
            return duration > 0f ? duration : DEFAULT_HURT_DURATION;
        }

        // 根据受击动画的移动锁定窗口同步输入与位移约束
        private void SyncMovementLock()
        {
            bool isMovementLocked = m_damageConfig.IsHurtMovementLocked(
                m_runtimeState.CurrentHurtAnimationId, m_hurtAnimationTimer.NormalizedTime);

            if (!isMovementLocked)
                m_context.Input.Buffer.ClearAll();

            m_context.Input.IsInputLocked = isMovementLocked;
            m_context.Movement.IsMovementLocked = isMovementLocked;
        }

        // 结束受击硬直并立即开放状态转换
        private void FinishHurt()
        {
            m_context.Input.IsInputLocked = false;
            m_context.Movement.IsMovementLocked = false;
            m_context.Action.IsStateFinished = true;
        }

        // 写入当前受击动画配置的水平击退速度
        private void SetKnockbackVelocity(ShooterHurtAnimationId animationId)
        {
            float speed = m_damageConfig.GetHurtHorizontalKnockbackSpeed(animationId);
            float duration = m_damageConfig.GetHurtHorizontalKnockbackDuration(animationId);

            Vector3 knockbackDirection = m_context.Damage.PendingHitDirection;
            knockbackDirection.y = 0f;

            if (knockbackDirection.sqrMagnitude <= MIN_FORCED_MOVE_SQR_MAGNITUDE || speed <= 0f || duration <= 0f)
                return;

            m_context.Movement.SetForcedMoveVelocity(knockbackDirection.normalized * speed);
            m_forcedMoveTimer.Start(duration);
        }

        // 在水平击退脉冲结束后清理残留速度
        private void TickForcedMove(float deltaTime)
        {
            if (!m_forcedMoveTimer.IsRunning)
                return;

            m_forcedMoveTimer.Tick(deltaTime);
            if (!m_forcedMoveTimer.IsFinished)
                return;

            m_context.Movement.ClearForcedMoveVelocity();
            m_context.Movement.ClearHorizontalVelocity();
        }

        // 清空当前全部水平位移来源
        private void ClearHorizontalMove()
        {
            m_context.Movement.ClearForcedMoveVelocity();
            m_context.Movement.ClearHorizontalVelocity();
            m_context.Movement.ClearHorizontalMoveIntent();
        }
    }
}
