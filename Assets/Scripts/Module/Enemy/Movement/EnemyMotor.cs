/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人移动执行器，负责角色位移、重力与身体转向
 * │  类    名: EnemyMotor.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using Module.Enemy.Context;
using Module.Enemy.Movement.Config;
using UnityEngine;

namespace Module.Enemy.Movement
{
    public sealed class EnemyMotor
    {
        private const float GROUNDED_VERTICAL_SPEED = -2f;

        private CharacterController m_characterController;
        private EnemyContext m_context;
        private EnemyMoveConfigSO m_moveConfig;
        private float m_verticalSpeed;

        // 初始化敌人移动执行器
        public void Init(CharacterController characterController, EnemyContext context, EnemyMoveConfigSO moveConfig)
        {
            m_characterController = characterController;
            m_context = context;
            m_moveConfig = moveConfig;
            m_verticalSpeed = 0f;
        }

        // 在固定帧执行敌人位移、重力与旋转
        public void FixedTick(float fixedDeltaTime)
        {
            Vector3 horizontalVelocity = Vector3.zero;
            if (m_context.Movement.HasForcedMoveVelocity)
                horizontalVelocity = m_context.Movement.ForcedMoveVelocity;
            else if (m_context.Movement.HasMoveRequest && !m_context.Action.IsMovementLocked)
                horizontalVelocity = m_context.Movement.MoveDirection * m_moveConfig.MoveSpeedValue;

            Rotate(fixedDeltaTime);
            if (m_context.Movement.TryConsumeVerticalLaunch(out float verticalLaunchSpeed))
                m_verticalSpeed = verticalLaunchSpeed;
            else if (m_characterController.isGrounded && m_verticalSpeed < 0f)
                m_verticalSpeed = GROUNDED_VERTICAL_SPEED;
            else
                m_verticalSpeed += Physics.gravity.y * fixedDeltaTime;

            Vector3 velocity = horizontalVelocity + Vector3.up * m_verticalSpeed;
            m_characterController.Move(velocity * fixedDeltaTime);
            m_context.Movement.TickForcedMove(fixedDeltaTime);
            m_context.Movement.SetMoving(horizontalVelocity.sqrMagnitude > 0f && !m_context.Action.IsHurt);
        }

        // 重置移动执行器的竖直速度
        public void Reset()
        {
            m_verticalSpeed = 0f;
            m_context.Movement.Reset();
        }

        // 根据技能目标或行为请求旋转敌人身体
        private void Rotate(float deltaTime)
        {
            if (m_context.Action.IsHurt || m_context.Action.IsDead)
                return;

            Vector3 lookDirection;
            if (m_context.Action.CurrentSkillType.HasValue && m_context.Action.CanRotate
                && m_context.Target.CurrentTarget != null)
            {
                lookDirection = m_context.Target.CurrentTarget.position - m_context.Transform.position;
            }
            else if (m_context.Movement.HasLookRequest)
            {
                lookDirection = m_context.Movement.LookDirection;
            }
            else
            {
                lookDirection = m_context.Movement.MoveDirection;
            }

            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude <= 0.01f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
            m_context.Transform.rotation = Quaternion.RotateTowards(m_context.Transform.rotation,
                targetRotation, m_moveConfig.TurnSpeedValue * deltaTime);
        }
    }
}
