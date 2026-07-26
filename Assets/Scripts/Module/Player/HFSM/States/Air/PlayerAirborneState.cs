/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家空中复合状态，负责空中移动意图与默认子状态
 * │  类    名: PlayerAirborneState.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.Config.Air;
using Module.Player.Context;
using Module.Player.Core.View;
using UnityEngine;

namespace Module.Player.HFSM.States.Air
{
    public sealed class PlayerAirborneState : PlayerCompositeState
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        private readonly PlayerContext m_context;
        private readonly PlayerAirConfigSO m_airConfig;

        public override PlayerStateId Id => PlayerStateId.Airborne;
        public override PlayerStateId ParentId => PlayerStateId.None;

        public PlayerAirborneState(PlayerContext context, PlayerAirConfigSO airConfig)
        {
            m_context = context;
            m_airConfig = airConfig;
        }

        public override PlayerStateId GetInitialChildId()
        {
            return PlayerStateId.AirborneFall;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            if (!canAirMove())
            {
                m_context.MoveDir = Vector3.zero;
                m_context.TargetMoveSpeed = 0f;
                return;
            }

            m_context.MoveDir = getViewRelativeMoveDirection(m_context.MoveInput);
            m_context.TargetMoveSpeed = m_airConfig.AirMoveSpeed;
        }

        // 判断玩家当前是否可以进行空中水平移动
        private bool canAirMove()
        {
            return
                !m_context.IsInputLocked &&
                !m_context.IsMovementLocked &&
                m_context.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
        }

        // 根据当前视角模式计算空中移动方向
        private Vector3 getViewRelativeMoveDirection(Vector2 moveInput)
        {
            if (m_context.ViewMode == PlayerViewMode.FirstPerson)
                return buildMoveDirection(m_context.Transform.forward, m_context.Transform.right, moveInput);

            Quaternion cameraYawRotation = Quaternion.Euler(0f, m_context.CameraYaw, 0f);
            Vector3 cameraForward = cameraYawRotation * Vector3.forward;
            Vector3 cameraRight = cameraYawRotation * Vector3.right;

            return buildMoveDirection(cameraForward, cameraRight, moveInput);
        }

        // 使用水平前方向与右方向生成空中移动方向
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
