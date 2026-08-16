/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: 玩家受击状态，负责受击动画、硬直与击飞位移的执行
 * │  类    名: PlayerHurtState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using Module.Combat.Damage;
using Module.Player.Context;
using Module.Player.HFSM.Animation.Type;
using Module.Player.HFSM.Config.Disabled;
using UnityEngine;
using Utils.Timer;

namespace Module.Player.HFSM.States.Disabled
{
    public sealed class PlayerHurtState : BasePlayerState
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        private readonly PlayerContext m_context;
        private readonly PlayerDamageConfigSO m_damageConfig;
        private readonly DurationTimer m_hurtTimer;
        private readonly DurationTimer m_hurtAnimationTimer;
        private readonly DurationTimer m_forcedMoveTimer;

        private DamageReactionType m_reactionType;
        private bool m_hasLeftGround;
        private bool m_isKnockUpFalling;

        public override PlayerStateId Id => PlayerStateId.DisabledHurt;
        public override PlayerStateId ParentId => PlayerStateId.Disabled;

        public PlayerHurtState(PlayerContext context, PlayerDamageConfigSO damageConfig)
        {
            m_context = context;
            m_damageConfig = damageConfig;
            m_hurtTimer = new DurationTimer();
            m_hurtAnimationTimer = new DurationTimer();
            m_forcedMoveTimer = new DurationTimer();
        }

        // 进入受击状态并根据伤害反应初始化表现与位移
        public override void Enter()
        {
            m_reactionType = m_context.Damage.PendingReactionType;
            m_context.Damage.ConsumePendingHurt();
            m_context.Damage.SetHurtCancellationEnabled(false);
            m_context.Action.IsStateFinished = false;
            m_context.Movement.ClearForcedMoveVelocity();
            m_context.Movement.ClearHorizontalVelocity();
            m_context.Movement.ClearHorizontalMoveIntent();
            m_context.Action.SetRootMotionMoveEnabled(false);
            m_context.Action.ClearRootMotionDeltaPosition();
            m_forcedMoveTimer.Reset();
            m_hasLeftGround = false;
            m_isKnockUpFalling = false;

            switch (m_reactionType)
            {
                case DamageReactionType.Light:
                    PlayerHurtAnimationId lightAnimationId = ResolveDirectionalAnimation(PlayerHurtAnimationId.LightLeft,
                        PlayerHurtAnimationId.LightRight);
                    StartSingleHurt(lightAnimationId, m_damageConfig.GetHurtAnimationDuration(lightAnimationId));
                    break;
                case DamageReactionType.Heavy:
                    PlayerHurtAnimationId heavyAnimationId = ResolveDirectionalAnimation(PlayerHurtAnimationId.HeavyLeft,
                        PlayerHurtAnimationId.HeavyRight);
                    StartSingleHurt(heavyAnimationId, m_damageConfig.GetHurtAnimationDuration(heavyAnimationId));
                    break;
                case DamageReactionType.KnockUp:
                    StartKnockUp();
                    break;
            }
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
            m_context.Movement.ClearForcedMoveVelocity();
            m_context.Movement.ClearHorizontalVelocity();
            m_context.Movement.ClearHorizontalMoveIntent();
            m_context.Damage.SetHurtCancellationEnabled(false);
            m_context.Damage.SetHurtAnimationId(PlayerHurtAnimationId.None);
            RequestGroundedRecoveryAnimation();
        }

        // 推进普通受击硬直或击飞分段表现
        public override void Tick(float deltaTime)
        {
            TickForcedMove(deltaTime);

            if (m_reactionType != DamageReactionType.KnockUp)
            {
                TickHurtAnimation(deltaTime, false);
                m_hurtTimer.Tick(deltaTime);
                if (m_hurtTimer.IsFinished)
                    FinishHurt();
                return;
            }

            TickKnockUp(deltaTime);
        }

        // 开始轻受击或重受击的单段表现
        private void StartSingleHurt(PlayerHurtAnimationId animationId, float duration)
        {
            StartHurtAnimation(animationId);
            SetKnockbackVelocity(animationId);
            m_hurtTimer.Start(duration);
        }

        // 开始击飞起始动作并写入初速度
        private void StartKnockUp()
        {
            StartHurtAnimation(PlayerHurtAnimationId.KnockUpStart);
            SetKnockbackVelocity(PlayerHurtAnimationId.KnockUpStart);
            Vector3 velocity = m_context.Movement.Velocity;
            velocity.y = m_damageConfig.GetHurtVerticalLaunchSpeed(PlayerHurtAnimationId.KnockUpStart);
            m_context.Movement.Velocity = velocity;
            m_hurtTimer.Start(m_damageConfig.GetHurtAnimationDuration(PlayerHurtAnimationId.KnockUpStart));
        }

