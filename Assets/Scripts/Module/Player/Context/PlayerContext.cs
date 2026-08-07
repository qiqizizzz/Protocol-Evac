/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 玩家运行时上下文，保存输入、环境状态与运动意图
 * │  类    名: PlayerContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using UnityEngine;
using Module.Player.Core.View;
using Module.Player.HFSM;
using Module.Player.Input.Buffer;
using Module.Player.Skill;

namespace Module.Player.Context
{
    public sealed class PlayerContext
    {
        public Transform Transform { get; }
        public PlayerInputBuffer InputBuffer { get; }
        
        // ==================== 移动相关 ====================
        public Vector2 MoveInput { get; set; } //移动输入
        public Vector3 MoveDir { get; set; }   //移动方向
        public float TargetMoveSpeed { get; set; } //期望移动速度
        public Vector3 Velocity { get; set; } //当前速度
        public bool HasForcedMoveVelocity { get; private set; } //是否存在强制水平移动速度
        public Vector3 ForcedMoveVelocity { get; private set; } //强制水平移动速度
        public bool IsMovementLocked { get; set; } //禁止移动
        public bool IsGrounded { get; set; }
        public bool HasGroundedChecked { get; set; } //是否已经刷新过地面状态
        public float LastGroundedTime { get; set; } //最后一次处于地面的时间

        // ==================== 输入相关 ====================
        public bool IsSprintPressed { get; set; } //是否按住疾跑输入
        public bool IsInputLocked { get; set; } //是否输入被禁止
        public bool IsStateFinished { get; set; } //当前状态是否已经完成
        public int NormalAttackIndex { get; set; } //当前普通攻击段数
        public PlayerSkillStepPhase NormalAttackPhase { get; set; } //当前普通攻击段落阶段
        public bool IsRootMotionMoveEnabled { get; private set; } //是否允许使用动画根运动位移
        public Vector3 RootMotionDeltaPosition { get; private set; } //动画累计的根运动位移
        public bool IsWeaponVisible { get; set; } //武器是否显示

        // ==================== 视角相关 ====================
        public Vector2 LookInput { get; set; } //视角输入
        public PlayerViewMode ViewMode { get; set; } //当前视角模式
        public PlayerViewMode? TargetViewMode { get; set; } //请求切换的视角模式
        public float CameraYaw { get; set; } //相机水平角
        public float CameraPitch { get; set; } //相机俯仰角
        public PlayerStateId? AnimReplayStateId { get; private set; } //请求从起点重播的动画状态
        public float AnimReplayBlendDuration { get; private set; }

        public PlayerContext(Transform transform)
        {
            Transform = transform;
            InputBuffer = new PlayerInputBuffer();
            ResetRunTimeData();
        }
        
        public void ResetRunTimeData()
        {
            MoveInput = Vector2.zero;
            MoveDir = Vector3.zero;
            TargetMoveSpeed = 0f;
            Velocity = Vector3.zero;
            HasForcedMoveVelocity = false;
            ForcedMoveVelocity = Vector3.zero;
            IsMovementLocked = false;
            IsGrounded = false;
            HasGroundedChecked = false;
            LastGroundedTime = float.NegativeInfinity;
            IsSprintPressed = false;
            IsInputLocked = false;
            IsStateFinished = false;
            NormalAttackIndex = 0;
            NormalAttackPhase = PlayerSkillStepPhase.Begin;
            IsRootMotionMoveEnabled = false;
            RootMotionDeltaPosition = Vector3.zero;
            IsWeaponVisible = false;
            InputBuffer.ClearAll();
            LookInput = Vector2.zero;
            ViewMode = PlayerViewMode.FirstPerson;
            TargetViewMode = null;
            CameraYaw = Transform != null ? Transform.eulerAngles.y : 0f;
            CameraPitch = 0f;
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

        // 设置强制水平移动速度
        public void SetForcedMoveVelocity(Vector3 velocity)
        {
            HasForcedMoveVelocity = true;
            ForcedMoveVelocity = velocity;
        }

        // 清空强制水平移动速度
        public void ClearForcedMoveVelocity()
        {
            HasForcedMoveVelocity = false;
            ForcedMoveVelocity = Vector3.zero;
        }
    }
}
