/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人移动上下文，保存导航方向与移动表现事实
 * │  类    名: EnemyMovementContext.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Enemy.Context.Runtime
{
    public sealed class EnemyMovementContext
    {
        public Vector3 MoveDirection { get; private set; }
        public Vector3 LookDirection { get; private set; }
        public bool HasMoveRequest { get; private set; }
        public bool HasLookRequest { get; private set; }
        public bool IsMoving { get; private set; }

        // 提交本帧导航移动方向
        public void SetMoveDirection(Vector3 moveDirection)
        {
            moveDirection.y = 0f;
            MoveDirection = moveDirection.normalized;
            HasMoveRequest = MoveDirection.sqrMagnitude > 0f;
        }

        // 提交本帧身体朝向方向
        public void SetLookDirection(Vector3 lookDirection)
        {
            lookDirection.y = 0f;
            LookDirection = lookDirection.normalized;
            HasLookRequest = LookDirection.sqrMagnitude > 0f;
        }

        // 更新移动执行后的表现事实
        public void SetMoving(bool isMoving)
        {
            IsMoving = isMoving;
        }

        // 停止导航移动并保留当前朝向请求
        public void StopMove()
        {
            MoveDirection = Vector3.zero;
            HasMoveRequest = false;
            IsMoving = false;
        }

        // 清理全部移动运行时事实
        public void Reset()
        {
            MoveDirection = Vector3.zero;
            LookDirection = Vector3.zero;
            HasMoveRequest = false;
            HasLookRequest = false;
            IsMoving = false;
        }
    }
}

