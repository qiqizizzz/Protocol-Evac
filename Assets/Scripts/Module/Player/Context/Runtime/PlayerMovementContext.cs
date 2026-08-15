/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 玩家移动运行时上下文，保存位移意图、速度与地面状态
 * │  类    名: PlayerMovementContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Player.Context.Runtime
{
    public sealed class PlayerMovementContext : IPlayerRuntimeContext
    {
        public Vector3 MoveDir { get; set; }
        public float TargetMoveSpeed { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 StopDirection { get; private set; }
        public bool HasForcedMoveVelocity { get; private set; }
        public Vector3 ForcedMoveVelocity { get; private set; }
        public PlayerStopAnimationId StopAnimationId { get; set; }
        public bool IsMovementLocked { get; set; }
        public bool IsGrounded { get; set; }
        public bool HasGroundedChecked { get; set; }
        public float LastGroundedTime { get; set; }
        public bool HasLastPlantedFoot { get; private set; }
        public bool IsLastPlantedFootLeft { get; private set; }

        // 创建移动运行时上下文
        public PlayerMovementContext()
        {
            Reset();
        }

        // 重置移动运行时数据
        public void Reset()
        {
            MoveDir = Vector3.zero;
            TargetMoveSpeed = 0f;
            Velocity = Vector3.zero;
            StopDirection = Vector3.zero;
            HasForcedMoveVelocity = false;
            ForcedMoveVelocity = Vector3.zero;
            StopAnimationId = PlayerStopAnimationId.None;
            IsMovementLocked = false;
            IsGrounded = false;
            HasGroundedChecked = false;
            LastGroundedTime = float.NegativeInfinity;
            HasLastPlantedFoot = false;
            IsLastPlantedFootLeft = false;
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

        // 清空当前水平速度并保留竖直速度
        public void ClearHorizontalVelocity()
        {
            Velocity = new Vector3(0f, Velocity.y, 0f);
        }

        // 清空当前水平移动意图
        public void ClearHorizontalMoveIntent()
        {
            MoveDir = Vector3.zero;
            TargetMoveSpeed = 0f;
        }

        // 设置急停期间角色应朝向的水平移动方向
        public void SetStopDirection(Vector3 direction)
        {
            direction.y = 0f;
            StopDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.zero;
        }

        // 清空急停方向，避免后续根运动复用旧方向
        public void ClearStopDirection()
        {
            StopDirection = Vector3.zero;
        }

        // 记录最近一次由移动动画触发的落脚
        public void RecordPlantedFoot(bool isLeftFoot)
        {
            HasLastPlantedFoot = true;
            IsLastPlantedFootLeft = isLeftFoot;
        }
    }
}
