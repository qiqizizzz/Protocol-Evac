/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 玩家运行时上下文，保存输入、环境状态与运动意图
 * │  类    名: PlayerContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using UnityEngine;
using Utils.log;

namespace Module.Player.Context
{
    public sealed class PlayerContext
    {
        public Transform Transform { get; }
        
        // ==================== 移动相关 ====================
        public Vector2 MoveInput { get; set; } //移动输入
        public Vector3 MoveDir { get; set; }   //移动方向
        public float TargetMoveSpeed { get; set; } //期望移动速度
        public Vector3 Velocity { get; set; } //当前速度
        public bool IsMovementLocked { get; set; } //禁止移动
        public bool IsGrounded { get; set; }

        // ==================== 输入相关 ====================
        public bool IsSprintPressed { get; set; } //是否按住疾跑输入
        public bool IsInputLocked { get; set; } //是否输入被禁止

        public PlayerContext(Transform transform)
        {
            if (transform == null)
            {
                QLog.Error("Transform is null");
                return;
            }
            
            Transform = transform;
            ResetRunTimeData();
        }
        
        public void ResetRunTimeData()
        {
            MoveInput = Vector2.zero;
            MoveDir = Vector3.zero;
            TargetMoveSpeed = 0f;
            Velocity = Vector3.zero;
            IsMovementLocked = false;
            IsGrounded = false;
            IsSprintPressed = false;
            IsInputLocked = false;
        }
    }
}