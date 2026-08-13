/*
 * ┌─────────────────────────────────────────────┐
 * │  描    述: 玩家地面急停状态，负责选择急停动画并等待其播放结束
 * │  类    名: PlayerStopState.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Context.Runtime;
using Module.Player.HFSM.Config.Move;
using UnityEngine;
using Utils.Timer;

namespace Module.Player.HFSM.States.Ground
{
    public sealed class PlayerStopState : BasePlayerState
    {
        private const float STOP_EXIT_BLEND_DURATION = 0f;
        private readonly PlayerContext m_context;
        private readonly PlayerMoveConfigSO m_moveConfig;
        private readonly DurationTimer m_stopTimer;

        public override PlayerStateId Id => PlayerStateId.GroundedStop;
        public override PlayerStateId ParentId => PlayerStateId.Grounded;

        public PlayerStopState(PlayerContext context, PlayerMoveConfigSO moveConfig)
        {
            m_context = context;
            m_moveConfig = moveConfig;
            m_stopTimer = new DurationTimer();
        }

        // 进入急停状态并按当前实际速度选择对应动作
        public override void Enter()
        {
            PlayerStopAnimationId animationId = SelectStopAnimationId();
            PlayerStopClipPairData clipPairData = m_moveConfig.GetStopClipPairData(animationId);
            bool useLeftFoot = IsLeftFoot(animationId);

            m_context.Action.IsStateFinished = false;
            m_context.Movement.StopAnimationId = animationId;
            m_context.Movement.ClearHorizontalMoveIntent();
            m_context.Action.SetRootMotionMoveEnabled(false);
            m_context.Action.ClearRootMotionDeltaPosition();
            m_context.Action.RequestAnimReplay(PlayerStateId.GroundedStop, 0.03f);
            m_stopTimer.Start(clipPairData.GetDuration(useLeftFoot));
        }

        // 离开急停状态时恢复对应地面移动表现
        public override void Exit()
        {
            m_stopTimer.Reset();
            m_context.Action.IsStateFinished = false;
            m_context.Action.SetRootMotionMoveEnabled(false);
            m_context.Action.ClearRootMotionDeltaPosition();
            m_context.Movement.ClearHorizontalVelocity();
            m_context.Movement.StopAnimationId = PlayerStopAnimationId.None;

            if (m_context.Movement.IsGrounded)
            {
                PlayerStateId locomotionStateId = m_context.Input.MoveInput.sqrMagnitude > 0.01f
                    ? PlayerStateId.GroundedMove
                    : PlayerStateId.GroundedIdle;
                m_context.Action.RequestAnimReplay(locomotionStateId, STOP_EXIT_BLEND_DURATION);
            }
        }

        // 推进急停动画计时
        public override void Tick(float deltaTime)
        {
            m_stopTimer.Tick(deltaTime);
            if (m_stopTimer.IsFinished)
                m_context.Action.IsStateFinished = true;
        }

        // 急停期间保持没有新的水平移动意图
        public override void FixedTick(float fixedDeltaTime)
        {
            m_context.Movement.ClearHorizontalMoveIntent();
        }

        // 根据水平速度档位和最近落脚选择急停动作
        private PlayerStopAnimationId SelectStopAnimationId()
        {
            float horizontalSpeed = GetHorizontalSpeed();
            bool useLeftFoot = SelectStopUseLeftFoot();

            if (horizontalSpeed >= (m_moveConfig.RunSpeed + m_moveConfig.SprintSpeed) * 0.5f)
                return useLeftFoot ? PlayerStopAnimationId.SprintLeft : PlayerStopAnimationId.SprintRight;

            if (horizontalSpeed >= (m_moveConfig.WalkSpeed + m_moveConfig.RunSpeed) * 0.5f)
                return useLeftFoot ? PlayerStopAnimationId.RunLeft : PlayerStopAnimationId.RunRight;

            return useLeftFoot ? PlayerStopAnimationId.WalkLeft : PlayerStopAnimationId.WalkRight;
        }

        // 选择与最近落脚相反的急停起脚
        private bool SelectStopUseLeftFoot()
        {
            if (m_context.Movement.HasLastPlantedFoot)
                return !m_context.Movement.IsLastPlantedFootLeft;

            return false;
        }

        // 判断指定急停动作是否使用左脚落地
        private bool IsLeftFoot(PlayerStopAnimationId animationId)
        {
            return animationId is PlayerStopAnimationId.WalkLeft
                or PlayerStopAnimationId.RunLeft
                or PlayerStopAnimationId.SprintLeft;
        }

        // 获取当前实际水平速度
        private float GetHorizontalSpeed()
        {
            Vector3 velocity = m_context.Movement.Velocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }
    }
}
