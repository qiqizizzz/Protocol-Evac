/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色移动执行器，基于 CharacterController 执行水平与竖直位移
* │  类    名: ShooterMotor.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ChaisakiPlayer.Context;
using Module.ShootingPlayer.Config;
using UnityEngine;

namespace Module.ShootingPlayer.Core
{
    // 射击角色移动执行器，不做身体转向与根运动，朝向由第一人称视角控制
    public sealed class ShooterMotor
    {
        private const float GROUNDED_STICK_VELOCITY = -2f;

        private CharacterController m_characterController;
        private PlayerContext m_context;
        private ShooterMoveConfigSO m_moveConfig;

        // 初始化移动执行器依赖
        public void Init(CharacterController characterController, PlayerContext context,
            ShooterMoveConfigSO moveConfig)
        {
            m_characterController = characterController;
            m_context = context;
            m_moveConfig = moveConfig;
        }

        // 固定帧执行位移
        public void FixedTick(float fixedDeltaTime)
        {
            Vector3 velocity = m_context.Movement.Velocity;
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

            if (m_context.Movement.HasForcedMoveVelocity)
            {
                Vector3 forcedMoveVelocity = m_context.Movement.ForcedMoveVelocity;
                horizontalVelocity = new Vector3(forcedMoveVelocity.x, 0f, forcedMoveVelocity.z);
            }
            else if (!m_context.Movement.IsMovementLocked)
            {
                Vector3 targetHorizontalVelocity =
                    m_context.Movement.MoveDir.normalized * m_context.Movement.TargetMoveSpeed;

                // 目标速度大于当前速度时使用加速，否则使用减速
                float speedChangeRate = targetHorizontalVelocity.sqrMagnitude > horizontalVelocity.sqrMagnitude
                    ? m_moveConfig.Acceleration
                    : m_moveConfig.Deceleration;

                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetHorizontalVelocity,
                    speedChangeRate * fixedDeltaTime);
            }
            else
            {
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero,
                    m_moveConfig.Deceleration * fixedDeltaTime);
            }

            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;

            if (m_characterController.isGrounded && velocity.y < 0f)
                velocity.y = GROUNDED_STICK_VELOCITY;
            else
                velocity.y += Physics.gravity.y * fixedDeltaTime;

            m_characterController.Move(velocity * fixedDeltaTime);

            m_context.Movement.Velocity = velocity;
            m_context.Movement.IsGrounded = m_characterController.isGrounded;
            m_context.Movement.HasGroundedChecked = true;
            if (m_context.Movement.IsGrounded)
                m_context.Movement.LastGroundedTime = Time.time;
        }
    }
}
