/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家移动执行器，负责基于CharacterController执行最终位移                      
 * │  类    名: PlayerMotor.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Core.View;
using Module.Player.Core.View.Config;
using Module.Player.HFSM.Config.Air;
using Module.Player.HFSM.Config.Move;
using UnityEngine;

namespace Module.Player.Core
{
    public sealed class PlayerMotor
    {
        private CharacterController m_characterController;
        private PlayerContext m_context;
        private PlayerMoveConfigSO m_moveConfig;
        private PlayerViewConfigSO m_viewConfig;
        private PlayerAirConfigSO m_airConfig;
        
        // 初始化玩家移动执行器
        public void Init(CharacterController characterController, PlayerContext context, PlayerMoveConfigSO moveConfig, PlayerViewConfigSO viewConfig, PlayerAirConfigSO airConfig)
        {
            m_characterController = characterController;
            m_context = context;
            m_moveConfig = moveConfig;
            m_viewConfig = viewConfig;
            m_airConfig = airConfig;
        }

        //固定帧移动
        public void FixedTick(float fixedDeltaTime)
        {
            Vector3 velocity = m_context.Movement.Velocity;
            if (m_context.View.ViewMode == PlayerViewMode.ThirdPerson
                && m_context.Movement.StopDirection.sqrMagnitude > 0.0001f)
            {
                RotateByDirection(m_context.Movement.StopDirection, fixedDeltaTime);
            }

            Vector3 rootMotionDeltaPosition = m_context.Action.ConsumeRootMotionDeltaPosition();
            if (rootMotionDeltaPosition.sqrMagnitude > 0f && !m_context.Movement.HasForcedMoveVelocity)
            {
                ApplyRootMotionMove(rootMotionDeltaPosition, ref velocity, fixedDeltaTime);
                return;
            }

            Vector3 hVelocity = new Vector3(velocity.x, 0f, velocity.z);

            //水平移动
            Vector3 targetHVelocity = Vector3.zero;
            if (m_context.Movement.HasForcedMoveVelocity)
            {
                Vector3 forcedMoveVelocity = m_context.Movement.ForcedMoveVelocity;
                hVelocity = new Vector3(forcedMoveVelocity.x, 0f, forcedMoveVelocity.z);
            }
            else if (!m_context.Movement.IsMovementLocked)
            {
                targetHVelocity = m_context.Movement.MoveDir.normalized * m_context.Movement.TargetMoveSpeed;

                //sqrMagnitude是计算向量长度平方的方法
                //如果目标速度比当前速度大则使用加速度，否则使用减速度
                float speedChangeRate = targetHVelocity.sqrMagnitude > hVelocity.sqrMagnitude
                    ? m_moveConfig.Acceleration
                    : m_moveConfig.Deceleration;

                hVelocity = Vector3.MoveTowards(hVelocity, targetHVelocity, speedChangeRate * fixedDeltaTime);
            }
            else
            {
                hVelocity = Vector3.MoveTowards(hVelocity, targetHVelocity, m_moveConfig.Deceleration * fixedDeltaTime);
            }

            velocity.x = hVelocity.x;
            velocity.z = hVelocity.z;

            if (m_context.View.ViewMode == PlayerViewMode.ThirdPerson)
            {
                if (m_context.Movement.HasForcedMoveVelocity)
                    RotateByDirection(m_context.Movement.ForcedMoveVelocity, fixedDeltaTime);
                else if (m_context.View.IsLockOn)
                    RotateByDirection(m_context.View.LockTarget.position - m_context.Transform.position, fixedDeltaTime);
                else if (!m_context.Movement.IsMovementLocked)
                    RotateByDirection(m_context.Movement.MoveDir, fixedDeltaTime);
            }
            
            //竖直移动
            if (m_characterController.isGrounded && velocity.y < 0f)
                velocity.y = -2f; // 保持角色贴地，避免浮空
            else
                velocity.y += GetVerticalGravity(velocity.y) * fixedDeltaTime;

            m_characterController.Move(velocity * fixedDeltaTime);

            //更新状态
            m_context.Movement.Velocity = velocity;
            m_context.Movement.IsGrounded = m_characterController.isGrounded;
            m_context.Movement.HasGroundedChecked = true;
            if (m_context.Movement.IsGrounded)
                m_context.Movement.LastGroundedTime = Time.time;
        }

        // 使用动画根运动驱动本次固定帧位移
        private void ApplyRootMotionMove(Vector3 rootMotionDeltaPosition, ref Vector3 velocity, float fixedDeltaTime)
        {
            if (m_characterController.isGrounded && velocity.y < 0f)
                velocity.y = -2f;
            else
                velocity.y += GetVerticalGravity(velocity.y) * fixedDeltaTime;

            rootMotionDeltaPosition.y = velocity.y * fixedDeltaTime;
            m_characterController.Move(rootMotionDeltaPosition);

            velocity.x = 0f;
            velocity.z = 0f;
            m_context.Movement.Velocity = velocity;
            m_context.Movement.IsGrounded = m_characterController.isGrounded;
            m_context.Movement.HasGroundedChecked = true;
            if (m_context.Movement.IsGrounded)
                m_context.Movement.LastGroundedTime = Time.time;
        }

        // 根据当前竖直速度计算本帧使用的重力
        private float GetVerticalGravity(float verticalVelocity)
        {
            if (verticalVelocity < 0f)
                return Physics.gravity.y * m_airConfig.FallGravityMultiplier;

            return Physics.gravity.y;
        }

        // 根据移动方向旋转玩家身体
        public void RotateByDirection(Vector3 moveDirection, float deltaTime)
        {
            moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude <= 0.01f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            m_context.Transform.rotation = Quaternion.RotateTowards(
                m_context.Transform.rotation,
                targetRotation,
                m_viewConfig.ThirdPersonBodyTurnSpeed * deltaTime);
        }
    }
}
