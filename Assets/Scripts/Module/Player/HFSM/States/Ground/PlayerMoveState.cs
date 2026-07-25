/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家地面移动状态，负责写入移动方向与目标速度                      
 * │  类    名: PlayerMoveState.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Config.Move;
using Module.Player.Context;
using Module.Player.Core.View;
using UnityEngine;

namespace Module.Player.HFSM.States.Ground
{
    public class PlayerMoveState : BasePlayerState
    {
        private readonly PlayerContext m_context;
        private readonly PlayerMoveConfigSO m_moveConfig;

        public override PlayerStateId Id => PlayerStateId.GroundedMove;
        public override PlayerStateId ParentId => PlayerStateId.Grounded;
        
        public PlayerMoveState(PlayerContext context, PlayerMoveConfigSO moveConfig)
        {
            m_context = context;
            m_moveConfig = moveConfig;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            Vector2 moveInput = m_context.MoveInput;
            Vector3 moveDir = getViewRelativeMoveDirection(moveInput);

            m_context.MoveDir = moveDir;
            m_context.TargetMoveSpeed = m_context.IsSprintPressed ? m_moveConfig.SprintSpeed : m_moveConfig.WalkSpeed;
        }

        // 根据当前视角模式计算玩家移动方向
        private Vector3 getViewRelativeMoveDirection(Vector2 moveInput)
        {
            if (m_context.ViewMode == PlayerViewMode.FirstPerson)
                return buildMoveDirection(m_context.Transform.forward, m_context.Transform.right, moveInput);

            Quaternion cameraYawRotation = Quaternion.Euler(0f, m_context.CameraYaw, 0f);
            Vector3 cameraForward = cameraYawRotation * Vector3.forward;
            Vector3 cameraRight = cameraYawRotation * Vector3.right;

            return buildMoveDirection(cameraForward, cameraRight, moveInput);
        }

        // 使用水平前方向与右方向生成移动方向
        private Vector3 buildMoveDirection(Vector3 forward, Vector3 right, Vector2 moveInput)
        {
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return right * moveInput.x + forward * moveInput.y;
        }
    }
}
