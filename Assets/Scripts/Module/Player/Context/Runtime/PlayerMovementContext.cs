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
        public bool HasForcedMoveVelocity { get; private set; }
        public Vector3 ForcedMoveVelocity { get; private set; }
        public bool IsMovementLocked { get; set; }
        public bool IsGrounded { get; set; }
        public bool HasGroundedChecked { get; set; }
        public float LastGroundedTime { get; set; }

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
            HasForcedMoveVelocity = false;
            ForcedMoveVelocity = Vector3.zero;
            IsMovementLocked = false;
            IsGrounded = false;
            HasGroundedChecked = false;
            LastGroundedTime = float.NegativeInfinity;
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
    }
}