        // 推进击飞起始、滞空与落地三个动画阶段
        private void TickKnockUp(float deltaTime)
        {
            TickHurtAnimation(deltaTime,
                m_context.Damage.HurtAnimationId == PlayerHurtAnimationId.KnockUpLoop);

            if (!m_hasLeftGround && !m_context.Movement.IsGrounded)
                m_hasLeftGround = true;

            if (m_context.Damage.HurtAnimationId == PlayerHurtAnimationId.KnockUpStart)
            {
                m_hurtTimer.Tick(deltaTime);
                if (!m_hurtTimer.IsFinished)
                    return;

                StartHurtAnimation(PlayerHurtAnimationId.KnockUpLoop);
                return;
            }

            if (!m_isKnockUpFalling && m_hasLeftGround && m_context.Movement.IsGrounded)
            {
                m_isKnockUpFalling = true;
                StartHurtAnimation(PlayerHurtAnimationId.KnockUpFall);
                m_context.Movement.ClearForcedMoveVelocity();
                m_context.Movement.ClearHorizontalVelocity();
                m_hurtTimer.Start(m_damageConfig.GetHurtAnimationDuration(PlayerHurtAnimationId.KnockUpFall));
                return;
            }

            if (!m_isKnockUpFalling)
                return;

            m_hurtTimer.Tick(deltaTime);
            if (m_hurtTimer.IsFinished)
                FinishHurt();
        }

        // 开始指定受击动画并初始化窗口采样计时
        private void StartHurtAnimation(PlayerHurtAnimationId animationId)
        {
            m_context.Damage.SetHurtAnimationId(animationId);
            m_context.Action.RequestAnimReplay(PlayerStateId.DisabledHurt);
            m_hurtAnimationTimer.Start(m_damageConfig.GetHurtAnimationDuration(animationId));
            SyncMovementLock();
        }

        // 推进受击动画窗口时间并同步移动锁定状态
        private void TickHurtAnimation(float deltaTime, bool loop)
        {
            m_hurtAnimationTimer.Tick(deltaTime);
            if (loop && m_hurtAnimationTimer.IsFinished)
                m_hurtAnimationTimer.Start(m_hurtAnimationTimer.Duration);

            SyncMovementLock();
        }

        // 根据当前受击动画的移动锁定窗口同步输入与移动约束
        private void SyncMovementLock()
        {
            bool isMovementLocked = m_damageConfig.IsHurtMovementLocked(
                m_context.Damage.HurtAnimationId, m_hurtAnimationTimer.NormalizedTime);
            if (!isMovementLocked && !m_context.Damage.IsHurtCancellationEnabled)
                m_context.Input.Buffer.ClearAll();

            m_context.Input.IsInputLocked = isMovementLocked;
            m_context.Movement.IsMovementLocked = isMovementLocked;
            m_context.Damage.SetHurtCancellationEnabled(!isMovementLocked);
        }

        // 完成受击控制窗口并立即开放状态转换
        private void FinishHurt()
        {
            m_context.Input.IsInputLocked = false;
            m_context.Movement.IsMovementLocked = false;
            m_context.Action.IsStateFinished = true;
        }

        // 受击退出到地面状态时立即切回对应移动动画
        private void RequestGroundedRecoveryAnimation()
        {
            if (!m_context.Movement.IsGrounded)
                return;

            PlayerStateId targetStateId = m_context.Input.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR
                ? PlayerStateId.GroundedMove
                : PlayerStateId.GroundedIdle;
            m_context.Action.RequestAnimReplay(targetStateId);
        }

        // 根据来袭方向选择左侧或右侧受击动画
        private PlayerHurtAnimationId ResolveDirectionalAnimation(PlayerHurtAnimationId leftAnimationId,
            PlayerHurtAnimationId rightAnimationId)
        {
            Vector3 sourceDirection = -m_context.Damage.PendingHitDirection;
            sourceDirection.y = 0f;
            return Vector3.Dot(sourceDirection, m_context.Transform.right) >= 0f
                ? rightAnimationId
                : leftAnimationId;
        }

        // 写入当前受击动画配置的水平强制位移
        private void SetKnockbackVelocity(PlayerHurtAnimationId animationId)
        {
            float speed = m_damageConfig.GetHurtHorizontalKnockbackSpeed(animationId);
            float duration = m_damageConfig.GetHurtHorizontalKnockbackDuration(animationId);
            Vector3 knockbackDirection = m_context.Damage.PendingHitDirection;
            knockbackDirection.y = 0f;
            if (knockbackDirection.sqrMagnitude <= 0.0001f || speed <= 0f || duration <= 0f)
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
    }
}
