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
        private readonly PlayerContext m_context;
        private readonly PlayerDamageConfigSO m_damageConfig;
        private readonly DurationTimer m_hurtTimer;
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
            m_forcedMoveTimer = new DurationTimer();
        }

        // 进入受击状态并根据伤害反应初始化表现与位移
        public override void Enter()
        {
            m_reactionType = m_context.Damage.PendingReactionType;
            m_context.Damage.ConsumePendingHurt();
            m_context.Action.IsStateFinished = false;
            m_context.Input.IsInputLocked = true;
            m_context.Movement.IsMovementLocked = true;
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
                    StartSingleHurt(lightAnimationId, m_damageConfig.GetHurtDuration(lightAnimationId));
                    break;
                case DamageReactionType.Heavy:
                    PlayerHurtAnimationId heavyAnimationId = ResolveDirectionalAnimation(PlayerHurtAnimationId.HeavyLeft,
                        PlayerHurtAnimationId.HeavyRight);
                    StartSingleHurt(heavyAnimationId, m_damageConfig.GetHurtDuration(heavyAnimationId));
                    SetKnockbackVelocity(m_damageConfig.HeavyHurtKnockbackSpeed);
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
            m_forcedMoveTimer.Reset();
            m_context.Action.IsStateFinished = false;
            m_context.Input.IsInputLocked = false;
            m_context.Movement.IsMovementLocked = false;
            m_context.Movement.ClearForcedMoveVelocity();
            m_context.Movement.ClearHorizontalVelocity();
            m_context.Movement.ClearHorizontalMoveIntent();
            m_context.Damage.SetHurtAnimationId(PlayerHurtAnimationId.None);
        }

        // 推进普通受击硬直或击飞分段表现
        public override void Tick(float deltaTime)
        {
            TickForcedMove(deltaTime);

            if (m_reactionType != DamageReactionType.KnockUp)
            {
                m_hurtTimer.Tick(deltaTime);
                m_context.Action.IsStateFinished = m_hurtTimer.IsFinished;
                return;
            }

            TickKnockUp(deltaTime);
        }

        // 开始轻受击或重受击的单段表现
        private void StartSingleHurt(PlayerHurtAnimationId animationId, float duration)
        {
            m_context.Damage.SetHurtAnimationId(animationId);
            m_context.Action.RequestAnimReplay(PlayerStateId.DisabledHurt);
            m_hurtTimer.Start(duration);
        }

        // 开始击飞起始动作并写入初速度
        private void StartKnockUp()
        {
            m_context.Damage.SetHurtAnimationId(PlayerHurtAnimationId.KnockUpStart);
            m_context.Action.RequestAnimReplay(PlayerStateId.DisabledHurt);
            SetKnockbackVelocity(m_damageConfig.KnockUpHorizontalSpeed);
            Vector3 velocity = m_context.Movement.Velocity;
            velocity.y = m_damageConfig.KnockUpVerticalSpeed;
            m_context.Movement.Velocity = velocity;
            m_hurtTimer.Start(m_damageConfig.GetHurtDuration(PlayerHurtAnimationId.KnockUpStart));
        }

        // 推进击飞起始、滞空与落地三个动画阶段
        private void TickKnockUp(float deltaTime)
        {
            if (!m_hasLeftGround && !m_context.Movement.IsGrounded)
                m_hasLeftGround = true;

            if (m_context.Damage.HurtAnimationId == PlayerHurtAnimationId.KnockUpStart)
            {
                m_hurtTimer.Tick(deltaTime);
                if (!m_hurtTimer.IsFinished)
                    return;

                m_context.Damage.SetHurtAnimationId(PlayerHurtAnimationId.KnockUpLoop);
                m_context.Action.RequestAnimReplay(PlayerStateId.DisabledHurt);
                return;
            }

            if (!m_isKnockUpFalling && m_hasLeftGround && m_context.Movement.IsGrounded)
            {
                m_isKnockUpFalling = true;
                m_context.Damage.SetHurtAnimationId(PlayerHurtAnimationId.KnockUpFall);
                m_context.Movement.ClearForcedMoveVelocity();
                m_context.Movement.ClearHorizontalVelocity();
                m_context.Action.RequestAnimReplay(PlayerStateId.DisabledHurt);
                m_hurtTimer.Start(m_damageConfig.GetHurtDuration(PlayerHurtAnimationId.KnockUpFall));
                return;
            }

            if (!m_isKnockUpFalling)
                return;

            m_hurtTimer.Tick(deltaTime);
            m_context.Action.IsStateFinished = m_hurtTimer.IsFinished;
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

        // 写入向来袭反方向的水平强制位移
        private void SetKnockbackVelocity(float speed)
        {
            Vector3 knockbackDirection = -m_context.Damage.PendingHitDirection;
            knockbackDirection.y = 0f;
            if (knockbackDirection.sqrMagnitude <= 0.0001f || speed <= 0f ||
                m_damageConfig.HorizontalKnockbackDuration <= 0f)
                return;

            m_context.Movement.SetForcedMoveVelocity(knockbackDirection.normalized * speed);
            m_forcedMoveTimer.Start(m_damageConfig.HorizontalKnockbackDuration);
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
