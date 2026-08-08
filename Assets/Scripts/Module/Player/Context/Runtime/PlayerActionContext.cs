/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 玩家动作运行时上下文，保存技能阶段、根运动与动画请求
 * │  类    名: PlayerActionContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using Module.Player.HFSM;
using Module.Player.Skill;
using UnityEngine;

namespace Module.Player.Context.Runtime
{
    public sealed class PlayerActionContext : IPlayerRuntimeContext
    {
        public bool IsStateFinished { get; set; }
        public int NormalAttackIndex { get; set; }
        public PlayerSkillStepPhase NormalAttackPhase { get; set; }
        public bool IsRootMotionMoveEnabled { get; private set; }
        public Vector3 RootMotionDeltaPosition { get; private set; }
        public bool IsWeaponVisible { get; set; }
        public PlayerStateId? AnimReplayStateId { get; private set; }
        public float AnimReplayBlendDuration { get; private set; }

        // 创建动作运行时上下文
        public PlayerActionContext()
        {
            Reset();
        }

        // 重置动作运行时数据
        public void Reset()
        {
            IsStateFinished = false;
            NormalAttackIndex = 0;
            NormalAttackPhase = PlayerSkillStepPhase.Begin;
            IsRootMotionMoveEnabled = false;
            RootMotionDeltaPosition = Vector3.zero;
            IsWeaponVisible = false;
            AnimReplayStateId = null;
            AnimReplayBlendDuration = 0f;
        }

        // 请求动画表现层从起点重播指定状态
        public void RequestAnimReplay(PlayerStateId stateId)
        {
            AnimReplayStateId = stateId;
            AnimReplayBlendDuration = 0f;
        }

        // 请求动画表现层从起点重播指定状态，并携带混合时长
        public void RequestAnimReplay(PlayerStateId stateId, float blendDuration)
        {
            AnimReplayStateId = stateId;
            AnimReplayBlendDuration = blendDuration;
        }

        // 设置是否允许使用动画根运动位移
        public void SetRootMotionMoveEnabled(bool isEnabled)
        {
            IsRootMotionMoveEnabled = isEnabled;
            if (!isEnabled)
                RootMotionDeltaPosition = Vector3.zero;
        }

        // 累加动画层输出的根运动位移
        public void AddRootMotionDeltaPosition(Vector3 deltaPosition)
        {
            RootMotionDeltaPosition += deltaPosition;
        }

        // 取出并清空当前帧累计的根运动位移
        public Vector3 ConsumeRootMotionDeltaPosition()
        {
            Vector3 deltaPosition = RootMotionDeltaPosition;
            RootMotionDeltaPosition = Vector3.zero;
            return deltaPosition;
        }

        // 消费一次性动画重播请求
        public PlayerStateId? ConsumeAnimReplayRequest()
        {
            PlayerStateId? stateId = AnimReplayStateId;
            AnimReplayStateId = null;
            return stateId;
        }
    }
}
