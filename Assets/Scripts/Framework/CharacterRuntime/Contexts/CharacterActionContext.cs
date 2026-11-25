/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 角色动作运行时上下文，保存根运动、武器表现与动画重播请求
 * │  类    名: CharacterActionContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using System;
using Module.Ability.Data;
using UnityEngine;

namespace Framework.Character.Runtime.Contexts
{
    public class CharacterActionContext<TStateId> : ICharacterRuntimeContext where TStateId : struct, Enum
    {
        public bool IsStateFinished { get; set; }
        public bool IsRootMotionMoveEnabled { get; private set; }
        public Vector3 RootMotionDeltaPosition { get; private set; }
        public bool IsWeaponVisible { get; set; }
        public TStateId? AnimReplayStateId { get; private set; }
        public float AnimReplayBlendDuration { get; private set; }
        public int NormalAttackIndex { get; set; }
        public AbilityStepPhase NormalAttackPhase { get; set; }

        // 创建角色动作运行时上下文
        public CharacterActionContext()
        {
            Reset();
        }

        // 重置动作运行时数据
        public virtual void Reset()
        {
            IsStateFinished = false;
            IsRootMotionMoveEnabled = false;
            RootMotionDeltaPosition = Vector3.zero;
            IsWeaponVisible = false;
            AnimReplayStateId = null;
            AnimReplayBlendDuration = 0f;
            NormalAttackIndex = 0;
            NormalAttackPhase = AbilityStepPhase.Begin;
        }

        // 请求动画表现层从起点重播指定状态
        public void RequestAnimReplay(TStateId stateId)
        {
            AnimReplayStateId = stateId;
            AnimReplayBlendDuration = 0f;
        }

        // 请求动画表现层从起点重播指定状态，并携带混合时长
        public void RequestAnimReplay(TStateId stateId, float blendDuration)
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

        // 清空尚未消费的根运动位移
        public void ClearRootMotionDeltaPosition()
        {
            RootMotionDeltaPosition = Vector3.zero;
        }

        // 消费一次性动画重播请求
        public TStateId? ConsumeAnimReplayRequest()
        {
            TStateId? stateId = AnimReplayStateId;
            AnimReplayStateId = null;
            return stateId;
        }
    }
}
