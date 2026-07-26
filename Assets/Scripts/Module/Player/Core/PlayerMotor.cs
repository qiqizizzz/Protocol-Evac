/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家移动执行器，负责基于CharacterController执行最终位移                      
 * │  类    名: PlayerMotor.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Config.Move;
using Module.Player.Config.View;
using Module.Player.Context;
using Module.Player.Core.View;
using UnityEngine;

namespace Module.Player.Core
{
    public sealed class PlayerMotor
    {
        private CharacterController m_characterController;
        private PlayerContext m_context;
        private PlayerMoveConfigSO m_moveConfig;
        private PlayerViewConfigSO m_viewConfig;
        
        // 初始化玩家移动执行器
        public void Init(CharacterController characterController, PlayerContext context, PlayerMoveConfigSO moveConfig, PlayerViewConfigSO viewConfig)
        {
            m_characterController = characterController;
            m_context = context;
            m_moveConfig = moveConfig;
            m_viewConfig = viewConfig;
        }

        //固定帧移动
        public void FixedTick(float fixedDeltaTime)
        {
            Vector3 velocity = m_context.Velocity;
            Vector3 hVelocity = new Vector3(velocity.x, 0f, velocity.z);

            //水平移动
            Vector3 targetHVelocity = Vector3.zero;
            if (!m_context.IsMovementLocked)
                targetHVelocity = m_context.MoveDir.normalized * m_context.TargetMoveSpeed;

            //sqrMagnitude是计算向量长度平方的方法
            //如果目标速度比当前速度大则使用加速度，否则使用减速度
            float speedChangeRate = targetHVelocity.sqrMagnitude > hVelocity.sqrMagnitude
                ? m_moveConfig.Acceleration
                : m_moveConfig.Deceleration;

            hVelocity = Vector3.MoveTowards(hVelocity, targetHVelocity, speedChangeRate * fixedDeltaTime);

            velocity.x = hVelocity.x;
            velocity.z = hVelocity.z;

            if (m_context.ViewMode == PlayerViewMode.ThirdPerson && !m_context.IsMovementLocked)
                RotateByDirection(m_context.MoveDir, fixedDeltaTime);
            
            //竖直移动
            if (m_characterController.isGrounded && velocity.y < 0f)
                velocity.y = -2f; // 保持角色贴地，避免浮空
            else
                velocity.y += Physics.gravity.y * fixedDeltaTime;

            m_characterController.Move(velocity * fixedDeltaTime);

            //更新状态
            m_context.Velocity = velocity;
            m_context.IsGrounded = m_characterController.isGrounded;
            m_context.HasGroundedChecked = true;
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
